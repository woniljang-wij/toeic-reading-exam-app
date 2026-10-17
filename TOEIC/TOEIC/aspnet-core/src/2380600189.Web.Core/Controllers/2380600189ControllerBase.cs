using Abp.AspNetCore.Mvc.Controllers;
using Abp.IdentityFramework;
using Microsoft.AspNetCore.Identity;

namespace TOEIC.Controllers
{
    public abstract class TOEICControllerBase : AbpController
    {
        protected TOEICControllerBase()
        {
            LocalizationSourceName = TOEICConsts.LocalizationSourceName;
        }

        protected void CheckErrors(IdentityResult identityResult)
        {
            identityResult.CheckErrors(LocalizationManager);
        }
    }
}
