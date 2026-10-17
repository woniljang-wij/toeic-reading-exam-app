using Abp.Application.Services;
using TOEIC.MultiTenancy.Dto;

namespace TOEIC.MultiTenancy;

public interface ITenantAppService : IAsyncCrudAppService<TenantDto, int, PagedTenantResultRequestDto, CreateTenantDto, TenantDto>
{
}

