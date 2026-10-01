using Ida.Api.Endpoints;

namespace Ida.Api;

public static class EndpointSetup
{
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
        app.MapCommonEndpoints();
    }
}
