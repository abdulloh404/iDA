namespace Ida.Api.Endpoints;

public static class MasterDataEndpoints
{
    public static void MapCoreMasterDataEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapSpecialtiesEndpoints();

        app.MapSubSpecialtiesEndpoints();

        app.MapHospitalEndpoints();

        app.MapTitlesEndpoints();

        app.MapBanksEndpoints();

        app.MapBankBranchesEndpoints();

        app.MapDocumentTypesEndpoints();

        app.MapPitTaxBracketsEndpoints();

        app.MapTaxAllowanceTypesEndpoints();

        app.MapTaxAllowanceItemsEndpoints();
    }
}

