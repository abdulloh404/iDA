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
    public static void MapCoreDoctorEndpoints(this IEndpointRouteBuilder app)
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
    }

    private static CrudResource Resource(string name) =>
        CrudRegistry.Resources.FirstOrDefault(r => r.Name == name)
        ?? throw new InvalidOperationException(
            $"No CrudSpec declares the resource '{name}'. Add one under " +
            $"Ida.Application/Features/Doctors/, or remove the route.");
}
