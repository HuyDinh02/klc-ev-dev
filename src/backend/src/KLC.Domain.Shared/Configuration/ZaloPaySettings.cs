namespace KLC.Configuration;

/// <summary>
/// Typed configuration for ZaloPay payment gateway.
/// Maps to the "Payment:ZaloPay" section in appsettings.json.
/// </summary>
public class ZaloPaySettings
{
    public const string Section = "Payment:ZaloPay";

    public int AppId { get; set; }
    public string Key1 { get; set; } = string.Empty;
    public string Key2 { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://sb-openapi.zalopay.vn";
    public string CallbackUrl { get; set; } = string.Empty;
}
