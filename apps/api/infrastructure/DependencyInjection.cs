using Ida.Application.Common;
using Ida.Domain.Common;
using Ida.Application.Features.IngestConfiguration;
using Ida.Application.Features.IngestMonitoring;
using Ida.Infrastructure.Databases;
using Ida.Infrastructure.Excel;
using Ida.Infrastructure.Persistence;
using Ida.Infrastructure.Persistence.Interceptors;
using Ida.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Ida.Infrastructure;

public static class DependencyInjection
{

    public static IServiceCollection AddInfrastructure(this IServiceCollection services,
        IConfiguration config)
    {
        services.AddHttpContextAccessor();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ISecretProtector, AesSecretProtector>();
        services.AddSingleton<IExcelWriter, ClosedXmlExcelWriter>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IGreetingProvider, GreetingProvider>();

        services.AddSingleton<ICurrentUser, HttpCurrentUser>();
        services.AddSingleton<ITenantContext, HttpTenantContext>();
        services.AddSingleton<IRequestContext, HttpRequestContext>();

        services.AddSingleton<DatabaseRegistry>();
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<SecretsSaveChangesInterceptor>();
        services.AddScoped<DatabaseContexts>();

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IQueryExecutor, EfQueryExecutor>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IReferenceGuard, ReferenceGuard>();
        services.AddScoped<IUserActivity>(provider =>
            new UserActivity(provider.GetRequiredService<DatabaseContexts>().Core));
        services.AddScoped<IAuditTrail, AuditTrail>();
        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<IIngestConfigurationStore>(provider =>
            new IngestConfigurationStore(GetRequestSource(provider)));
        services.AddScoped<IIngestMonitoringStore>(provider =>
            new IngestMonitoringStore(GetRequestSource(provider)));
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
