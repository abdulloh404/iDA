using Ida.Application.Common;
using Ida.Infrastructure.Persistence;
using Ida.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Ida.Infrastructure.Databases;

public sealed class DatabaseContexts(
    DatabaseRegistry registry,
    ITenantContext tenant,
    SecretsSaveChangesInterceptor secrets,
    AuditSaveChangesInterceptor audit) : IRuntimeDatabaseContexts, IAsyncDisposable
{
    private IdaDbContext? _core;

    public IdaDbContext Core => _core ??= CreateRuntimeContext();

    DbContext IRuntimeDatabaseContexts.Current => Core;
    IEnumerable<DbContext> IRuntimeDatabaseContexts.Created => Created;

    public IEnumerable<IdaDbContext> Created
    {
        get
        {
            if (_core is not null) yield return _core;
        }
    }

    public static IdaDbContext CreateSchemaContext(DatabaseEndpoint endpoint, string connectionString)
    {
        if (endpoint.Kind != "core") throw new InvalidOperationException("The Core service cannot create a BU database context.");
        var options = new DbContextOptionsBuilder<IdaDbContext>();
        options.UseNpgsql(connectionString, npgsql => npgsql
            .MapIdaEnums(endpoint.SchemaName)
            .MigrationsHistoryTable("__ef_migrations_history", endpoint.SchemaName));
        options.ReplaceService<IModelCacheKeyFactory, DatabaseModelCacheKeyFactory>();
        return new IdaDbContext(options.Options, new NoTenant(), DatabaseLayout.Core(endpoint.SchemaName));
    }

    private IdaDbContext CreateRuntimeContext()
    {
        var options = new DbContextOptionsBuilder<IdaDbContext>();
        options.UseNpgsql(registry.CoreSource, npgsql => npgsql
            .MapIdaEnums(registry.Core.SchemaName)
            .MigrationsHistoryTable("__ef_migrations_history", registry.Core.SchemaName));
        options.ReplaceService<IModelCacheKeyFactory, DatabaseModelCacheKeyFactory>();
        options.ConfigureWarnings(warnings => warnings.Ignore(
            Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
        options.AddInterceptors(secrets, audit);
        return new IdaDbContext(options.Options, tenant, DatabaseLayout.Core(registry.Core.SchemaName));
    }

    public async ValueTask DisposeAsync()
    {
        if (_core is not null) await _core.DisposeAsync();
    }

    private sealed class NoTenant : ITenantContext
    {
        public string HospitalId => string.Empty;
        public bool HasTenant => false;
        public IReadOnlyCollection<string> AllowedHospitals => [];
    }
}
