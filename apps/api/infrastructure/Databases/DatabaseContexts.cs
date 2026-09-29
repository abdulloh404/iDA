using Ida.Application.Common;
using Ida.Infrastructure.Persistence;
using Ida.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;

namespace Ida.Infrastructure.Databases;

public sealed class DatabaseContexts(
    DatabaseRegistry registry,
    ITenantContext tenant,
    SecretsSaveChangesInterceptor secrets,
    AuditSaveChangesInterceptor audit) : IAsyncDisposable
{
    private readonly Dictionary<string, IdaDbContext> _branches =
        new(StringComparer.OrdinalIgnoreCase);
    private IdaDbContext? _core;
    private IdaDbContext? _branch;
    private DatabaseEndpoint? _branchEndpoint;

    public IdaDbContext Core =>
        _core ??= CreateRuntimeContext(registry.Core, registry.Core, tenant);

    public IdaDbContext Branch =>
        _branch ?? throw new InvalidOperationException(
            "The BU database context has not been initialized for this request.");

    public DatabaseEndpoint BranchEndpoint =>
        _branchEndpoint ?? throw new InvalidOperationException(
            "The BU database endpoint has not been initialized for this request.");

    public IEnumerable<IdaDbContext> Created
    {
        get
        {
            if (_core is not null) yield return _core;
            foreach (var context in _branches.Values) yield return context;
        }
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (!tenant.HasTenant) return;
        var endpoint = await registry.GetBranchAsync(tenant.HospitalId, ct);
        _branchEndpoint = endpoint;
        _branch = ForBranch(endpoint);
    }

    public IdaDbContext ForBranch(DatabaseEndpoint endpoint)
    {
        if (!string.Equals(endpoint.Kind, "bu", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Database endpoint '{endpoint.Code}' is not a BU endpoint.");

        if (_branches.TryGetValue(endpoint.Code, out var existing)) return existing;

        var contextTenant = new FixedTenant(endpoint.HospitalId
            ?? throw new InvalidOperationException(
                $"BU database endpoint '{endpoint.Code}' has no hospital id."));
        var context = CreateRuntimeContext(endpoint, registry.Core, contextTenant);
        _branches.Add(endpoint.Code, context);
        return context;
    }

    public static IdaDbContext CreateSchemaContext(
        DatabaseEndpoint endpoint,
        DatabaseEndpoint core,
        string connectionString)
    {
        var options = new DbContextOptionsBuilder<IdaDbContext>();
        options.UseNpgsql(connectionString, npgsql => npgsql
            .MapIdaEnums(core.SchemaName)
            .MigrationsHistoryTable("__ef_migrations_history", endpoint.SchemaName));
        options.ReplaceService<IModelCacheKeyFactory, DatabaseModelCacheKeyFactory>();
        return new IdaDbContext(
            options.Options,
            new FixedTenant(endpoint.HospitalId),
            DatabaseLayout.For(endpoint, core));
    }

    private IdaDbContext CreateRuntimeContext(
        DatabaseEndpoint endpoint,
        DatabaseEndpoint core,
        ITenantContext contextTenant)
    {
        var options = new DbContextOptionsBuilder<IdaDbContext>();
        options.UseNpgsql(registry.GetSource(endpoint), npgsql => npgsql
            .MapIdaEnums(core.SchemaName)
            .MigrationsHistoryTable("__ef_migrations_history", endpoint.SchemaName));
        options.ReplaceService<IModelCacheKeyFactory, DatabaseModelCacheKeyFactory>();
        options.ConfigureWarnings(warnings => warnings.Ignore(
            Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId
                .PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
        options.AddInterceptors(
            secrets,
            audit,
            new TenantConnectionInterceptor(contextTenant));
        return new IdaDbContext(
            options.Options,
            contextTenant,
            DatabaseLayout.For(endpoint, core));
    }

    public async ValueTask DisposeAsync()
    {
        if (_core is not null) await _core.DisposeAsync();
        foreach (var context in _branches.Values) await context.DisposeAsync();
    }

    private sealed class FixedTenant(string? hospitalId) : ITenantContext
    {
        public string HospitalId { get; } = hospitalId ?? string.Empty;
        public bool HasTenant => HospitalId.Length > 0;
        public IReadOnlyCollection<string> AllowedHospitals =>
            HasTenant ? [HospitalId] : [];
    }
}
