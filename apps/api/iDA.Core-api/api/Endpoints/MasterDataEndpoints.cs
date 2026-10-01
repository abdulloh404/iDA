using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.Accounting;
using Ida.Application.Features.MasterData.General;
using Ida.Application.Features.MasterData.Tax402;
using Ida.Application.Features.MasterData.Tax406;
using Ida.Domain.Bu;
using Ida.Domain.Core;

namespace Ida.Api.Endpoints;

public static class MasterDataEndpoints
{
    public static void MapCoreMasterDataEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapCrud<MstSpecialty, SpecialtyListItem, SpecialtyDetail, SpecialtyInput>(
            Resource("specialties"));

        app.MapCrud<MstSubSpecialty, SubSpecialtyListItem, SubSpecialtyDetail, SubSpecialtyInput>(
            Resource("sub-specialties"));

        app.MapHospitalEndpoints();

        app.MapCrud<MstTitle, TitleListItem, TitleDetail, TitleInput>(
            Resource("titles"));

        app.MapCrud<MstBank, BankListItem, BankDetail, BankInput>(
            Resource("banks"));

        app.MapCrud<MstBankBranch, BankBranchListItem, BankBranchDetail, BankBranchInput>(
            Resource("bank-branches"));

        app.MapCrud<MstDocumentType, DocumentTypeListItem, DocumentTypeDetail,
            DocumentTypeInput>(Resource("document-types"));

        app.MapCrud<PitTaxBracket, PitTaxBracketListItem, PitTaxBracketDetail,
            PitTaxBracketInput>(Resource("pit-tax-brackets"));

        app.MapCrud<TaxAllowanceType, TaxAllowanceTypeListItem, TaxAllowanceTypeDetail,
            TaxAllowanceTypeInput>(Resource("tax-allowance-types"));

        app.MapCrud<TaxAllowanceItem, TaxAllowanceItemListItem, TaxAllowanceItemDetail,
            TaxAllowanceItemInput>(Resource("tax-allowance-items"));
    }

    private static CrudResource Resource(string name) =>
        CrudRegistry.Resources.FirstOrDefault(r => r.Name == name)
        ?? throw new InvalidOperationException(
            $"No CrudSpec declares the resource '{name}'. Add one under " +
            $"Ida.Application/Features/MasterData/, or remove the route.");
}

