using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Application.Common;
using PropertyManagement.Application.Models.Financial;
using PropertyManagement.Application.Services;
using PropertyManagement.Domain.Enums;

namespace PropertyManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FinancialController : ControllerBase
{
    private readonly IFinancialService _financialService;
    private readonly ICurrentUserService _currentUserService;

    public FinancialController(IFinancialService financialService, ICurrentUserService currentUserService)
    {
        _financialService = financialService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// List financial transactions. Tenants only see their own transactions; Managers/Admins can see all.
    /// </summary>
    [HttpGet("transactions")]
    [ProducesResponseType(typeof(List<TransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions([FromQuery] Guid? leaseId, [FromQuery] Guid? tenantId, [FromQuery] TransactionStatus? status, CancellationToken cancellationToken)
    {
        var isTenantOnly = !_currentUserService.Roles.Contains("Admin") && !_currentUserService.Roles.Contains("PropertyManager");
        var effectiveTenantId = isTenantOnly ? _currentUserService.UserId : tenantId;

        var result = await _financialService.GetTransactionsAsync(leaseId, effectiveTenantId, status, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get transaction by ID
    /// </summary>
    [HttpGet("transactions/{id:guid}")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactionById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _financialService.GetTransactionByIdAsync(id, cancellationToken);
        if (!result.Succeeded)
        {
            return NotFound(result);
        }

        var isTenantOnly = !_currentUserService.Roles.Contains("Admin") && !_currentUserService.Roles.Contains("PropertyManager");
        if (isTenantOnly && result.Data?.TenantId != _currentUserService.UserId)
        {
            return Forbid();
        }

        return Ok(result);
    }

    /// <summary>
    /// Get tenant ledger summary for a lease (Total billed, total paid, outstanding balance)
    /// </summary>
    [HttpGet("ledger/{leaseId:guid}")]
    [ProducesResponseType(typeof(TenantLedgerSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLedger(Guid leaseId, CancellationToken cancellationToken)
    {
        var result = await _financialService.GetTenantLedgerSummaryAsync(leaseId, cancellationToken);
        if (!result.Succeeded)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Generate a new rent charge or fee on a lease (Admin or Property Manager only)
    /// </summary>
    [HttpPost("charges")]
    [Authorize(Roles = "Admin,PropertyManager")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCharge([FromBody] CreateRentChargeDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _financialService.CreateRentChargeAsync(dto, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetTransactionById), new { id = result.Data!.Id }, result);
    }

    /// <summary>
    /// Record a payment for an existing charge
    /// </summary>
    [HttpPost("payments")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordPayment([FromBody] RecordPaymentDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _financialService.RecordPaymentAsync(dto, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Record an incoming payment (e.g. check or bank payment) directly for a lease
    /// </summary>
    [HttpPost("direct-payments")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDirectPayment([FromBody] CreateDirectPaymentDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _financialService.CreateDirectPaymentAsync(dto, cancellationToken);
        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
