using System.Threading.Tasks;
using KLC.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings;

namespace KLC.Controllers;

[Authorize]
[Route("api/v1/settings")]
public class SettingsController : KLCController
{
    private readonly ISettingProvider _settingProvider;
    private readonly ISettingManager _settingManager;

    public SettingsController(ISettingProvider settingProvider, ISettingManager settingManager)
    {
        _settingProvider = settingProvider;
        _settingManager = settingManager;
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
    public async Task UpdateAsync([FromBody] SystemSettingsDto input)
    {
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
