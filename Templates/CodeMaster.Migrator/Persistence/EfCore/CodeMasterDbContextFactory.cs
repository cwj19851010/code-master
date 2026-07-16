using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CodeMaster.Migrator.Persistence.EfCore;

/// <summary>
/// EF Core 设计时工厂（用于迁移命令）
/// </summary>
public class CodeMasterDbContextFactory : IDesignTimeDbContextFactory<CodeMasterDbContext>
{
    public CodeMasterDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<CodeMasterDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=127.0.0.1;Port=3306;Database=CodeMasterDB;User ID=root;Password=your_mysql_password;Character Set=utf8mb4;SslMode=None;AllowPublicKeyRetrieval=True;";

#if DB_MYSQL
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
#elif DB_PGSQL
        optionsBuilder.UseNpgsql(connectionString);
#elif DB_SQLITE
        optionsBuilder.UseSqlite(connectionString);
#elif DB_ORACLE
        optionsBuilder.UseOracle(connectionString);
#else
        optionsBuilder.UseSqlServer(connectionString);
#endif

        return new CodeMasterDbContext(optionsBuilder.Options);
    }
}
