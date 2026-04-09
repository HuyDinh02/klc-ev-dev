using System;
using Microsoft.Extensions.Logging;

namespace KLC.Auditing;

public interface IAuditEventLogger
{
    void LogPaymentEvent(string action, Guid transactionId, decimal amount, string gateway, string? userId = null);
    void LogAuthEvent(string action, string? userId = null, string? ipAddress = null, string? details = null);
    void LogOcppEvent(string action, string chargePointId, string? details = null);
    void LogSessionEvent(string action, Guid sessionId, string? userId = null, string? details = null);
}

public class AuditEventLogger : IAuditEventLogger, Volo.Abp.DependencyInjection.ITransientDependency
{
    private readonly ILogger<AuditEventLogger> _logger;

    public AuditEventLogger(ILogger<AuditEventLogger> logger)
    {
        _logger = logger;
    }

    public void LogPaymentEvent(string action, Guid transactionId, decimal amount, string gateway, string? userId = null)
    {
        _logger.LogInformation(
            "AUDIT:Payment Action={Action} TransactionId={TransactionId} Amount={Amount} Gateway={Gateway} UserId={UserId}",
            action, transactionId, amount, gateway, userId);
    }

    public void LogAuthEvent(string action, string? userId = null, string? ipAddress = null, string? details = null)
    {
        _logger.LogInformation(
            "AUDIT:Auth Action={Action} UserId={UserId} IP={IpAddress} Details={Details}",
            action, userId, ipAddress, details);
    }

    public void LogOcppEvent(string action, string chargePointId, string? details = null)
    {
        _logger.LogInformation(
            "AUDIT:OCPP Action={Action} ChargePointId={ChargePointId} Details={Details}",
            action, chargePointId, details);
    }

    public void LogSessionEvent(string action, Guid sessionId, string? userId = null, string? details = null)
    {
        _logger.LogInformation(
            "AUDIT:Session Action={Action} SessionId={SessionId} UserId={UserId} Details={Details}",
            action, sessionId, userId, details);
    }
}
