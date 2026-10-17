using Abp.Application.Services;
using TOEIC.Authorization.Accounts.Dto;
using System.Threading.Tasks;

namespace TOEIC.Authorization.Accounts;

public interface IAccountAppService : IApplicationService
{
    Task<IsTenantAvailableOutput> IsTenantAvailable(IsTenantAvailableInput input);

    Task<RegisterOutput> Register(RegisterInput input);
}
