using Ida.Api.Endpoints;

namespace Ida.Api;

public static class EndpointSetup
{
    public static void MapCoreEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapAuthEndpoints();
        app.MapCoreMasterDataEndpoints();
        app.MapCoreDoctorEndpoints();
        app.MapCoreSystemSettingsEndpoints();
        app.MapCoreInternalEndpoints();
        MapCommonEndpoints(app);
    }

    public static void MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapTenantMasterDataEndpoints();
        app.MapTenantDoctorEndpoints();
        app.MapShareRateEndpoints();
        app.MapDutyRateEndpoints();
        app.MapDutyScheduleEndpoints();
        app.MapDoctorFee402Endpoints();
        app.MapTenantSystemSettingsEndpoints();
        app.MapApprovalEndpoints();
        app.MapIngestConfigurationEndpoints();
        app.MapIngestMonitoringEndpoints();
        app.MapTenantInternalEndpoints();
        MapCommonEndpoints(app);
    }

    private static void MapCommonEndpoints(IEndpointRouteBuilder app)
    {
        app.MapIdaEndpoints();
        app.MapGet("/healthz", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
        app.MapGet("/health", () => Results.Ok(new { status = "Healthy", checkedAt = DateTimeOffset.UtcNow })).AllowAnonymous();
    }
}
