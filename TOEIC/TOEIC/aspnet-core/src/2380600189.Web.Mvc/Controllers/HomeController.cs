using Abp.AspNetCore.Mvc.Authorization;
using TOEIC.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace TOEIC.Web.Controllers;

[AbpMvcAuthorize]
public class HomeController : TOEICControllerBase
{
    public ActionResult Index()
    {
        return View();
    }
}
