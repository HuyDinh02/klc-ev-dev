namespace KLC.Ocpp.Messages;

/// <summary>
/// OCPP 1.6J message type identifiers.
/// </summary>
public static class OcppMessageType
{
    /// <summary>
    /// Call message (request from Charge Point or Central System).
    /// </summary>
    public const int Call = 2;

    /// <summary>
    /// CallResult message (successful response).
    /// </summary>
    public const int CallResult = 3;

    /// <summary>
    /// CallError message (error response).
    /// </summary>
    public const int CallError = 4;
}

/// <summary>
/// OCPP 1.6J error codes.
/// </summary>
public static class OcppErrorCode
{
    public const string NotImplemented = "NotImplemented";
    public const string NotSupported = "NotSupported";
    public const string InternalError = "InternalError";
    public const string ProtocolError = "ProtocolError";
    public const string SecurityError = "SecurityError";
    public const string FormationViolation = "FormationViolation";
    public const string PropertyConstraintViolation = "PropertyConstraintViolation";
    public const string OccurrenceConstraintViolation = "OccurrenceConstraintViolation";
    public const string TypeConstraintViolation = "TypeConstraintViolation";
    public const string GenericError = "GenericError";
}
