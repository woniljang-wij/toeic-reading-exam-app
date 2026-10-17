using Abp.Application.Services;
using TOEIC.Sessions.Dto;
using System.Threading.Tasks;

namespace TOEIC.Sessions;

public interface ISessionAppService : IApplicationService
{
    Task<GetCurrentLoginInformationsOutput> GetCurrentLoginInformations();
}
