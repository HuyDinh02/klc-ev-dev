using Microsoft.Extensions.Localization;
using KCharge.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace KCharge;

[Dependency(ReplaceServices = true)]
public class KChargeBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<KChargeResource> _localizer;

    public KChargeBrandingProvider(IStringLocalizer<KChargeResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
