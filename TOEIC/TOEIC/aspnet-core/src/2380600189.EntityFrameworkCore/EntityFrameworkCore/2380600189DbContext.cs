using Abp.Zero.EntityFrameworkCore;
using TOEIC.Authorization.Roles;
using TOEIC.Authorization.Users;
using TOEIC.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace TOEIC.EntityFrameworkCore;

public class TOEICDbContext : AbpZeroDbContext<Tenant, Role, User, TOEICDbContext>
{
    /* Define a DbSet for each entity of the application */

    public TOEICDbContext(DbContextOptions<TOEICDbContext> options)
        : base(options)
    {
    }
}
