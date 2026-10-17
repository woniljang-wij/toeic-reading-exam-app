using Abp.Events.Bus;
using Abp.Modules;
using Abp.Reflection.Extensions;
using TOEIC.Configuration;
using TOEIC.EntityFrameworkCore;
using TOEIC.Migrator.DependencyInjection;
using Castle.MicroKernel.Registration;
using Microsoft.Extensions.Configuration;

namespace TOEIC.Migrator;

[DependsOn(typeof(TOEICEntityFrameworkModule))]
public class TOEICMigratorModule : AbpModule
{
    private readonly IConfigurationRoot _appConfiguration;

    public TOEICMigratorModule(TOEICEntityFrameworkModule abpProjectNameEntityFrameworkModule)
    {
        abpProjectNameEntityFrameworkModule.SkipDbSeed = true;

        _appConfiguration = AppConfigurations.Get(
            typeof(TOEICMigratorModule).GetAssembly().GetDirectoryPathOrNull()
        );
    }

    public override void PreInitialize()
    {
        Configuration.DefaultNameOrConnectionString = _appConfiguration.GetConnectionString(
            TOEICConsts.ConnectionStringName
        );

        Configuration.BackgroundJobs.IsJobExecutionEnabled = false;
        Configuration.ReplaceService(
            typeof(IEventBus),
            () => IocManager.IocContainer.Register(
                Component.For<IEventBus>().Instance(NullEventBus.Instance)
            )
        );
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(TOEICMigratorModule).GetAssembly());
        ServiceCollectionRegistrar.Register(IocManager);
    }
}
