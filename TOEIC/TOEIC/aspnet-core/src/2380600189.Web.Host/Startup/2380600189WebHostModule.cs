using Abp.Modules;
using Abp.Reflection.Extensions;
using TOEIC.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace TOEIC.Web.Host.Startup
{
    [DependsOn(
       typeof(TOEICWebCoreModule))]
    public class TOEICWebHostModule : AbpModule
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfigurationRoot _appConfiguration;

        public TOEICWebHostModule(IWebHostEnvironment env)
        {
            _env = env;
            _appConfiguration = env.GetAppConfiguration();
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(TOEICWebHostModule).GetAssembly());
        }
    }
}
