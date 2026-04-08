using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using KLC.Ocpp;
using KLC.Permissions;
using KLC.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings;

namespace KLC.Controllers;

[Authorize(KLCPermissions.Settings.Default)]
[Route("api/v1/settings")]
public class SettingsController : KLCController
{
    private readonly ISettingProvider _settingProvider;
    private readonly ISettingManager _settingManager;
    private readonly OcppConnectionManager _connectionManager;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OcppRedisCommandBridge _redisCommandBridge;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        ISettingProvider settingProvider,
        ISettingManager settingManager,
        OcppConnectionManager connectionManager,
        IServiceScopeFactory scopeFactory,
        OcppRedisCommandBridge redisCommandBridge,
        ILogger<SettingsController> logger)
    {
        _settingProvider = settingProvider;
        _settingManager = settingManager;
        _connectionManager = connectionManager;
        _scopeFactory = scopeFactory;
        _redisCommandBridge = redisCommandBridge;
        _logger = logger;
    }

    [HttpGet]
    public async Task<SystemSettingsDto> GetAsync()
    {
        return new SystemSettingsDto
        {
            SiteName = await _settingProvider.GetOrNullAsync(KLCSettings.General.SiteName) ?? "KLC CSMS",
            Timezone = await _settingProvider.GetOrNullAsync(KLCSettings.General.Timezone) ?? "Asia/Ho_Chi_Minh",
            Currency = await _settingProvider.GetOrNullAsync(KLCSettings.General.Currency) ?? "VND",
            Language = await _settingProvider.GetOrNullAsync(KLCSettings.General.Language) ?? "vi",

            EmailNotifications = bool.Parse(await _settingProvider.GetOrNullAsync(KLCSettings.Notifications.EmailEnabled) ?? "true"),
            SmsNotifications = bool.Parse(await _settingProvider.GetOrNullAsync(KLCSettings.Notifications.SmsEnabled) ?? "false"),
            PushNotifications = bool.Parse(await _settingProvider.GetOrNullAsync(KLCSettings.Notifications.PushEnabled) ?? "true"),
            AlertEmail = await _settingProvider.GetOrNullAsync(KLCSettings.Notifications.AlertEmail) ?? "",

            OcppWebSocketPort = int.Parse(await _settingProvider.GetOrNullAsync(KLCSettings.Ocpp.WebSocketPort) ?? "5002"),
            OcppHeartbeatInterval = int.Parse(await _settingProvider.GetOrNullAsync(KLCSettings.Ocpp.HeartbeatInterval) ?? "60"),
            OcppMeterValueInterval = int.Parse(await _settingProvider.GetOrNullAsync(KLCSettings.Ocpp.MeterValueInterval) ?? "30"),

            DefaultPaymentGateway = await _settingProvider.GetOrNullAsync(KLCSettings.Payments.DefaultGateway) ?? "VNPay",
            AutoInvoiceGeneration = bool.Parse(await _settingProvider.GetOrNullAsync(KLCSettings.Payments.AutoInvoiceGeneration) ?? "true"),
            EInvoiceProvider = await _settingProvider.GetOrNullAsync(KLCSettings.Payments.EInvoiceProvider) ?? "MISA",

            SessionTimeout = int.Parse(await _settingProvider.GetOrNullAsync(KLCSettings.Security.SessionTimeout) ?? "30"),
            RequireMfa = bool.Parse(await _settingProvider.GetOrNullAsync(KLCSettings.Security.RequireMfa) ?? "false"),
            PasswordMinLength = int.Parse(await _settingProvider.GetOrNullAsync(KLCSettings.Security.PasswordMinLength) ?? "8"),
        };
    }

    [HttpPut]
    [Authorize(KLCPermissions.Settings.Update)]
    public async Task UpdateAsync([FromBody] SystemSettingsDto input)
    {
        // Server-side validation
        if (input.OcppHeartbeatInterval < 10 || input.OcppHeartbeatInterval > 3600)
            throw new ArgumentException("Heartbeat interval must be between 10 and 3600 seconds.");
        if (input.OcppMeterValueInterval < 5 || input.OcppMeterValueInterval > 3600)
            throw new ArgumentException("Meter value interval must be between 5 and 3600 seconds.");
        if (input.OcppWebSocketPort < 1 || input.OcppWebSocketPort > 65535)
            throw new ArgumentException("WebSocket port must be between 1 and 65535.");
        if (input.SessionTimeout < 5 || input.SessionTimeout > 1440)
            throw new ArgumentException("Session timeout must be between 5 and 1440 minutes.");
        if (input.PasswordMinLength < 6 || input.PasswordMinLength > 24)
            throw new ArgumentException("Password minimum length must be between 6 and 24.");

        await _settingManager.SetGlobalAsync(KLCSettings.General.SiteName, input.SiteName);
        await _settingManager.SetGlobalAsync(KLCSettings.General.Timezone, input.Timezone);
        await _settingManager.SetGlobalAsync(KLCSettings.General.Currency, input.Currency);
        await _settingManager.SetGlobalAsync(KLCSettings.General.Language, input.Language);

        await _settingManager.SetGlobalAsync(KLCSettings.Notifications.EmailEnabled, input.EmailNotifications.ToString().ToLower());
        await _settingManager.SetGlobalAsync(KLCSettings.Notifications.SmsEnabled, input.SmsNotifications.ToString().ToLower());
        await _settingManager.SetGlobalAsync(KLCSettings.Notifications.PushEnabled, input.PushNotifications.ToString().ToLower());
        await _settingManager.SetGlobalAsync(KLCSettings.Notifications.AlertEmail, input.AlertEmail);

        await _settingManager.SetGlobalAsync(KLCSettings.Ocpp.WebSocketPort, input.OcppWebSocketPort.ToString());
        await _settingManager.SetGlobalAsync(KLCSettings.Ocpp.HeartbeatInterval, input.OcppHeartbeatInterval.ToString());
        await _settingManager.SetGlobalAsync(KLCSettings.Ocpp.MeterValueInterval, input.OcppMeterValueInterval.ToString());

        await _settingManager.SetGlobalAsync(KLCSettings.Payments.DefaultGateway, input.DefaultPaymentGateway);
        await _settingManager.SetGlobalAsync(KLCSettings.Payments.AutoInvoiceGeneration, input.AutoInvoiceGeneration.ToString().ToLower());
        await _settingManager.SetGlobalAsync(KLCSettings.Payments.EInvoiceProvider, input.EInvoiceProvider);

        await _settingManager.SetGlobalAsync(KLCSettings.Security.SessionTimeout, input.SessionTimeout.ToString());
        await _settingManager.SetGlobalAsync(KLCSettings.Security.RequireMfa, input.RequireMfa.ToString().ToLower());
        await _settingManager.SetGlobalAsync(KLCSettings.Security.PasswordMinLength, input.PasswordMinLength.ToString());
    }

    /// <summary>
    /// Push current OCPP configuration to all online chargers.
    /// Sends ChangeConfiguration commands for each managed OCPP key.
    /// </summary>
    [HttpPost("apply-to-chargers")]
    [Authorize(KLCPermissions.Settings.Update)]
    public async Task<ApplyToChargersResultDto> ApplyToChargersAsync()
    {
        var connections = _connectionManager.GetAllConnections();
        var results = new List<ApplyToChargerItemDto>();
        var successCount = 0;
        var failCount = 0;

        foreach (var connection in connections)
        {
            if (!connection.IsRegistered || !connection.StationId.HasValue)
                continue;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var configService = scope.ServiceProvider.GetRequiredService<OcppPostBootConfigService>();
                await configService.SendPostBootConfigurationAsync(connection);

                results.Add(new ApplyToChargerItemDto
                {
                    ChargePointId = connection.ChargePointId,
                    Success = true,
                    Message = "Configuration sent successfully"
                });
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to push config to {ChargePointId}", connection.ChargePointId);
                results.Add(new ApplyToChargerItemDto
                {
                    ChargePointId = connection.ChargePointId,
                    Success = false,
                    Message = ex.Message
                });
                failCount++;
            }
        }

        // Also try Redis pub/sub for chargers on other instances
        if (_redisCommandBridge.IsAvailable)
        {
            _logger.LogInformation("APPLY_CONFIG: Redis bridge available — chargers on other instances will receive config on next BootNotification");
        }

        _logger.LogInformation(
            "APPLY_CONFIG: Pushed config to {Total} chargers — {Success} succeeded, {Fail} failed",
            successCount + failCount, successCount, failCount);

        return new ApplyToChargersResultDto
        {
            TotalChargers = successCount + failCount,
            SuccessCount = successCount,
            FailCount = failCount,
            Results = results
        };
    }
}

public class ApplyToChargersResultDto
{
    public int TotalChargers { get; set; }
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public List<ApplyToChargerItemDto> Results { get; set; } = new();
}

public class ApplyToChargerItemDto
{
    public string ChargePointId { get; set; } = "";
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}

public class SystemSettingsDto
{
    public string SiteName { get; set; } = "KLC CSMS";
    public string Timezone { get; set; } = "Asia/Ho_Chi_Minh";
    public string Currency { get; set; } = "VND";
    public string Language { get; set; } = "vi";

    public bool EmailNotifications { get; set; } = true;
    public bool SmsNotifications { get; set; }
    public bool PushNotifications { get; set; } = true;
    public string AlertEmail { get; set; } = "";

    public int OcppWebSocketPort { get; set; } = 5002;
    public int OcppHeartbeatInterval { get; set; } = 60;
    public int OcppMeterValueInterval { get; set; } = 30;

    public string DefaultPaymentGateway { get; set; } = "VNPay";
    public bool AutoInvoiceGeneration { get; set; } = true;
    public string EInvoiceProvider { get; set; } = "MISA";

    public int SessionTimeout { get; set; } = 30;
    public bool RequireMfa { get; set; }
    public int PasswordMinLength { get; set; } = 8;
}
