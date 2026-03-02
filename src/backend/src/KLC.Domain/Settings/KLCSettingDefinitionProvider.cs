using Volo.Abp.Settings;

namespace KLC.Settings;

public class KLCSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(KLCSettings.MySetting1));
    }
}
