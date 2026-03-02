using KLC.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace KLC.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class KLCController : AbpControllerBase
{
    protected KLCController()
    {
        LocalizationResource = typeof(KLCResource);
    }
}
