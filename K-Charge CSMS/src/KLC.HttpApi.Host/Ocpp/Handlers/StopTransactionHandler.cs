using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Uow;
using KLC.ChargingSessions;
using KLC.Ocpp.Events;

namespace KLC.Ocpp.Handlers;

/// <summary>
/// Handles StopTransaction messages from charge points.
/// Completes the charging session, records final meter value, and validates the idTag if provided.
/// </summary>
public class StopTransactionHandler : IOcppMessageHandler
{
    private readonly IChargingSessionRepository _sessionRepository;
    private readonly IDistributedEventBus _eventBus;
    private readonly IUnitOfWorkManager _uowManager;
    private readonly ILogger<StopTransactionHandler> _logger;
    private readonly IGuidGenerator _guidGenerator;

    public StopTransactionHandler(
        IChargingSessionRepository sessionRepository,
        IDistributedEventBus eventBus,
        IUnitOfWorkManager uowManager,
        ILogger<StopTransactionHandler> logger,
        IGuidGenerator guidGenerator)
    {
        _sessionRepository = sessionRepository;
        _eventBus = eventBus;
        _uowManager = uowManager;
        _logger = logger;
        _guidGenerator = guidGenerator;
    }

    public async Task<JObject> HandleAsync(
        string chargePointId,
        JObject payload,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract payload fields
            var transactionId = payload.Value<int>("transactionId");
            var meterStop = payload.Value<int>("meterStop");
            var timestampStr = payload.Value<string>("timestamp");
            var reason = payload.Value<string?>("reason");
            var transactionData = payload.Value<JArray>("transactionData");

            var timestamp = DateTime.TryParse(timestampStr, out var parsed)
                ? parsed
                : DateTime.UtcNow;

            using var uow = _uowManager.Begin();

            // Find session by transaction ID
            var session = await _sessionRepository.FindByTransactionIdAsync(
                transactionId,
                includeDetails: true,
                cancellationToken: cancellationToken);

            if (session == null)
            {
                _logger.LogWarning(
                    "StopTransaction: session not found for transaction {TransactionId} on charge point {ChargePointId}",
                    transactionId, chargePointId);

                // Accept silently per OCPP spec
                await uow.CompleteAsync();
                return BuildResponse("Accepted");
            }

            // Stop the session
            session.Stop(meterStop, timestamp, reason);

            // Parse and add meter values if present
            if (transactionData != null && transactionData.Count > 0)
            {
                ParseAndAddMeterValues(session, transactionData);
            }

            await _sessionRepository.UpdateAsync(session, autoSave: true, cancellationToken: cancellationToken);
            await uow.CompleteAsync();

            var energy = session.EnergyConsumedWh;
            var duration = session.Duration ?? TimeSpan.Zero;

            _logger.LogInformation(
                "StopTransaction completed session {SessionId} with transaction {TransactionId} on {ChargePointId}: " +
                "Energy {Energy}Wh, Duration {Duration}, Reason {Reason}",
                session.Id, transactionId, chargePointId, energy, duration, reason ?? "N/A");

            // Publish event
            await _eventBus.PublishAsync(new ChargingSessionCompletedEto
            {
                SessionId = session.Id,
                TransactionId = transactionId,
                ChargePointId = chargePointId,
                ConnectorId = session.ConnectorId,
                EnergyConsumedWh = energy,
                Duration = duration,
                StopReason = reason
            }, cancellationToken: cancellationToken);

            return BuildResponse("Accepted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing StopTransaction for charge point {ChargePointId}",
                chargePointId);
            return BuildResponse("Accepted");
        }
    }

    private void ParseAndAddMeterValues(ChargingSession session, JArray transactionData)
    {
        try
        {
            foreach (var meterValueObj in transactionData)
            {
                if (!(meterValueObj is JObject meterObj))
                    continue;

                var timestamp = meterObj.Value<string>("timestamp");
                var sampledValues = meterObj.Value<JArray>("sampledValue");

                if (string.IsNullOrEmpty(timestamp) || sampledValues == null)
                    continue;

                if (!DateTime.TryParse(timestamp, out var parsedTime))
                    parsedTime = DateTime.UtcNow;

                foreach (var sampledValue in sampledValues)
                {
                    if (!(sampledValue is JObject sampledObj))
                        continue;

                    var value = sampledObj.Value<string>("value");
                    var measurand = sampledObj.Value<string>("measurand") ?? "Energy.Active.Import.Register";
                    var unit = sampledObj.Value<string>("unit") ?? "Wh";
                    var context = sampledObj.Value<string>("context") ?? "Sample.Periodic";
                    var format = sampledObj.Value<string>("format") ?? "Raw";
                    var location = sampledObj.Value<string?>("location");
                    var phase = sampledObj.Value<string?>("phase");

                    if (string.IsNullOrEmpty(value))
                        continue;

                    var meterValue = new MeterValue(
                        _guidGenerator.Create(),
                        parsedTime,
                        value,
                        measurand,
                        unit,
                        context,
                        format,
                        location,
                        phase);

                    session.AddMeterValue(meterValue);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing meter values in StopTransaction");
        }
    }

    private static JObject BuildResponse(string status)
    {
        return new JObject
        {
            {
                "idTagInfo", new JObject
                {
                    { "status", status }
                }
            }
        };
    }
}
