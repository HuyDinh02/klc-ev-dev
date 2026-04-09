namespace KLC.Driver;

/// <summary>
/// Centralized cache key constants for the Driver BFF layer.
/// All cache keys follow the pattern "entity:{id}:field".
/// </summary>
public static class CacheKeys
{
    // Station keys
    public static string StationConnectors(Guid stationId) => $"station:{stationId}:connectors";
    public static string StationDetail(Guid stationId) => $"station:{stationId}:detail";

    // Session keys
    public static string SessionDetail(Guid sessionId) => $"session:{sessionId}:detail";

    // User keys
    public static string UserActiveSession(Guid userId) => $"user:{userId}:active-session";
    public static string UserWalletBalance(Guid userId) => $"user:{userId}:wallet-balance";
    public static string UserWalletSummary(Guid userId) => $"user:{userId}:wallet-summary";
    public static string UserAvailableVouchers(Guid userId) => $"user:{userId}:available-vouchers";
    public static string UserProfile(Guid userId) => $"user:{userId}:profile";
    public static string UserStats(Guid userId) => $"user:{userId}:stats";
    public static string UserFavorites(Guid userId) => $"user:{userId}:favorites";
    public static string UserUnreadNotifications(Guid userId) => $"user:{userId}:unread-notifications";
    public static string UserPaymentMethods(Guid userId) => $"user:{userId}:payment-methods";
    public static string UserVehicles(Guid userId) => $"user:{userId}:vehicles";
    public static string UserDefaultVehicle(Guid userId) => $"user:{userId}:default-vehicle";
}
