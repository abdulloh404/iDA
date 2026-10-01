using Ida.Domain.Common;

namespace Ida.Domain.Bu;

public class MstIncomeType402 : TenantMasterEntity
{
    public Guid? DepartmentId { get; set; }
    public string? AccountNo { get; set; }

    public MstDepartment? Department { get; set; }
}

public class MstAdjustmentType : TenantMasterEntity
{
    public ItemDirection Direction { get; set; } = ItemDirection.Add;
    public Guid? DepartmentId { get; set; }

    public MstDepartment? Department { get; set; }
}

public class MstExpenseType : TenantMasterEntity
{
    public ItemDirection Direction { get; set; } = ItemDirection.Deduct;
    public string? AccountNo { get; set; }
    public Guid? DepartmentId { get; set; }

    public MstDepartment? Department { get; set; }
}

public class InvoicePrefixRule : TenantEntity
{
    public string InvoicePrefix { get; set; } = string.Empty;

    public string? PaymentLocation { get; set; }
    public InvoiceCalcMode CalcMode { get; set; } = InvoiceCalcMode.NormalShare;
    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }
}

public class InvoiceArCashRule : TenantEntity
{
    public string InvoicePrefix { get; set; } = string.Empty;

    public bool IsAr { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }
}

public class InvoiceAccrualRule : TenantEntity
{
    public string InvoicePrefix { get; set; } = string.Empty;
    public AdmissionType AdmissionType { get; set; } = AdmissionType.All;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }
}

