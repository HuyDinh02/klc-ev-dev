namespace KLC.Configuration;

/// <summary>
/// Typed configuration for wallet settings.
/// Maps to the "Wallet" section in appsettings.json.
/// </summary>
public class WalletSettings
{
    public const string Section = "Wallet";

    /// <summary>Minimum top-up amount (VND). BA: 50,000đ</summary>
    public decimal MinTopUpAmount { get; set; } = 50_000;

    /// <summary>Maximum top-up amount per transaction (VND)</summary>
    public decimal MaxTopUpAmount { get; set; } = 10_000_000;

    /// <summary>Minimum wallet balance required to start charging (VND). BA: 50,000đ</summary>
    public decimal MinBalanceToStart { get; set; } = 50_000;

    /// <summary>Auto-stop charging when remaining balance drops below this (VND). BA: 20,000đ</summary>
    public decimal LowBalanceThreshold { get; set; } = 20_000;
}
