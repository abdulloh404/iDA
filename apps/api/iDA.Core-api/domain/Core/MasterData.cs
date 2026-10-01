using Ida.Domain.Common;

namespace Ida.Domain.Core;

public class MstTitle : AuditableEntity, ICodedEntity
{
    public string Code { get; set; } = string.Empty;
    public string TitleNameTh { get; set; } = string.Empty;
    public string? TitleNameEn { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }
}

public class MstSpecialty : AuditableEntity, ICodedEntity
{
    public string Code { get; set; } = string.Empty;
    public string SpecialtyNameTh { get; set; } = string.Empty;
    public string? SpecialtyNameEn { get; set; }
    public int? DisplaySeq { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }

    public ICollection<MstSubSpecialty> SubSpecialties { get; set; } = [];
}

public class MstSubSpecialty : AuditableEntity, ICodedEntity
{
    public string Code { get; set; } = string.Empty;
    public Guid SpecialtyId { get; set; }
    public string SubSpecialtyNameTh { get; set; } = string.Empty;
    public string? SubSpecialtyNameEn { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }

    public MstSpecialty? Specialty { get; set; }
}

public class MstBank : AuditableEntity, ICodedEntity
{
    public string Code { get; set; } = string.Empty;
    public string BankNameTh { get; set; } = string.Empty;
    public string? BankNameEn { get; set; }
    public string? SwiftCode { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }

    public ICollection<MstBankBranch> Branches { get; set; } = [];
}

public class MstBankBranch : AuditableEntity, ICodedEntity
{

    public string Code { get; set; } = string.Empty;
    public Guid BankId { get; set; }
    public string BranchNameTh { get; set; } = string.Empty;
    public string? BranchNameEn { get; set; }
    public string? Address { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactFax { get; set; }
    public string? ContactEmail { get; set; }
    public bool IsHeadOffice { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }

    public MstBank? Bank { get; set; }
}

public class MstDocumentType : AuditableEntity, ICodedEntity
{
    public string Code { get; set; } = string.Empty;
    public string DocTypeNameTh { get; set; } = string.Empty;
    public string? DocTypeNameEn { get; set; }
    public bool HasExpiry { get; set; }

    public int? AlertBeforeDays { get; set; }
    public bool IsRequired { get; set; }

    public string ScopeLevel { get; set; } = "CORE";
    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }
}

