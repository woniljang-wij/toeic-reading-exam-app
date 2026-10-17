using TOEIC.Configuration.Dto;
using System.Threading.Tasks;

namespace TOEIC.Configuration;

public interface IConfigurationAppService
{
    Task ChangeUiTheme(ChangeUiThemeInput input);
}
