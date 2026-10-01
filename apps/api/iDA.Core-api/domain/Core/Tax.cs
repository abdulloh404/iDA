using Ida.Domain.Common;

namespace Ida.Domain.Core;

public class PitTaxBracket : AuditableEntity
{
    public short TaxYear { get; set; }
    public decimal IncomeFrom { get; set; }

    public decimal? IncomeTo { get; set; }
    public decimal Percent { get; set; }

    public decimal BaseAmount { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }
}

public class TaxAllowanceType : AuditableEntity
{

    public short TaxYear { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }

    public ICollection<TaxAllowanceItem> Items { get; set; } = [];
}

public class TaxAllowanceItem : AuditableEntity
{
    public Guid TaxAllowanceTypeId { get; set; }
    public string AllowanceName { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public decimal Amount { get; set; }
    public int DisplaySeq { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;

    public TaxAllowanceType? AllowanceType { get; set; }
}

