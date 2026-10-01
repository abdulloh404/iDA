using Ida.Application.Common;
using Ida.Infrastructure.Databases;
using Ida.Infrastructure.Persistence;
using Ida.Infrastructure.Security;
using Ida.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ida.Infrastructure;

public static class DependencyInjection
{

    public static IServiceCollection AddInfrastructure(this IServiceCollection services,
        IConfiguration config, DatabaseRuntime runtime = DatabaseRuntime.Core)
    {
        services.AddCommonInfrastructure();
        services.AddSingleton<ITenantContext, HttpTenantContext>();

        services.AddSingleton(_ => new DatabaseRegistry(config, runtime));
        services.AddSingleton<TenantApiDirectory>();
        services.AddSingleton<ITenantApiDirectory>(provider => provider.GetRequiredService<TenantApiDirectory>());
        services.AddScoped<ICoreDirectory, CoreDirectory>();
        services.AddScoped<DatabaseContexts>();
        services.AddScoped<IRuntimeDatabaseContexts>(provider => provider.GetRequiredService<DatabaseContexts>());

        services.AddScoped<IDatabaseErrorMapper, CoreDatabaseErrorMapper>();

        services.AddScoped<ReferenceGuard>();
        services.AddScoped<IReferenceGuard>(provider => provider.GetRequiredService<ReferenceGuard>());
        services.AddScoped<IUserActivity>(provider =>
            new UserActivity(provider.GetRequiredService<DatabaseContexts>().Core));
        services.AddScoped<DatabaseSeeder>();
        return services;
    }
}
