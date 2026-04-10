using Volo.Abp.Settings;

namespace KLC.Settings;

public class KLCSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        // General
        context.Add(new SettingDefinition(KLCSettings.General.SiteName, "KLC CSMS", isVisibleToClients: true));
        context.Add(new SettingDefinition(KLCSettings.General.Timezone, "Asia/Ho_Chi_Minh", isVisibleToClients: true));
        context.Add(new SettingDefinition(KLCSettings.General.Currency, "VND", isVisibleToClients: true));
        context.Add(new SettingDefinition(KLCSettings.General.Language, "vi", isVisibleToClients: true));

        // Notifications
        context.Add(new SettingDefinition(KLCSettings.Notifications.EmailEnabled, "true"));
        context.Add(new SettingDefinition(KLCSettings.Notifications.SmsEnabled, "false"));
        context.Add(new SettingDefinition(KLCSettings.Notifications.PushEnabled, "true"));
        context.Add(new SettingDefinition(KLCSettings.Notifications.AlertEmail, "admin@klc.vn"));

        // OCPP
        context.Add(new SettingDefinition(KLCSettings.Ocpp.WebSocketPort, "5002"));
        context.Add(new SettingDefinition(KLCSettings.Ocpp.HeartbeatInterval, "60"));
        context.Add(new SettingDefinition(KLCSettings.Ocpp.MeterValueInterval, "30"));
        context.Add(new SettingDefinition(KLCSettings.Ocpp.ClockAlignedDataInterval, "0"));
        context.Add(new SettingDefinition(KLCSettings.Ocpp.MeterValuesSampledData, "Energy.Active.Import.Register,Current.Import,Voltage,Power.Active.Import,SoC"));
        context.Add(new SettingDefinition(KLCSettings.Ocpp.StopTxnSampledData, "Energy.Active.Import.Register,Current.Import,Voltage,Power.Active.Import,SoC"));
        context.Add(new SettingDefinition(KLCSettings.Ocpp.StopTransactionOnEVSideDisconnect, "true"));
        context.Add(new SettingDefinition(KLCSettings.Ocpp.AutoConfigOnBoot, "true"));
        context.Add(new SettingDefinition(KLCSettings.Ocpp.AutoRegisterStations, "false"));

        // Payments
        context.Add(new SettingDefinition(KLCSettings.Payments.DefaultGateway, "VNPay"));
        context.Add(new SettingDefinition(KLCSettings.Payments.AutoInvoiceGeneration, "true"));
        context.Add(new SettingDefinition(KLCSettings.Payments.EInvoiceProvider, "MISA"));

        // Security
        context.Add(new SettingDefinition(KLCSettings.Security.SessionTimeout, "30"));
        context.Add(new SettingDefinition(KLCSettings.Security.RequireMfa, "false"));
        context.Add(new SettingDefinition(KLCSettings.Security.PasswordMinLength, "8"));

        // Wallet
        context.Add(new SettingDefinition(KLCSettings.Wallet.MinBalanceToStart, "50000"));
        context.Add(new SettingDefinition(KLCSettings.Wallet.AutoStopThreshold, "20000"));
        context.Add(new SettingDefinition(KLCSettings.Wallet.MonitorIntervalSeconds, "30"));
    }
}
