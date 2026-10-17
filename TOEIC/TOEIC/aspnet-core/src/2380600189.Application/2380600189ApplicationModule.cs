using Abp.AutoMapper;
using Abp.Modules;
using Abp.Reflection.Extensions;
using TOEIC.Authorization;

namespace TOEIC;

[DependsOn(
    typeof(TOEICCoreModule),
    typeof(AbpAutoMapperModule))]
public class TOEICApplicationModule : AbpModule
{
    public override void PreInitialize()
    {
        Configuration.Authorization.Providers.Add<TOEICAuthorizationProvider>();
    }

    public override void Initialize()
    {
        var thisAssembly = typeof(TOEICApplicationModule).GetAssembly();

        IocManager.RegisterAssemblyByConvention(thisAssembly);

        Configuration.Modules.AbpAutoMapper().Configurators.Add(
            // Scan the assembly for classes which inherit from AutoMapper.Profile
            cfg => cfg.AddMaps(thisAssembly)
        );
    }
}
