namespace Ida.Api.Endpoints;

public static class MasterDataEndpoints
{
    public static void MapTenantMasterDataEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapDepartmentsEndpoints();

        app.MapClinicsEndpoints();

        app.MapDoctorTypesEndpoints();

        app.MapDoctorGroupsEndpoints();

        app.MapStatusPrivilegesEndpoints();

        app.MapPrivilegeTypesEndpoints();

        app.MapPrivilegeSubtypesEndpoints();

        app.MapIncomeDeductionItemsEndpoints();

        app.MapTreatmentsEndpoints();

        app.MapTreatmentCategoriesEndpoints();

        app.MapPaymentTypesEndpoints();

        app.MapReceiptTypesEndpoints();

        app.MapArCodesEndpoints();

        app.MapShareCategoriesEndpoints();

        app.MapGlPostingSetupsEndpoints();

        app.MapNoWaitPaymentRulesEndpoints();

        app.MapTaxTypesEndpoints();

        app.MapIncomeTypes402Endpoints();

        app.MapAdjustmentTypesEndpoints();

        app.MapExpenseTypesEndpoints();

        app.MapInvoicePrefixRulesEndpoints();

        app.MapInvoiceArCashRulesEndpoints();
    }
}

