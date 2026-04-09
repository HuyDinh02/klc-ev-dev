using System;
using System.Threading.Tasks;
using KLC.Ocpp.Messages;
using KLC.Ocpp.Vendors;
using Microsoft.Extensions.Logging;

namespace KLC.Ocpp.Handlers;

public class HeartbeatHandler : IOcppActionHandler
{
    private readonly ILogger<HeartbeatHandler> _logger;
    private readonly IOcppService _ocppService;
    private readonly VendorProfileFactory _vendorProfileFactory;

    public string Action => "Heartbeat";

    public HeartbeatHandler(
        ILogger<HeartbeatHandler> logger,
        IOcppService ocppService,
        VendorProfileFactory vendorProfileFactory)
    {
        _logger = logger;
        _ocppService = ocppService;
        _vendorProfileFactory = vendorProfileFactory;
    }

    public async Task<string> HandleAsync(OcppHandlerContext context)
    {
        context.Connection.RecordHeartbeat();
        _logger.LogDebug("Heartbeat from {ChargePointId}", context.Connection.ChargePointId);

        // Persist to database
        await _ocppService.HandleHeartbeatAsync(context.Connection.ChargePointId);

        // Use vendor profile timezone if set, otherwise UTC (OCPP standard)
        var vendorProfile = _vendorProfileFactory.Resolve(context.Connection.VendorProfileType);
        var tz = vendorProfile.ResponseTimezone ?? TimeZoneInfo.Utc;
        var responseTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

        var response = new HeartbeatResponse
        {
            CurrentTime = responseTime.ToString("o")
        };

        return context.Parser.SerializeCallResult(context.UniqueId, response);
    }
}
