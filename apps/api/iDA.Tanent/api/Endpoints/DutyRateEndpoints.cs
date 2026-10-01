namespace Ida.Api.Endpoints;

public static class DutyRateEndpoints
{
    public static void MapDutyRateEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHolidayDutyRatesEndpoints();

        app.MapHolidayDutyExclusionsEndpoints();

        app.MapDutyRatesEndpoints();

        app.MapDutyRateDaysEndpoints();

        app.MapGuaranteeRatesEndpoints();

        app.MapGuaranteeRateTreatmentsEndpoints();

        app.MapGuaranteeRateDaysEndpoints();
    }
}

