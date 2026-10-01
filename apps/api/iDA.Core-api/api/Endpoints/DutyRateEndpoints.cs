using Ida.Application.Common.Crud;
using Ida.Application.Features.DutyRates;
using Ida.Domain.Bu;

namespace Ida.Api.Endpoints;

public static class DutyRateEndpoints
{
    public static void MapDutyRateEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapCrud<DutyHolidayRate, HolidayRateListItem, HolidayRateDetail,
            HolidayRateInput>(Resource("holiday-duty-rates"));

        app.MapCrud<DutyHolidayExclusion, HolidayExclusionRow, HolidayExclusionDetail,
            HolidayExclusionInput>(Resource("holiday-duty-exclusions"));

        app.MapCrud<DutyRate, DutyRateListItem, DutyRateDetail,
            DutyRateInput>(Resource("duty-rates"));

        app.MapCrud<DutyRateDay, DutyRateDayRow, DutyRateDayDetail,
            DutyRateDayInput>(Resource("duty-rate-days"));

        app.MapCrud<GuaranteeRate, GuaranteeRateListItem, GuaranteeRateDetail,
            GuaranteeRateInput>(Resource("guarantee-rates"));

        app.MapCrud<GuaranteeRateTreatment, GuaranteeTreatmentRow, GuaranteeTreatmentDetail,
            GuaranteeTreatmentInput>(Resource("guarantee-rate-treatments"));

        app.MapCrud<GuaranteeRateDay, GuaranteeDayRow, GuaranteeDayDetail,
            GuaranteeDayInput>(Resource("guarantee-rate-days"));
    }

    private static CrudResource Resource(string name) =>
        CrudRegistry.Resources.FirstOrDefault(r => r.Name == name)
        ?? throw new InvalidOperationException(
            $"No CrudSpec declares the resource '{name}'. Add one under " +
            $"Ida.Application/Features/DutyRates/, or remove the route.");
}

