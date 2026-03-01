using System;
using System.Collections.Generic;
using System.Text;
using KCharge.Localization;
using Volo.Abp.Application.Services;

namespace KCharge;

/* Inherit your application services from this class.
 */
public abstract class KChargeAppService : ApplicationService
{
    protected KChargeAppService()
    {
        LocalizationResource = typeof(KChargeResource);
    }
}
