using Ida.Application.Common;
using Ida.Application.Features.IngestConfiguration;
using Ida.Application.Features.IngestMonitoring;
using Ida.Infrastructure.Databases;
using Ida.Infrastructure.Persistence;
using Ida.Infrastructure.Security;
using Ida.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Ida.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config, DatabaseRuntime runtime = DatabaseRuntime.Tenant)
    {
        if (runtime != DatabaseRuntime.Tenant) throw new ArgumentOutOfRangeException(nameof(runtime));
        services.AddCommonInfrastructure();
        services.AddSingleton(_ => new DatabaseRegistry(config, runtime));
        services.AddSingleton<ITenantContext, HttpTenantContext>();
        services.AddScoped<ICoreDirectory, HttpCoreDirectory>();
        services.AddScoped<DatabaseContexts>();
        services.AddScoped<IRuntimeDatabaseContexts>(provider => provider.GetRequiredService<DatabaseContexts>());
        services.AddScoped<ReferenceGuard>();
        services.AddScoped<IReferenceGuard>(provider => provider.GetRequiredService<ReferenceGuard>());
        services.AddScoped<IIngestConfigurationStore>(provider => new IngestConfigurationStore(GetRequestSource(provider), provider.GetRequiredService<ICoreDirectory>(), provider.GetRequiredService<DatabaseRegistry>()));
        services.AddScoped<IIngestMonitoringStore>(provider => new IngestMonitoringStore(GetRequestSource(provider), provider.GetRequiredService<ICoreDirectory>(), provider.GetRequiredService<DatabaseRegistry>()));
        services.AddScoped<IMockIngestRunner, MockIngestRunner>();
        return services;
    }

    private static NpgsqlDataSource GetRequestSource(IServiceProvider provider)
    {
        var contexts = provider.GetRequiredService<DatabaseContexts>();
        var registry = provider.GetRequiredService<DatabaseRegistry>();
        return registry.GetSource(contexts.BranchEndpoint);
    }
}
