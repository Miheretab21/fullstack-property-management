namespace PropertyManagement.Infrastructure.Settings;

public class ChapaSettings
{
    public const string SectionName = "Chapa";

    public string SecretKey { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = "https://api.chapa.co";
    public string? CallbackUrl { get; set; }
    public string ReturnUrl { get; set; } = "http://localhost:4300/payment-result";
}
