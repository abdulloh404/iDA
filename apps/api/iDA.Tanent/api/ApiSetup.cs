using Ida.Application.Common;
using Ida.Application.Features.Doctors;
using Ida.Infrastructure;
using Ida.Infrastructure.Databases;
using Ida.Infrastructure.Security;
using Ida.Infrastructure.Services;

namespace Ida.Api;

public static class ApiSetup
{
    public static void ConfigureIdaApi(this WebApplicationBuilder builder, string[] args, DatabaseRuntime runtime)
    {
        if (runtime != DatabaseRuntime.Tenant) throw new ArgumentOutOfRangeException(nameof(runtime));
        builder.ConfigureCommonApi(args, "Api");
    }

    public static DatabaseRuntime ReadApiRuntime(IConfiguration configuration, DatabaseRuntime expected)
    {
        if (expected != DatabaseRuntime.Tenant) throw new ArgumentOutOfRangeException(nameof(expected));
        var mode = configuration["Api:Mode"];
        if (mode is null || string.Equals(mode, expected.ToString(), StringComparison.OrdinalIgnoreCase)) return expected;
        throw new InvalidOperationException($"Api:Mode must be '{expected}' for this API host.");
    }

    public static IServiceCollection AddIdaApi(this IServiceCollection services, IConfiguration configuration, DatabaseRuntime runtime)
    {
        if (runtime != DatabaseRuntime.Tenant) throw new ArgumentOutOfRangeException(nameof(runtime));
        if (configuration.GetSection("Api:Tenants").GetChildren().Any())
            throw new InvalidOperationException("Tenant API settings must contain only its own BU and the Core API, without Api:Tenants.");
        var connectionKeys = configuration.GetSection("ConnectionStrings").GetChildren().Select(section => section.Key);
        if (connectionKeys.Except(["Tenant", "TenantMigration"], StringComparer.OrdinalIgnoreCase).Any())
            throw new InvalidOperationException("Tenant API settings may only contain Tenant and TenantMigration database connections.");
        services.AddCommonApi(configuration, typeof(DoctorCodeSpec).Assembly, "iDA Tenant API");
        if (string.Equals(ServiceApiClient.ReadKey(configuration), ServiceApiClient.ReadDestinationKey(configuration, "Api:Core:ServiceKey"), StringComparison.Ordinal))
            throw new InvalidOperationException("The Tenant API service key must differ from the Core API service key.");
        services.AddInfrastructure(configuration, runtime);
        return services;
    }

    public static void UseIdaApi(this WebApplication app, DatabaseRuntime runtime)
    {
        if (runtime != DatabaseRuntime.Tenant) throw new ArgumentOutOfRangeException(nameof(runtime));
        var registry = app.Services.GetRequiredService<DatabaseRegistry>();
        var pathBase = app.Configuration["Api:PathBase"] ?? app.Configuration["API_PATH_BASE"] ?? "/" + registry.FixedBranch.ConnectionKey.ToLowerInvariant();
        app.UseCommonApi(pathBase, tenant => tenant.Use(async (context, next) =>
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var hospitalId = context.User.FindFirst(IdaClaims.HospitalId)?.Value;
                if (!string.Equals(hospitalId, registry.FixedBranch.HospitalId, StringComparison.Ordinal))
                    throw ApiException.Forbidden("tenant_mismatch", "Token ไม่ตรงกับโรงพยาบาลของ Tenant API นี้ กรุณาเปลี่ยนโรงพยาบาลก่อน");
                await context.RequestServices.GetRequiredService<DatabaseContexts>().InitializeAsync(context.RequestAborted);
            }
            await next(context);
        }));
    }
}
