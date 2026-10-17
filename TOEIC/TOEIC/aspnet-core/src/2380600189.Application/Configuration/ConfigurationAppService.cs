using Abp.Authorization;
using Abp.Runtime.Session;
using TOEIC.Configuration.Dto;
using System.Threading.Tasks;

namespace TOEIC.Configuration;

[AbpAuthorize]
public class ConfigurationAppService : TOEICAppServiceBase, IConfigurationAppService
{
    public async Task ChangeUiTheme(ChangeUiThemeInput input)
    {
        await SettingManager.ChangeSettingForUserAsync(AbpSession.ToUserIdentifier(), AppSettingNames.UiTheme, input.Theme);
    }
}
