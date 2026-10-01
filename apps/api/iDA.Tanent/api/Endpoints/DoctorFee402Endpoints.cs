using Ida.Api.Auth;
using Ida.Application.Features.DoctorFee402;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DoctorFee402Endpoints
{
    public static void MapDoctorFee402Endpoints(this IEndpointRouteBuilder app)
    {
        app.MapPositionFeesEndpoints();

        app.MapExternalFeesEndpoints();

        app.MapExternalFeeLinesEndpoints();

        app.MapFeeItemsEndpoints();

        app.MapHospitalPaidTaxesEndpoints();

        app.MapTaxDeductionsEndpoints();

        app.MapTaxDeductionItemsEndpoints();

        app.MapTaxExemptionsEndpoints();

        app.MapPost("/api/doctor-fees/decide", async (DecideDoctorFeesCommand command,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(command, ct)))
            .WithTags("จัดการค่าแพทย์ 40(2)")
            .WithName("doctor_fees_decide")
            .WithDescription("อนุมัติ / ไม่อนุมัติ / ส่งกลับ รายการค่าแพทย์หลายรายการพร้อมกัน " +
                "(resource = external-fees หรือ fee-items) — ทั้งชุดสำเร็จหรือไม่สำเร็จด้วยกัน")
            .Produces<DecideDoctorFeesResult>()
            .RequirePermission("doctor-fee-402.approve");
    }
}

