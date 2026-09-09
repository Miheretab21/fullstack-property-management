using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PropertyManagement.Application.Common;
using PropertyManagement.Domain.Enums;
using PropertyManagement.Infrastructure.Identity;
using PropertyManagement.Infrastructure.Settings;

namespace PropertyManagement.API.Controllers;

[ApiController]
[Route("api/payments/chapa")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ChapaSettings _settings;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IApplicationDbContext context, ICurrentUserService currentUser,
        UserManager<ApplicationUser> userManager, IHttpClientFactory httpClientFactory,
        IOptions<ChapaSettings> settings, ILogger<PaymentsController> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _userManager = userManager;
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    [HttpPost("initialize")]
    public async Task<IActionResult> Initialize([FromBody] InitializeChapaPaymentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.SecretKey))
            return Problem("Chapa is not configured. Set Chapa:SecretKey in secure configuration.", statusCode: StatusCodes.Status503ServiceUnavailable);

        var transaction = await GetAuthorizedTransactionAsync(request.TransactionId, cancellationToken);
        if (transaction is null) return NotFound("Pending transaction not found or is not available to the current user.");
        if (transaction.Status == TransactionStatus.Paid) return BadRequest("This transaction has already been paid.");

        var tenant = await _userManager.FindByIdAsync(transaction.Lease!.TenantId.ToString());
        if (tenant is null || string.IsNullOrWhiteSpace(tenant.Email)) return BadRequest("The tenant needs a valid email address before paying with Chapa.");

        // Chapa allows a maximum of 50 characters for tx_ref.
        var txRef = $"pms-{Guid.NewGuid():N}";
        transaction.ChapaTransactionReference = txRef;
        transaction.ChapaPaymentReference = null;
        await _context.SaveChangesAsync(cancellationToken);

        var returnUrl = AddTransactionIdToReturnUrl(_settings.ReturnUrl, transaction.Id);
        var payload = new
        {
            amount = transaction.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
            currency = "ETB",
            email = tenant.Email,
            first_name = tenant.FirstName,
            last_name = tenant.LastName,
            tx_ref = txRef,
            callback_url = string.IsNullOrWhiteSpace(_settings.CallbackUrl) ? null : _settings.CallbackUrl,
            return_url = returnUrl,
            // Chapa limits the title to 16 characters and description to 50 characters.
            customization = new { title = "Rent Payment", description = "Property rent payment" }
        };

        var response = await SendAsync(HttpMethod.Post, "v1/transaction/initialize", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var chapaError = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Chapa initialization failed for transaction {TransactionId}: {StatusCode}. Response: {ChapaResponse}",
                transaction.Id, response.StatusCode, chapaError);
            return Problem(
                title: "Chapa rejected the payment request",
                detail: string.IsNullOrWhiteSpace(chapaError)
                    ? "Chapa did not provide an explanation. Check the test secret key and Chapa account settings."
                    : chapaError,
                statusCode: StatusCodes.Status502BadGateway);
        }

        var chapa = await response.Content.ReadFromJsonAsync<ChapaApiResponse>(cancellationToken: cancellationToken);
        var checkoutUrl = chapa?.Data?.CheckoutUrl;
        if (!string.Equals(chapa?.Status, "success", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(checkoutUrl))
            return StatusCode(StatusCodes.Status502BadGateway, "Chapa did not return a checkout URL.");

        return Ok(new { checkoutUrl, transactionId = transaction.Id });
    }

    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] VerifyChapaPaymentRequest request, CancellationToken cancellationToken)
    {
        var transaction = await GetAuthorizedTransactionAsync(request.TransactionId, cancellationToken);
        if (transaction is null) return NotFound("Transaction not found or is not available to the current user.");
        return await VerifyAndRecordAsync(transaction, cancellationToken);
    }

    // Configure this public URL in Chapa when the API is publicly reachable. It still verifies against Chapa before recording payment.
    [AllowAnonymous]
    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery(Name = "trx_ref")] string? transactionReference, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(transactionReference)) return BadRequest("Missing transaction reference.");
        var transaction = await _context.Transactions.Include(t => t.Lease)
            .FirstOrDefaultAsync(t => t.ChapaTransactionReference == transactionReference, cancellationToken);
        if (transaction is null) return NotFound();
        return await VerifyAndRecordAsync(transaction, cancellationToken);
    }

    private async Task<IActionResult> VerifyAndRecordAsync(Domain.Entities.Transaction transaction, CancellationToken cancellationToken)
    {
        if (transaction.Status == TransactionStatus.Paid)
            return Ok(new { succeeded = true, status = "Paid", message = "Payment has already been verified." });
        if (string.IsNullOrWhiteSpace(_settings.SecretKey) || string.IsNullOrWhiteSpace(transaction.ChapaTransactionReference))
            return BadRequest("No Chapa payment has been initialized for this transaction.");

        var response = await SendAsync(HttpMethod.Get, $"v1/transaction/verify/{Uri.EscapeDataString(transaction.ChapaTransactionReference)}", null, cancellationToken);
        if (!response.IsSuccessStatusCode) return Ok(new { succeeded = false, status = "Pending", message = "Payment is not complete yet." });

        var chapa = await response.Content.ReadFromJsonAsync<ChapaApiResponse>(cancellationToken: cancellationToken);
        var data = chapa?.Data;
        var verificationIssue = GetVerificationIssue(chapa, data, transaction);
        if (verificationIssue is not null)
        {
            _logger.LogWarning("Chapa verification failed for transaction {TransactionId}: {VerificationIssue}", transaction.Id, verificationIssue);
            return Ok(new { succeeded = false, status = "Pending", message = verificationIssue });
        }

        transaction.Status = TransactionStatus.Paid;
        transaction.PaymentMethod = $"Chapa ({data.Method ?? "Online"})";
        transaction.PaymentDate = DateTime.UtcNow;
        transaction.ChapaPaymentReference = data.Reference;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { succeeded = true, status = "Paid", message = "Payment verified successfully." });
    }

    private async Task<Domain.Entities.Transaction?> GetAuthorizedTransactionAsync(Guid transactionId, CancellationToken cancellationToken)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Lease)
                .ThenInclude(l => l!.Unit)
                    .ThenInclude(u => u!.Property)
            .FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken);
        if (transaction?.Lease is null) return null;
        if (_currentUser.Roles.Contains("Admin")) return transaction;
        if (_currentUser.Roles.Contains("PropertyManager"))
            return transaction.Lease.Unit?.Property?.AssignedManagerId == _currentUser.UserId ? transaction : null;
        return transaction.Lease.TenantId == _currentUser.UserId ? transaction : null;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? payload, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("Chapa");
        client.BaseAddress = new Uri(_settings.ApiBaseUrl.TrimEnd('/') + "/");
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.SecretKey);
        if (payload is not null) request.Content = JsonContent.Create(payload);
        return await client.SendAsync(request, cancellationToken);
    }

    private static string AddTransactionIdToReturnUrl(string returnUrl, Guid transactionId)
        => $"{returnUrl}{(returnUrl.Contains('?') ? '&' : '?')}transactionId={transactionId}";

    private static string? GetVerificationIssue(ChapaApiResponse? chapa, ChapaPaymentData? data, Domain.Entities.Transaction transaction)
    {
        if (!string.Equals(chapa?.Status, "success", StringComparison.OrdinalIgnoreCase))
            return "Chapa did not confirm the verification request as successful.";
        if (data is null) return "Chapa did not return payment details for this transaction.";
        if (!string.Equals(data.Status, "success", StringComparison.OrdinalIgnoreCase))
            return $"Chapa reports this payment as '{data.Status ?? "unknown"}', not successful.";
        if (!string.Equals(data.Currency, "ETB", StringComparison.OrdinalIgnoreCase))
            return $"The verified currency '{data.Currency ?? "unknown"}' does not match ETB.";
        if (data.Amount != transaction.Amount)
            return $"The verified amount ({data.Amount:0.00} ETB) does not match the rent amount ({transaction.Amount:0.00} ETB).";
        if (!string.Equals(data.TxRef, transaction.ChapaTransactionReference, StringComparison.Ordinal))
            return "The payment reference returned by Chapa does not match this rent charge.";
        return null;
    }

    public record InitializeChapaPaymentRequest(Guid TransactionId);
    public record VerifyChapaPaymentRequest(Guid TransactionId);

    private sealed class ChapaApiResponse { public string? Status { get; init; } public ChapaPaymentData? Data { get; init; } }
    private sealed class ChapaPaymentData
    {
        [JsonPropertyName("checkout_url")]
        public string? CheckoutUrl { get; init; }
        public string? Status { get; init; }
        public string? Currency { get; init; }
        public decimal Amount { get; init; }
        [JsonPropertyName("tx_ref")]
        public string? TxRef { get; init; }
        public string? Reference { get; init; }
        public string? Method { get; init; }
    }
}
