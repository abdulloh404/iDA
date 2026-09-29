using Ida.Application.Common;
using Ida.Infrastructure.Databases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace Ida.Infrastructure.Persistence;

public class IdaDbContextFactory : IDesignTimeDbContextFactory<IdaDbContext>
{
    public IdaDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            config.GetConnectionString("PostgresMigration")
            ?? config["CORE_DB_ADMIN_CONNECTION"]
            ?? config["CORE_DB_CONNECTION"]
            ?? config.GetConnectionString("Postgres")
            ?? "Host=localhost;Port=5432;Database=appdb;Username=postgres";
        var schema = config["CORE_DB_SCHEMA"] ?? IdaDbContext.CoreSchema;

        var builder = new DbContextOptionsBuilder<IdaDbContext>();
        builder.UseNpgsql(connectionString, npgsql => npgsql
            .MapIdaEnums(schema)
            .MigrationsHistoryTable("__ef_migrations_history", schema));
        builder.ReplaceService<
            Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory,
            DatabaseModelCacheKeyFactory>();

        return new IdaDbContext(
            builder.Options,
            new NoTenant(),
            DatabaseLayout.Core(schema));
    }

    private sealed class NoTenant : ITenantContext
    {
        public string HospitalId => string.Empty;
        public bool HasTenant => false;
        public IReadOnlyCollection<string> AllowedHospitals => [];
    }
}
