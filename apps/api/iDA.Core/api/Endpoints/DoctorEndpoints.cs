namespace Ida.Api.Endpoints;

public static class DoctorEndpoints
{
    public static void MapCoreDoctorEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapDoctorsEndpoints();

        app.MapDoctorLicensesEndpoints();

        app.MapDoctorContactsEndpoints();

        app.MapDoctorAddressesEndpoints();

        app.MapDoctorEducationsEndpoints();

        app.MapDoctorTrainingsEndpoints();

        app.MapDoctorWorkHistoriesEndpoints();

        app.MapDoctorAffiliationsEndpoints();

        app.MapDoctorFamiliesEndpoints();

        app.MapDoctorProfessionalRecordsEndpoints();

        app.MapDoctorInsurancesEndpoints();

        app.MapDoctorDocumentsEndpoints();

        app.MapDoctorHospitalLinksEndpoints();
    }
}
