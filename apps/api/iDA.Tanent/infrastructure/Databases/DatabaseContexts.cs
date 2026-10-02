using Ida.Application.Common;
using Ida.Infrastructure.Persistence;
using Ida.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;

namespace Ida.Infrastructure.Databases;

public sealed class DatabaseContexts(
    DatabaseRegistry registry,
    SecretsSaveChangesInterceptor secrets,
    AuditSaveChangesInterceptor audit) : IRuntimeDatabaseContexts, IAsyncDisposable
{
    private readonly Dictionary<string, IdaDbContext> _branches =
        new(StringComparer.OrdinalIgnoreCase);
    private IdaDbContext? _branch;
    private DatabaseEndpoint? _branchEndpoint;

    public IdaDbContext Branch =>
        _branch ?? throw new InvalidOperationException(
            "The BU database context has not been initialized for this request.");

    public DatabaseEndpoint BranchEndpoint =>
        _branchEndpoint ?? throw new InvalidOperationException(
            "The BU database endpoint has not been initialized for this request.");

    public IEnumerable<IdaDbContext> Created => _branches.Values;
    DbContext IRuntimeDatabaseContexts.Current => Branch;
    IEnumerable<DbContext> IRuntimeDatabaseContexts.Created => Created;

    public Task InitializeAsync(CancellationToken ct = default)
    {
        _branchEndpoint = registry.FixedBranch;
        _branch = ForBranch(_branchEndpoint);
        return Task.CompletedTask;
    }

    public IdaDbContext ForBranch(DatabaseEndpoint endpoint)
    {
        if (endpoint != registry.FixedBranch)
            throw new InvalidOperationException("This runtime cannot open the requested BU database.");
        if (!string.Equals(endpoint.Kind, "bu", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Database endpoint '{endpoint.ConnectionKey}' is not a BU endpoint.");

        if (_branches.TryGetValue(endpoint.ConnectionKey, out var existing)) return existing;

        var contextTenant = new FixedTenant(endpoint.HospitalId
            ?? throw new InvalidOperationException(
                $"BU database endpoint '{endpoint.ConnectionKey}' has no hospital id."));
        var context = CreateRuntimeContext(endpoint, contextTenant);
        _branches.Add(endpoint.ConnectionKey, context);
        return context;
    }

    public static IdaDbContext CreateSchemaContext(DatabaseEndpoint endpoint, string coreSchemaName, string connectionString)
    {
        var options = new DbContextOptionsBuilder<IdaDbContext>();
        options.UseNpgsql(connectionString, npgsql => npgsql
            .MapIdaEnums(endpoint.SchemaName)
            .MigrationsHistoryTable("__ef_migrations_history", endpoint.SchemaName));
        options.ReplaceService<IModelCacheKeyFactory, DatabaseModelCacheKeyFactory>();
        return new IdaDbContext(
            options.Options,
            new FixedTenant(endpoint.HospitalId),
            DatabaseLayout.For(endpoint, coreSchemaName));
    }

    private IdaDbContext CreateRuntimeContext(
        DatabaseEndpoint endpoint,
        ITenantContext contextTenant)
    {
        var options = new DbContextOptionsBuilder<IdaDbContext>();
        options.UseNpgsql(registry.GetSource(endpoint), npgsql => npgsql
            .MapIdaEnums(endpoint.SchemaName)
            .MigrationsHistoryTable("__ef_migrations_history", endpoint.SchemaName));
        options.ReplaceService<IModelCacheKeyFactory, DatabaseModelCacheKeyFactory>();
        options.ConfigureWarnings(warnings => warnings.Ignore(
            Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId
                .PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
        options.AddInterceptors(secrets, audit);
        return new IdaDbContext(
            options.Options,
            contextTenant,
            DatabaseLayout.For(endpoint, registry.CoreSchemaName));
    }

    public async ValueTask DisposeAsync()
    {
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
