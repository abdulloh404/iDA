using Ida.Application.Common;
using Ida.Application.Features.Doctors;
using Ida.Infrastructure;
using Ida.Infrastructure.Databases;
using Ida.Infrastructure.Security;

namespace Ida.Api;

public static class ApiSetup
{
    public static void ConfigureIdaApi(this WebApplicationBuilder builder, string[] args, DatabaseRuntime runtime)
    {
        if (runtime != DatabaseRuntime.Tenant) throw new ArgumentOutOfRangeException(nameof(runtime));
        builder.ConfigureCommonApi(args, configuration =>
        {
            var buId = (configuration["Api:BuId"] ?? configuration["BU_ID"])?.Trim().ToUpperInvariant();
            return $"Api:Tenants:{buId}";
        });
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
        services.AddCommonApi(configuration, typeof(DoctorCodeSpec).Assembly, "iDA Tenant API");
        services.AddInfrastructure(configuration, runtime);
        return services;
    }

    public static void UseIdaApi(this WebApplication app, DatabaseRuntime runtime)
    {
        if (runtime != DatabaseRuntime.Tenant) throw new ArgumentOutOfRangeException(nameof(runtime));
        var registry = app.Services.GetRequiredService<DatabaseRegistry>();
        var serviceSection = $"Api:Tenants:{registry.FixedBranch.ConnectionKey}";
        var pathBase = app.Configuration["Api:PathBase"] ?? app.Configuration[$"{serviceSection}:PathBase"] ?? app.Configuration["API_PATH_BASE"]
            ?? app.Configuration[$"{registry.FixedBranch.ConnectionKey}_API_PATH"] ?? "/" + registry.FixedBranch.ConnectionKey.ToLowerInvariant();
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
