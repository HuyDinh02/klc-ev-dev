namespace KLC.Configuration;

/// <summary>
/// Typed configuration for wallet/payment general settings.
/// Maps to the "Payment:Wallet" section in appsettings.json.
/// </summary>
public class WalletSettings
{
    public const string Section = "Payment:Wallet";

    public decimal MinTopUpAmount { get; set; } = 10_000;
    public decimal MaxTopUpAmount { get; set; } = 5_000_000;
    public decimal MaxBalance { get; set; } = 10_000_000;
}
