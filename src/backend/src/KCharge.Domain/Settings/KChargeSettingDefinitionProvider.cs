using Volo.Abp.Settings;

namespace KCharge.Settings;

public class KChargeSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(KChargeSettings.MySetting1));
    }
}
