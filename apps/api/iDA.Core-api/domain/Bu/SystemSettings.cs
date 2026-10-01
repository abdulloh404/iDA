using System.ComponentModel.DataAnnotations.Schema;
using Ida.Domain.Common;

namespace Ida.Domain.Bu;

public class DfBadDebtTier : TenantEntity
{

    public decimal FromPercent { get; set; }

    public decimal ToPercent { get; set; }

    public bool PayActual { get; set; } = true;

    public decimal? PayPercent { get; set; }

    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }
}

public class DocSlipSetting : TenantEntity, IHasProtectedSecrets
{
    public Guid DoctorCodeId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? BackupEmail { get; set; }
    public byte[]? PdfPasswordEnc { get; set; }

    public bool SendPayslip { get; set; } = true;
    public bool SendTaxCertificate406 { get; set; } = true;
    public bool SendTaxCertificate50Tawi { get; set; } = true;

    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }

    public DoctorCode? DoctorCode { get; set; }

    [NotMapped]
    public IDictionary<string, string?> PendingSecrets { get; } =
        new Dictionary<string, string?>(StringComparer.Ordinal);

    public void ApplySecret(string name, byte[]? cipher, string? hash, string? last4)
    {
        if (name == nameof(PdfPasswordEnc)) PdfPasswordEnc = cipher;
    }
}

public class SysIncomeDocSetting : TenantEntity
{

    public DocumentSendCycle PayslipCycle { get; set; } = DocumentSendCycle.Monthly;

    public int PayslipDay { get; set; } = 5;
    public PayslipArDetail PayslipArDetail { get; set; } = PayslipArDetail.Hide;

    public DocumentSendCycle Certificate406Cycle { get; set; } = DocumentSendCycle.Monthly;
    public int Certificate406Day { get; set; } = 5;

    public DocumentSendCycle Certificate50TawiCycle { get; set; } = DocumentSendCycle.Yearly;
    public int Certificate50TawiDay { get; set; } = 15;
}

public class SysHisNotifyEmail : TenantEntity
{
    public string Email { get; set; } = string.Empty;
    public string? RecipientName { get; set; }

    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }
}

public class SysHisDoctorCodeMap : TenantEntity
{

    public string HisDoctorCode { get; set; } = string.Empty;

    public Guid DoctorCodeId { get; set; }

    public Guid? DepartmentId { get; set; }

    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }

    public DoctorCode? DoctorCode { get; set; }
    public MstDepartment? Department { get; set; }
}

public class SysExpiryAlertSetting : TenantEntity
{

    public int AlertDaysBefore { get; set; } = 30;
}

public class SysCheckinArea : TenantEntity
{
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }

    public int RadiusMeters { get; set; } = 200;

    public bool AllowOutside { get; set; }
}

