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
/// Handles MeterValues messages from charge points.
/// Records energy consumption, power, current, voltage, and SoC readings.
/// </summary>
public class MeterValuesHandler : IOcppMessageHandler
{
    private readonly IChargingSessionRepository _sessionRepository;
    private readonly IDistributedEventBus _eventBus;
    private readonly IUnitOfWorkManager _uowManager;
    private readonly ILogger<MeterValuesHandler> _logger;
    private readonly IGuidGenerator _guidGenerator;

    public MeterValuesHandler(
        IChargingSessionRepository sessionRepository,
        IDistributedEventBus eventBus,
        IUnitOfWorkManager uowManager,
        ILogger<MeterValuesHandler> logger,
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
            var connectorId = payload.Value<int>("connectorId");
            var transactionId = payload.Value<int?>("transactionId");
            var meterValues = payload.Value<JArray>("meterValue");

            if (!transactionId.HasValue || meterValues == null || meterValues.Count == 0)
            {
                _logger.LogDebug(
                    "MeterValues with no transaction ID or empty meter values for charge point {ChargePointId}, connector {ConnectorId}",
                    chargePointId, connectorId);
                return new JObject();
            }

            using var uow = _uowManager.Begin();

            // Find active session
            var session = await _sessionRepository.FindByTransactionIdAsync(
                transactionId.Value,
                includeDetails: true,
                cancellationToken: cancellationToken);

            if (session == null)
            {
                _logger.LogWarning(
                    "MeterValues: session not found for transaction {TransactionId} on charge point {ChargePointId}",
                    transactionId, chargePointId);
                await uow.CompleteAsync();
                return new JObject();
            }

            // Parse meter values and add to session
            var metrics = ParseAndAddMeterValues(session, meterValues);

            await _sessionRepository.UpdateAsync(session, autoSave: true, cancellationToken: cancellationToken);
            await uow.CompleteAsync();

            _logger.LogDebug(
                "MeterValues processed for transaction {TransactionId} on {ChargePointId}, connector {ConnectorId}: " +
                "Energy {Energy}Wh, Power {Power}W, Current {Current}A, Voltage {Voltage}V, SoC {SoC}%",
                transactionId, chargePointId, connectorId,
                metrics.Energy, metrics.Power, metrics.Current, metrics.Voltage, metrics.SoC);

            // Publish event with key metrics
            await _eventBus.PublishAsync(new MeterValueReceivedEto
            {
                ChargePointId = chargePointId,
                ConnectorId = connectorId,
                TransactionId = transactionId.Value,
                Timestamp = DateTime.UtcNow,
                EnergyWh = metrics.Energy,
                PowerW = metrics.Power,
                CurrentA = metrics.Current,
                VoltageV = metrics.Voltage,
                SoCPercent = metrics.SoC
            }, cancellationToken: cancellationToken);

            return new JObject();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MeterValues for charge point {ChargePointId}",
                chargePointId);
            return new JObject();
        }
    }

    private MeterMetrics ParseAndAddMeterValues(ChargingSession session, JArray meterValues)
    {
        var metrics = new MeterMetrics();

        foreach (var meterValueObj in meterValues)
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

                // Create and add meter value
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

                // Extract metrics
                if (decimal.TryParse(value, out var numValue))
                {
                    switch (measurand.ToLowerInvariant())
                    {
                        case "energy.active.import.register":
                            if (unit?.ToLowerInvariant() == "wh")
                                metrics.Energy = numValue;
                            break;

                        case "power.active.import":
                            if (unit?.ToLowerInvariant() == "w")
                                metrics.Power = numValue;
                            break;

                        case "current.import":
                            if (unit?.ToLowerInvariant() == "a")
                                metrics.Current = numValue;
                            break;

                        case "voltage":
                            if (unit?.ToLowerInvariant() == "v")
                                metrics.Voltage = numValue;
                            break;

                        case "soc":
                            if (unit?.ToLowerInvariant() == "%")
                                metrics.SoC = numValue;
                            break;
                    }
                }
            }
        }

        return metrics;
    }

    private class MeterMetrics
    {
        public decimal? Energy { get; set; }
        public decimal? Power { get; set; }
        public decimal? Current { get; set; }
        public decimal? Voltage { get; set; }
        public decimal? SoC { get; set; }
    }
}
