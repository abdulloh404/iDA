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
        app.MapCommonEndpoints();
    }
}
