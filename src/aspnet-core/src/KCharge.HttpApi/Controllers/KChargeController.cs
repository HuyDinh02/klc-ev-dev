using KCharge.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace KCharge.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class KChargeController : AbpControllerBase
{
    protected KChargeController()
    {
        LocalizationResource = typeof(KChargeResource);
    }
}
