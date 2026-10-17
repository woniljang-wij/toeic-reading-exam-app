using Abp.Modules;
using Abp.Reflection.Extensions;
using Backend.Controllers;
using Castle.MicroKernel.Registration;
using TOEIC.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace TOEIC.Web.Startup;

[DependsOn(typeof(TOEICWebCoreModule))]
public class TOEICWebMvcModule : AbpModule
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfigurationRoot _appConfiguration;

    public TOEICWebMvcModule(IWebHostEnvironment env)
    {
        _env = env;
        _appConfiguration = env.GetAppConfiguration();
    }

    public override void PreInitialize()
    {
        Configuration.Navigation.Providers.Add<TOEICNavigationProvider>();
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(TOEICWebMvcModule).GetAssembly());
        IocManager.IocContainer.Register(
            Classes.FromAssembly(typeof(ExamsController).Assembly)
                .BasedOn<ControllerBase>()
                .LifestyleTransient());
    }
}
