using Abp.AspNetCore.Mvc.Views;
using Abp.Runtime.Session;
using Microsoft.AspNetCore.Mvc.Razor.Internal;

namespace TOEIC.Web.Views;

public abstract class TOEICRazorPage<TModel> : AbpRazorPage<TModel>
{
    [RazorInject]
    public IAbpSession AbpSession { get; set; }

    protected TOEICRazorPage()
    {
        LocalizationSourceName = TOEICConsts.LocalizationSourceName;
    }
}
