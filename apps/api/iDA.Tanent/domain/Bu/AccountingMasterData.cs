using Ida.Domain.Common;

namespace Ida.Domain.Bu;

public class MstTaxType : TenantMasterEntity;

public class MstShareCategory : TenantMasterEntity;

public class MstTreatmentCategory : TenantMasterEntity
{

    public bool IsPackage { get; set; }
    public string SourceSystem { get; set; } = "HIS";
    public DateTimeOffset? SyncedAt { get; set; }
}

public class MstTreatment : TenantMasterEntity
{
    public Guid? TreatmentCategoryId { get; set; }

    public decimal? PremiumRate { get; set; }

    public decimal? SocialRate { get; set; }

    public decimal? UnitPrice { get; set; }

    public bool RequiresReading { get; set; }
    public string SourceSystem { get; set; } = "HIS";
    public DateTimeOffset? SyncedAt { get; set; }

    public MstTreatmentCategory? TreatmentCategory { get; set; }
}

public class MstReceiptType : TenantMasterEntity
{
    public Guid? BankId { get; set; }
    public ReceiptPaymentForm PaymentForm { get; set; } = ReceiptPaymentForm.Cash;

    public bool IsCharged { get; set; }
    public decimal? VatPercent { get; set; }
    public string SourceSystem { get; set; } = "HIS";

}

public class MstArCode : TenantMasterEntity
{
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? Address3 { get; set; }
    public string? Province { get; set; }
    public string? Postcode { get; set; }
    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string SourceSystem { get; set; } = "HIS";
}

public class MstIncomeDeductionItem : TenantMasterEntity
{
    public Guid? TaxTypeId { get; set; }
    public ItemDirection Direction { get; set; } = ItemDirection.Add;

    public Guid? DepartmentId { get; set; }
    public string? AccountNoOpd { get; set; }
    public string? AccountNoIpd { get; set; }

    public Guid? ExpenseTypeId { get; set; }

    public string? JvType { get; set; }

    public MstTaxType? TaxType { get; set; }
    public MstDepartment? Department { get; set; }
    public MstExpenseType? ExpenseType { get; set; }
}

public class GlPostingSetup : TenantEntity
{
    public Guid ShareCategoryId { get; set; }
    public string DebitAccountNo { get; set; } = string.Empty;
    public string? DebitDepartment { get; set; }
    public string CreditAccountNo { get; set; } = string.Empty;
    public string? CreditDepartment { get; set; }

    public Guid? DoctorCodeId { get; set; }
    public GlPostingDateRule PostingDateRule { get; set; } = GlPostingDateRule.BatchDate;
    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }

    public MstShareCategory? ShareCategory { get; set; }
    public DoctorCode? DoctorCode { get; set; }
}

public class NoWaitPaymentRule : TenantEntity
{
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string? ActivityCode { get; set; }
    public Guid? DoctorCodeId { get; set; }
    public Guid? TreatmentId { get; set; }
    public Guid? TreatmentCategoryId { get; set; }
    public Guid? ArCodeId { get; set; }
    public Guid? ReceiptTypeId { get; set; }
    public string? SubInvoice { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }

    public DoctorCode? DoctorCode { get; set; }
    public MstTreatment? Treatment { get; set; }
    public MstTreatmentCategory? TreatmentCategory { get; set; }
    public MstArCode? ArCode { get; set; }
    public MstReceiptType? ReceiptType { get; set; }
}

