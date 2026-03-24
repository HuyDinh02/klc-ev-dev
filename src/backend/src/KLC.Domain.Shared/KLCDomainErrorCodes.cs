namespace KLC;

public static class KLCDomainErrorCodes
{
    public const string EntityNotFound = "KLC:EntityNotFound";

    public static class Station
    {
        public const string DuplicateCode = "KLC:Station:DuplicateCode";
        public const string NotFound = "KLC:Station:NotFound";
        public const string InvalidLatitude = "KLC:Station:InvalidLatitude";
        public const string InvalidLongitude = "KLC:Station:InvalidLongitude";
        public const string HasActiveSessions = "KLC:Station:HasActiveSessions";
        public const string CannotEnableDecommissioned = "KLC:Station:CannotEnableDecommissioned";
    }

    public static class Connector
    {
        public const string StationNotFound = "KLC:Connector:StationNotFound";
        public const string DuplicateNumber = "KLC:Connector:DuplicateNumber";
        public const string MaxPowerInvalid = "KLC:Connector:MaxPowerInvalid";
    }

    public static class Fault
    {
        public const string InvalidStatusTransition = "KLC:Fault:InvalidStatusTransition";
        public const string InvalidPriority = "KLC:Fault:InvalidPriority";
    }

    public static class Tariff
    {
        public const string InvalidBaseRate = "KLC:Tariff:InvalidBaseRate";
        public const string InvalidTaxRate = "KLC:Tariff:InvalidTaxRate";
        public const string InvalidEffectivePeriod = "KLC:Tariff:InvalidEffectivePeriod";
    }

    public static class Payment
    {
        public const string SessionNotOwned = "KLC:Payment:SessionNotOwned";
        public const string SessionNotCompleted = "KLC:Payment:SessionNotCompleted";
        public const string NotFound = "KLC:Payment:NotFound";
        public const string NotOwned = "KLC:Payment:NotOwned";
        public const string AlreadyCompleted = "KLC:Payment:AlreadyCompleted";
        public const string MethodNotOwned = "KLC:Payment:MethodNotOwned";
        public const string InvalidRefund = "KLC:Payment:InvalidRefund";
        public const string CannotCancel = "KLC:Payment:CannotCancel";
        public const string InvalidSignature = "KLC:Payment:InvalidSignature";
        public const string InvalidAmount = "KLC:Payment:InvalidAmount";
        public const string GatewayNotSupported = "KLC:Payment:GatewayNotSupported";
    }

    public static class Vehicle
    {
        public const string NotOwned = "KLC:Vehicle:NotOwned";
        public const string HasActiveSession = "KLC:Vehicle:HasActiveSession";
    }

    public static class Session
    {
        public const string ConnectorNotFound = "KLC:Session:ConnectorNotFound";
        public const string ConnectorNotAvailable = "KLC:Session:ConnectorNotAvailable";
        public const string AlreadyActive = "KLC:Session:AlreadyActive";
        public const string NoDefaultVehicle = "KLC:Session:NoDefaultVehicle";
        public const string NotOwned = "KLC:Session:NotOwned";
        public const string InvalidStatus = "KLC:Session:InvalidStatus";
        public const string InvalidStateTransition = "KLC:Session:InvalidStateTransition";
        public const string StartCommandFailed = "KLC:Session:StartCommandFailed";
        public const string StopCommandFailed = "KLC:Session:StopCommandFailed";
    }

    public static class Profile
    {
        public const string EmailAlreadyUsed = "KLC:Profile:EmailAlreadyUsed";
        public const string PhoneAlreadyUsed = "KLC:Profile:PhoneAlreadyUsed";
        public const string PasswordChangeFailed = "KLC:Profile:PasswordChangeFailed";
        public const string HasActiveSession = "KLC:Profile:HasActiveSession";
    }

    public static class StationGroup
    {
        public const string StationAlreadyAssigned = "KLC:StationGroup:StationAlreadyAssigned";
        public const string StationNotInGroup = "KLC:StationGroup:StationNotInGroup";
    }

    // User Management (already KLC: prefix)
    public const string UserNameAlreadyExists = "KLC:UserNameAlreadyExists";
    public const string EmailAlreadyExists = "KLC:EmailAlreadyExists";
    public const string UserCreationFailed = "KLC:UserCreationFailed";
    public const string CannotDeleteCurrentUser = "KLC:CannotDeleteCurrentUser";
    public const string CannotLockCurrentUser = "KLC:CannotLockCurrentUser";
    public const string PasswordResetFailed = "KLC:PasswordResetFailed";

    // Role Management (already KLC: prefix)
    public const string RoleNameAlreadyExists = "KLC:RoleNameAlreadyExists";
    public const string CannotUpdateStaticRole = "KLC:CannotUpdateStaticRole";
    public const string CannotDeleteStaticRole = "KLC:CannotDeleteStaticRole";
    public const string CannotDeleteRoleWithUsers = "KLC:CannotDeleteRoleWithUsers";

    // E-Invoice (already KLC: prefix)
    public const string InvoiceNotFound = "KLC:InvoiceNotFound";
    public const string EInvoiceAlreadyExists = "KLC:EInvoiceAlreadyExists";
    public const string EInvoiceCannotRetry = "KLC:EInvoiceCannotRetry";
    public const string EInvoiceAlreadyCancelled = "KLC:EInvoiceAlreadyCancelled";

    public static class Notification
    {
        public const string NotOwned = "KLC:Notification:NotOwned";
    }

    public static class Alert
    {
        public const string InvalidAcknowledge = "KLC:Alert:InvalidAcknowledge";
        public const string InvalidPriority = "KLC:Alert:InvalidPriority";
    }

    public static class Wallet
    {
        public const string InsufficientBalance = "KLC:Wallet:InsufficientBalance";
        public const string TransactionAlreadyCompleted = "KLC:Wallet:TransactionAlreadyCompleted";
        public const string TopUpFailed = "KLC:Wallet:TopUpFailed";
        public const string InvalidAmount = "KLC:Wallet:InvalidAmount";
        public const string MonthlyTopUpLimitExceeded = "KLC:Wallet:MonthlyTopUpLimitExceeded";
    }

    public static class Auth
    {
        public const string InvalidOtp = "KLC:Auth:InvalidOtp";
        public const string OtpExpired = "KLC:Auth:OtpExpired";
        public const string PhoneAlreadyRegistered = "KLC:Auth:PhoneAlreadyRegistered";
        public const string InvalidCredentials = "KLC:Auth:InvalidCredentials";
        public const string AccountSuspended = "KLC:Auth:AccountSuspended";
        public const string InvalidRefreshToken = "KLC:Auth:InvalidRefreshToken";
        public const string SocialLoginFailed = "KLC:Auth:SocialLoginFailed";
    }

    public static class Voucher
    {
        public const string NotFound = "KLC:Voucher:NotFound";
        public const string NotValid = "KLC:Voucher:NotValid";
        public const string AlreadyUsed = "KLC:Voucher:AlreadyUsed";
        public const string Expired = "KLC:Voucher:Expired";
        public const string DuplicateCode = "KLC:Voucher:DuplicateCode";
    }

    public static class Feedback
    {
        public const string NotFound = "KLC:Feedback:NotFound";
        public const string NotOwned = "KLC:Feedback:NotOwned";
        public const string AlreadyResolved = "KLC:Feedback:AlreadyResolved";
    }

    public static class Favorite
    {
        public const string AlreadyFavorited = "KLC:Favorite:AlreadyFavorited";
        public const string NotFavorited = "KLC:Favorite:NotFavorited";
    }

    public static class Maintenance
    {
        public const string NotFound = "KLC:Maintenance:NotFound";
        public const string InvalidStateTransition = "KLC:Maintenance:InvalidStateTransition";
    }

    public static class PowerSharing
    {
        public const string NotFound = "KLC:PowerSharing:NotFound";
        public const string InvalidCapacity = "KLC:PowerSharing:InvalidCapacity";
        public const string InvalidMinPower = "KLC:PowerSharing:InvalidMinPower";
        public const string ConnectorAlreadyInGroup = "KLC:PowerSharing:ConnectorAlreadyInGroup";
        public const string ConnectorNotInGroup = "KLC:PowerSharing:ConnectorNotInGroup";
        public const string MaxMembersExceeded = "KLC:PowerSharing:MaxMembersExceeded";
        public const string GroupNotActive = "KLC:PowerSharing:GroupNotActive";
    }

    public static class Operators
    {
        public const string NotFound = "KLC:Operator:NotFound";
        public const string DuplicateName = "KLC:Operator:DuplicateName";
        public const string InvalidApiKey = "KLC:Operator:InvalidApiKey";
        public const string NotActive = "KLC:Operator:NotActive";
        public const string StationAlreadyAssigned = "KLC:Operator:StationAlreadyAssigned";
        public const string StationNotAssigned = "KLC:Operator:StationNotAssigned";
        public const string RateLimitExceeded = "KLC:Operator:RateLimitExceeded";
        public const string NoStationAccess = "KLC:Operator:NoStationAccess";
    }

    public static class Fleet
    {
        public const string NotFound = "KLC:Fleet:NotFound";
        public const string InvalidBudget = "KLC:Fleet:InvalidBudget";
        public const string VehicleAlreadyInFleet = "KLC:Fleet:VehicleAlreadyInFleet";
        public const string VehicleNotInFleet = "KLC:Fleet:VehicleNotInFleet";
        public const string BudgetExceeded = "KLC:Fleet:BudgetExceeded";
        public const string DailyLimitExceeded = "KLC:Fleet:DailyLimitExceeded";
        public const string OutsideSchedule = "KLC:Fleet:OutsideSchedule";
        public const string StationNotAllowed = "KLC:Fleet:StationNotAllowed";
        public const string ChargingDenied = "KLC:Fleet:ChargingDenied";
    }
}
