namespace KLC;

public static class KLCDomainErrorCodes
{
    public static class Station
    {
        public const string DuplicateCode = "KLC:Station:DuplicateCode";
        public const string NotFound = "KLC:Station:NotFound";
        public const string InvalidLatitude = "KLC:Station:InvalidLatitude";
        public const string InvalidLongitude = "KLC:Station:InvalidLongitude";
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
    }

    public static class Vehicle
    {
        public const string NotOwned = "KLC:Vehicle:NotOwned";
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
    }
}
