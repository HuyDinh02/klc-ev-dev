namespace KLC.Configuration;

/// <summary>
/// Typed configuration for JWT token settings.
/// Maps to the "Jwt" section in appsettings.json.
/// </summary>
public class JwtSettings
{
    public const string Section = "Jwt";

    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "KLC.Driver.BFF";
    public string Audience { get; set; } = "KLC.Driver.App";
    public int ExpiryMinutes { get; set; } = 60;
}
