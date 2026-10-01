using Ida.Application.Common;
using Ida.Infrastructure.Excel;
using Ida.Infrastructure.Persistence;
using Ida.Infrastructure.Persistence.Interceptors;
using Ida.Infrastructure.Security;
using Ida.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ida.Infrastructure;

public static class CommonDependencyInjection
{
    public static IServiceCollection AddCommonInfrastructure(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ISecretProtector, AesSecretProtector>();
        services.AddSingleton<IExcelWriter, ClosedXmlExcelWriter>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IGreetingProvider, GreetingProvider>();
        services.AddSingleton<ICurrentUser, HttpCurrentUser>();
        services.AddSingleton<IRequestContext, HttpRequestContext>();
        services.AddHttpClient<ServiceApiClient>(client => client.Timeout = TimeSpan.FromSeconds(60))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<SecretsSaveChangesInterceptor>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IQueryExecutor, EfQueryExecutor>();
        services.AddScoped<ICrudRelatedData, CrudRelatedData>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuditTrail, AuditTrail>();
        services.TryAddScoped<IDatabaseErrorMapper, DefaultDatabaseErrorMapper>();
        return services;
    }
}
