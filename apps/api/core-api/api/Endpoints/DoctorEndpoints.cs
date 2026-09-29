using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Doctors;
using Ida.Domain.Bu;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DoctorEndpoints
{
    public static void MapDoctorEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapCrud<Doctor, DoctorListItem, DoctorDetail, DoctorInput>(
            Resource("doctors"));

        app.MapCrud<DoctorLicense, DoctorLicenseRow, DoctorLicenseDetail, DoctorLicenseInput>(
            Resource("doctor-licenses"));

        app.MapCrud<DoctorContact, DoctorContactRow, DoctorContactDetail, DoctorContactInput>(
            Resource("doctor-contacts"));

        app.MapCrud<DoctorAddress, DoctorAddressRow, DoctorAddressDetail, DoctorAddressInput>(
            Resource("doctor-addresses"));

        app.MapCrud<DoctorEducation, DoctorEducationRow, DoctorEducationDetail,
            DoctorEducationInput>(Resource("doctor-educations"));

        app.MapCrud<DoctorTraining, DoctorTrainingRow, DoctorTrainingDetail,
            DoctorTrainingInput>(Resource("doctor-trainings"));

        app.MapCrud<DoctorWorkHistory, DoctorWorkHistoryRow, DoctorWorkHistoryDetail,
            DoctorWorkHistoryInput>(Resource("doctor-work-histories"));

        app.MapCrud<DoctorAffiliation, DoctorAffiliationRow, DoctorAffiliationDetail,
            DoctorAffiliationInput>(Resource("doctor-affiliations"));

        app.MapCrud<DoctorFamily, DoctorFamilyRow, DoctorFamilyDetail, DoctorFamilyInput>(
            Resource("doctor-families"));

        app.MapCrud<DoctorProfessionalRecord, DoctorProfessionalRecordRow,
            DoctorProfessionalRecordDetail, DoctorProfessionalRecordInput>(
            Resource("doctor-professional-records"));

        app.MapCrud<DoctorInsurance, DoctorInsuranceRow, DoctorInsuranceDetail,
            DoctorInsuranceInput>(Resource("doctor-insurances"));

        app.MapCrud<DoctorDocument, DoctorDocumentRow, DoctorDocumentDetail,
            DoctorDocumentInput>(Resource("doctor-documents"));

        app.MapCrud<DoctorHospitalLink, DoctorHospitalLinkRow, DoctorHospitalLinkDetail,
            DoctorHospitalLinkInput>(Resource("doctor-hospital-links"));

        app.MapCrud<DoctorCode, DoctorCodeListItem, DoctorCodeDetail, DoctorCodeInput>(
            Resource("doctor-codes"));

        app.MapCrud<DoctorBankAccount, DoctorBankAccountRow, DoctorBankAccountDetail,
            DoctorBankAccountInput>(Resource("doctor-bank-accounts"));

        app.MapCrud<DoctorSpecialty, DoctorSpecialtyRow, DoctorSpecialtyDetail,
            DoctorSpecialtyInput>(Resource("doctor-specialties"));

        app.MapCrud<DoctorContract, DoctorContractRow, DoctorContractDetail,
            DoctorContractInput>(Resource("doctor-contracts"));

        app.MapCrud<BuDoctorDocument, BuDoctorDocumentRow, BuDoctorDocumentDetail,
            BuDoctorDocumentInput>(Resource("bu-doctor-documents"));

        app.MapCrud<MstWelfarePlan, WelfarePlanListItem, WelfarePlanDetail, WelfarePlanInput>(
            Resource("welfare-plans"));

        app.MapCrud<DoctorWelfare, DoctorWelfareListItem, DoctorWelfareDetail,
            DoctorWelfareInput>(Resource("doctor-welfares"));

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

    private static CrudResource Resource(string name) =>
        CrudRegistry.Resources.FirstOrDefault(r => r.Name == name)
        ?? throw new InvalidOperationException(
            $"No CrudSpec declares the resource '{name}'. Add one under " +
            $"Ida.Application/Features/Doctors/, or remove the route.");
}

