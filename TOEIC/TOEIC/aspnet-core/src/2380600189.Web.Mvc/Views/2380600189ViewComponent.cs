using Abp.AspNetCore.Mvc.ViewComponents;

namespace TOEIC.Web.Views;

public abstract class TOEICViewComponent : AbpViewComponent
{
    protected TOEICViewComponent()
    {
        LocalizationSourceName = TOEICConsts.LocalizationSourceName;
    }
}
