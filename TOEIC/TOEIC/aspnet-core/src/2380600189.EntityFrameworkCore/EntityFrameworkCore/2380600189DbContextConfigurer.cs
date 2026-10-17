using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace TOEIC.EntityFrameworkCore;

public static class TOEICDbContextConfigurer
{
    public static void Configure(DbContextOptionsBuilder<TOEICDbContext> builder, string connectionString)
    {
        builder.UseSqlServer(connectionString);
    }

    public static void Configure(DbContextOptionsBuilder<TOEICDbContext> builder, DbConnection connection)
    {
        builder.UseSqlServer(connection);
    }
}
