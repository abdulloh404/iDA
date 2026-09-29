using Ida.Api.Auth;
using Ida.Application.Common.Crud;
using Ida.Application.Features.DoctorFee402;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DoctorFee402Endpoints
{
    public static void MapDoctorFee402Endpoints(this IEndpointRouteBuilder app)
    {
        app.MapCrud<DfPositionFee, PositionFeeListItem, PositionFeeDetail,
            PositionFeeInput>(Resource("position-fees"));

        app.MapCrud<DfExternalFee, ExternalFeeListItem, ExternalFeeDetail,
            ExternalFeeInput>(Resource("external-fees"));

        app.MapCrud<DfExternalFeeLine, ExternalFeeLineRow, ExternalFeeLineDetail,
            ExternalFeeLineInput>(Resource("external-fee-lines"));

        app.MapCrud<DfFeeItem, FeeItemListItem, FeeItemDetail,
            FeeItemInput>(Resource("fee-items"));

        app.MapCrud<DfHospitalPaidTax, HospitalPaidTaxListItem, HospitalPaidTaxDetail,
            HospitalPaidTaxInput>(Resource("hospital-paid-taxes"));

        app.MapCrud<DfTaxDeduction, TaxDeductionListItem, TaxDeductionDetail,
            TaxDeductionInput>(Resource("tax-deductions"));

        app.MapCrud<DfTaxDeductionItem, TaxDeductionItemRow, TaxDeductionItemDetail,
            TaxDeductionItemInput>(Resource("tax-deduction-items"));

        app.MapCrud<DfTaxExemption, TaxExemptionListItem, TaxExemptionDetail,
            TaxExemptionInput>(Resource("tax-exemptions"));

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

    private static CrudResource Resource(string name) =>
        CrudRegistry.Resources.FirstOrDefault(r => r.Name == name)
        ?? throw new InvalidOperationException($"No CrudSpec declares the resource '{name}'.");
}

