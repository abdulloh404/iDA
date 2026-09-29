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
    public static void MapMasterDataEndpoints(this IEndpointRouteBuilder app)
    {

        app.MapCrud<MstSpecialty, SpecialtyListItem, SpecialtyDetail, SpecialtyInput>(
            Resource("specialties"));

        app.MapCrud<MstSubSpecialty, SubSpecialtyListItem, SubSpecialtyDetail, SubSpecialtyInput>(
            Resource("sub-specialties"));

        app.MapHospitalEndpoints();

        app.MapCrud<MstDepartment, DepartmentListItem, DepartmentDetail, DepartmentInput>(
            Resource("departments"));

        app.MapCrud<MstClinic, ClinicListItem, ClinicDetail, ClinicInput>(
            Resource("clinics"));

        app.MapCrud<MstDoctorType, MasterListItem, MasterDetail, MasterInput>(
            Resource("doctor-types"));

        app.MapCrud<MstDoctorGroup, DoctorGroupListItem, DoctorGroupDetail, DoctorGroupInput>(
            Resource("doctor-groups"));

        app.MapCrud<MstStatusPrivilege, MasterListItem, MasterDetail, MasterInput>(
            Resource("status-privileges"));

        app.MapCrud<MstPrivilegeType, MasterListItem, MasterDetail, MasterInput>(
            Resource("privilege-types"));

        app.MapCrud<MstPrivilegeSubtype, PrivilegeSubtypeListItem, PrivilegeSubtypeDetail,
            PrivilegeSubtypeInput>(Resource("privilege-subtypes"));

        app.MapCrud<MstTitle, TitleListItem, TitleDetail, TitleInput>(
            Resource("titles"));

        app.MapCrud<MstBank, BankListItem, BankDetail, BankInput>(
            Resource("banks"));

        app.MapCrud<MstBankBranch, BankBranchListItem, BankBranchDetail, BankBranchInput>(
            Resource("bank-branches"));

        app.MapCrud<MstIncomeDeductionItem, IncomeDeductionItemListItem,
            IncomeDeductionItemDetail, IncomeDeductionItemInput>(
            Resource("income-deduction-items"));

        app.MapCrud<MstTreatment, TreatmentListItem, TreatmentDetail, TreatmentInput>(
            Resource("treatments"));

        app.MapCrud<MstTreatmentCategory, TreatmentCategoryListItem, TreatmentCategoryDetail,
            TreatmentCategoryInput>(Resource("treatment-categories"));

        app.MapCrud<MstPaymentType, PaymentTypeListItem, PaymentTypeDetail, PaymentTypeInput>(
            Resource("payment-types"));

        app.MapCrud<MstReceiptType, ReceiptTypeListItem, ReceiptTypeDetail, ReceiptTypeInput>(
            Resource("receipt-types"));

        app.MapCrud<MstArCode, ArCodeListItem, ArCodeDetail, ArCodeInput>(
            Resource("ar-codes"));

        app.MapCrud<MstShareCategory, MasterListItem, MasterDetail, MasterInput>(
            Resource("share-categories"));

        app.MapCrud<GlPostingSetup, GlPostingSetupListItem, GlPostingSetupDetail,
            GlPostingSetupInput>(Resource("gl-posting-setups"));

        app.MapCrud<NoWaitPaymentRule, NoWaitPaymentRuleListItem, NoWaitPaymentRuleDetail,
            NoWaitPaymentRuleInput>(Resource("no-wait-payment-rules"));

        app.MapCrud<MstTaxType, MasterListItem, MasterDetail, MasterInput>(
            Resource("tax-types"));

        app.MapCrud<MstDocumentType, DocumentTypeListItem, DocumentTypeDetail,
            DocumentTypeInput>(Resource("document-types"));

        app.MapCrud<MstIncomeType402, IncomeType402ListItem, IncomeType402Detail,
            IncomeType402Input>(Resource("income-types-402"));

        app.MapCrud<MstAdjustmentType, AdjustmentTypeListItem, AdjustmentTypeDetail,
            AdjustmentTypeInput>(Resource("adjustment-types"));

        app.MapCrud<MstExpenseType, ExpenseTypeListItem, ExpenseTypeDetail, ExpenseTypeInput>(
            Resource("expense-types"));

        app.MapCrud<PitTaxBracket, PitTaxBracketListItem, PitTaxBracketDetail,
            PitTaxBracketInput>(Resource("pit-tax-brackets"));

        app.MapCrud<TaxAllowanceType, TaxAllowanceTypeListItem, TaxAllowanceTypeDetail,
            TaxAllowanceTypeInput>(Resource("tax-allowance-types"));

        app.MapCrud<TaxAllowanceItem, TaxAllowanceItemListItem, TaxAllowanceItemDetail,
            TaxAllowanceItemInput>(Resource("tax-allowance-items"));

        app.MapCrud<InvoicePrefixRule, InvoicePrefixRuleListItem, InvoicePrefixRuleDetail,
            InvoicePrefixRuleInput>(Resource("invoice-prefix-rules"));

        app.MapCrud<InvoiceArCashRule, InvoiceArCashRuleListItem, InvoiceArCashRuleDetail,
            InvoiceArCashRuleInput>(Resource("invoice-ar-cash-rules"));

    }

    private static CrudResource Resource(string name) =>
        CrudRegistry.Resources.FirstOrDefault(r => r.Name == name)
        ?? throw new InvalidOperationException(
            $"No CrudSpec declares the resource '{name}'. Add one under " +
            $"Ida.Application/Features/MasterData/, or remove the route.");
}

