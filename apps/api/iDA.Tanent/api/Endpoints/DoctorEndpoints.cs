using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Features.Doctors;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DoctorEndpoints
{
    public static void MapTenantDoctorEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapDoctorCodesEndpoints();

        app.MapDoctorBankAccountsEndpoints();

        app.MapDoctorSpecialtiesEndpoints();

        app.MapDoctorContractsEndpoints();

        app.MapBuDoctorDocumentsEndpoints();

        app.MapWelfarePlansEndpoints();

        app.MapDoctorWelfaresEndpoints();

        MapInterfacedTables(app);
    }

    private static void MapInterfacedTables(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/master-data").WithTags("ข้อมูลจากระบบต้นทาง");

        group.MapGet("/doctor-schedules", async (HttpRequest http, ISender mediator,
                CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListDoctorSchedulesQuery(ListQueryString.Read(http)), ct)))
            .WithName("doctor_schedules_list")
            .WithDescription("ตัวกรอง: doctorCodeId, from, to — ข้อมูลจาก SSB หรือ iMED")
            .Produces<PagedResult<DoctorScheduleRow>>()
            .RequirePermission("doctor-codes.read");

        group.MapGet("/doctor-schedule-offs", async (HttpRequest http, ISender mediator,
                CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListDoctorScheduleOffsQuery(ListQueryString.Read(http)), ct)))
            .WithName("doctor_schedule_offs_list")
            .WithDescription("ตัวกรอง: doctorCodeId — ข้อมูลจาก SSB หรือ iMED")
            .Produces<PagedResult<DoctorScheduleOffRow>>()
            .RequirePermission("doctor-codes.read");

        group.MapGet("/doctor-welfare-usages", async (HttpRequest http, ISender mediator,
                CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListDoctorWelfareUsagesQuery(ListQueryString.Read(http)), ct)))
            .WithName("doctor_welfare_usages_list")
            .WithDescription("ตัวกรอง: welfareId — รายการใช้สิทธิ์ที่ interface จาก HIS")
            .Produces<PagedResult<DoctorWelfareUsageRow>>()
            .RequirePermission("doctor-welfares.read");
    }
}
