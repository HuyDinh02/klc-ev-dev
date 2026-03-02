using System;
using System.Collections.Generic;
using System.Text;
using KLC.Localization;
using Volo.Abp.Application.Services;

namespace KLC;

/* Inherit your application services from this class.
 */
public abstract class KLCAppService : ApplicationService
{
    protected KLCAppService()
    {
        LocalizationResource = typeof(KLCResource);
    }
}
