using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace KLC.Notifications;

/// <summary>
/// Development SMS service that logs messages instead of sending them.
/// Replace with Twilio/AWS SNS implementation for production.
/// </summary>
public class LogOnlySmsService : ISmsService, ITransientDependency
{
    private readonly ILogger<LogOnlySmsService> _logger;

    public LogOnlySmsService(ILogger<LogOnlySmsService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string phoneNumber, string message)
    {
        _logger.LogInformation("[SMS-DEV] To: {Phone}, Message: {Message}", phoneNumber, message);
        return Task.CompletedTask;
    }
}
