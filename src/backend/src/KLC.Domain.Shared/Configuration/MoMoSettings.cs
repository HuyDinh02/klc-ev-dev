namespace KLC.Configuration;

/// <summary>
/// Typed configuration for MoMo payment gateway.
/// Maps to the "Payment:MoMo" section in appsettings.json.
/// </summary>
public class MoMoSettings
{
    public const string Section = "Payment:MoMo";

    public string PartnerCode { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://test-payment.momo.vn";
}
