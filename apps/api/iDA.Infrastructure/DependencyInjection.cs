using Ida.Application.Common;
using Ida.Domain.Common;
using Ida.Application.Features.IngestConfiguration;
using Ida.Application.Features.IngestMonitoring;
using Ida.Infrastructure.Databases;
using Ida.Infrastructure.Excel;
using Ida.Infrastructure.Persistence;
using Ida.Infrastructure.Persistence.Interceptors;
using Ida.Infrastructure.Security;
using Ida.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Ida.Infrastructure;

public static class DependencyInjection
{

    public static IServiceCollection AddInfrastructure(this IServiceCollection services,
        IConfiguration config, DatabaseRuntime runtime = DatabaseRuntime.Core)
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

        services.AddSingleton(_ => new DatabaseRegistry(config, runtime));
        services.AddHttpClient<ServiceApiClient>(client => client.Timeout = TimeSpan.FromSeconds(60))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
        services.AddSingleton<TenantApiDirectory>();
        services.AddSingleton<ITenantApiDirectory>(provider => provider.GetRequiredService<TenantApiDirectory>());
        if (runtime == DatabaseRuntime.Tenant) services.AddScoped<ICoreDirectory, HttpCoreDirectory>();
        else services.AddScoped<ICoreDirectory, CoreDirectory>();
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<SecretsSaveChangesInterceptor>();
        services.AddScoped<DatabaseContexts>();

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IQueryExecutor, EfQueryExecutor>();
        services.AddScoped<ICrudRelatedData, CrudRelatedData>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<ReferenceGuard>();
        services.AddScoped<IReferenceGuard>(provider => provider.GetRequiredService<ReferenceGuard>());
        services.AddScoped<IUserActivity>(provider =>
            new UserActivity(provider.GetRequiredService<DatabaseContexts>().Core));
        services.AddScoped<IAuditTrail, AuditTrail>();
        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<IIngestConfigurationStore>(provider =>
            new IngestConfigurationStore(GetRequestSource(provider), provider.GetRequiredService<ICoreDirectory>()));
        services.AddScoped<IIngestMonitoringStore>(provider =>
            new IngestMonitoringStore(GetRequestSource(provider), provider.GetRequiredService<ICoreDirectory>()));
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
