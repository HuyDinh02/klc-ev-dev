using Microsoft.Extensions.Localization;
using KLC.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace KLC;

[Dependency(ReplaceServices = true)]
public class KLCBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<KLCResource> _localizer;

    public KLCBrandingProvider(IStringLocalizer<KLCResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
