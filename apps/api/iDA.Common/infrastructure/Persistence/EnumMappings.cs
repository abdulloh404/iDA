using Ida.Domain.Common;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Ida.Infrastructure.Persistence;

public static class EnumMappings
{
    private static readonly UpperSnakeNameTranslator NameTranslator = new();

    public static NpgsqlDataSourceBuilder MapIdaEnums(
        this NpgsqlDataSourceBuilder builder,
        string schema = "core")
    {
        var labels = NameTranslator;

        builder.MapEnum<RecordStatus>($"{schema}.record_status", labels);
        builder.MapEnum<ApprovalStatus>($"{schema}.approval_status", labels);
        builder.MapEnum<GenderType>($"{schema}.gender_type", labels);
        builder.MapEnum<EmploymentStatus>($"{schema}.employment_status", labels);
        builder.MapEnum<WelfareScope>($"{schema}.welfare_scope", labels);
        builder.MapEnum<RelationGroup>($"{schema}.relation_group", labels);
        builder.MapEnum<IdDocType>($"{schema}.id_doc_type", labels);
        builder.MapEnum<TaxEntityType>($"{schema}.tax_entity_type", labels);
        builder.MapEnum<ItemDirection>($"{schema}.item_direction", labels);
        builder.MapEnum<ReceiptPaymentForm>($"{schema}.receipt_payment_form", labels);
        builder.MapEnum<GlPostingDateRule>($"{schema}.gl_posting_date_rule", labels);
        builder.MapEnum<InvoiceCalcMode>($"{schema}.invoice_calc_mode", labels);
        builder.MapEnum<AdmissionType>($"{schema}.admission_type", labels);

        builder.MapEnum<ShareRateLevel>($"{schema}.share_rate_level", labels);
        builder.MapEnum<SocialKind>($"{schema}.social_kind", labels);
        builder.MapEnum<ShareTaxKind>($"{schema}.share_tax_kind", labels);
        builder.MapEnum<ShareTaxBase>($"{schema}.share_tax_base", labels);
        builder.MapEnum<ShareMode>($"{schema}.share_mode", labels);

        builder.MapEnum<HolidayPayMode>($"{schema}.holiday_pay_mode", labels);
        builder.MapEnum<DutyPayKind>($"{schema}.duty_pay_kind", labels);
        builder.MapEnum<DutyRoom>($"{schema}.duty_room", labels);
        builder.MapEnum<GuaranteeKind>($"{schema}.guarantee_kind", labels);
        builder.MapEnum<GuaranteeCalcMode>($"{schema}.guarantee_calc_mode", labels);
        builder.MapEnum<WorkTimeRule>($"{schema}.work_time_rule", labels);
        builder.MapEnum<IncomeBase>($"{schema}.income_base", labels);
        builder.MapEnum<GuaranteeBasis>($"{schema}.guarantee_basis", labels);
        builder.MapEnum<CreditCardFeeBase>($"{schema}.credit_card_fee_base", labels);
        builder.MapEnum<TreatmentScopeKind>($"{schema}.treatment_scope_kind", labels);

        return builder;
    }

    public static NpgsqlDbContextOptionsBuilder MapIdaEnums(
        this NpgsqlDbContextOptionsBuilder builder,
        string schema = "core")
    {
        var labels = NameTranslator;

        builder.MapEnum<RecordStatus>("record_status", schema, labels);
        builder.MapEnum<ApprovalStatus>("approval_status", schema, labels);
        builder.MapEnum<GenderType>("gender_type", schema, labels);
        builder.MapEnum<EmploymentStatus>("employment_status", schema, labels);
        builder.MapEnum<WelfareScope>("welfare_scope", schema, labels);
        builder.MapEnum<RelationGroup>("relation_group", schema, labels);
        builder.MapEnum<IdDocType>("id_doc_type", schema, labels);
        builder.MapEnum<TaxEntityType>("tax_entity_type", schema, labels);
        builder.MapEnum<ItemDirection>("item_direction", schema, labels);
        builder.MapEnum<ReceiptPaymentForm>("receipt_payment_form", schema, labels);
        builder.MapEnum<GlPostingDateRule>("gl_posting_date_rule", schema, labels);
        builder.MapEnum<InvoiceCalcMode>("invoice_calc_mode", schema, labels);
        builder.MapEnum<AdmissionType>("admission_type", schema, labels);

        builder.MapEnum<ShareRateLevel>("share_rate_level", schema, labels);
        builder.MapEnum<SocialKind>("social_kind", schema, labels);
        builder.MapEnum<ShareTaxKind>("share_tax_kind", schema, labels);
        builder.MapEnum<ShareTaxBase>("share_tax_base", schema, labels);
        builder.MapEnum<ShareMode>("share_mode", schema, labels);

        builder.MapEnum<HolidayPayMode>("holiday_pay_mode", schema, labels);
        builder.MapEnum<DutyPayKind>("duty_pay_kind", schema, labels);
        builder.MapEnum<DutyRoom>("duty_room", schema, labels);
        builder.MapEnum<GuaranteeKind>("guarantee_kind", schema, labels);
        builder.MapEnum<GuaranteeCalcMode>("guarantee_calc_mode", schema, labels);
        builder.MapEnum<WorkTimeRule>("work_time_rule", schema, labels);
        builder.MapEnum<IncomeBase>("income_base", schema, labels);
        builder.MapEnum<GuaranteeBasis>("guarantee_basis", schema, labels);
        builder.MapEnum<CreditCardFeeBase>("credit_card_fee_base", schema, labels);
        builder.MapEnum<TreatmentScopeKind>("treatment_scope_kind", schema, labels);

        return builder;
    }
}
