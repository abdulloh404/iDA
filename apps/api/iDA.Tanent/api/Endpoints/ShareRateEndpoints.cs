namespace Ida.Api.Endpoints;

public static class ShareRateEndpoints
{
    public static void MapShareRateEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapShareRatesEndpoints();

        app.MapShareRateExclusionsEndpoints();

        app.MapPatientRightsEndpoints();
    }
}

