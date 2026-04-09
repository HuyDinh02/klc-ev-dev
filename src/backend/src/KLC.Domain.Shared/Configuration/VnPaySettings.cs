namespace KLC.Configuration;

/// <summary>
/// Typed configuration for VnPay payment gateway.
/// Maps to the "Payment:VnPay" section in appsettings.json.
/// </summary>
public class VnPaySettings
{
    public const string Section = "Payment:VnPay";

    public string TmnCode { get; set; } = string.Empty;
    public string HashSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://sandbox.vnpayment.vn";
    public string Version { get; set; } = "2.1.0";
    public string QueryApiUrl { get; set; } = "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction";
}
