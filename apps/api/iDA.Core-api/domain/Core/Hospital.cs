using Ida.Domain.Common;

namespace Ida.Domain.Core;

public class Hospital : IEntity<string>, IAuditable, ISoftDeletable, IConcurrencyAware
{
    public string Id { get; set; } = string.Empty;

    public uint RowVersion { get; set; }

    public string HospitalNameTh { get; set; } = string.Empty;
    public string? HospitalNameEn { get; set; }
    public string? ShortName { get; set; }
    public string? BranchNo { get; set; }

    public string? DoctorCodePrefix { get; set; }

    public string? GlPostCode { get; set; }
    public string? SetOfBooks { get; set; }
    public string? EkgTreatmentPrefix { get; set; }

    public string? TaxId { get; set; }
    public string? TaxAddrNo { get; set; }
    public string? TaxAddrBuilding { get; set; }
    public string? TaxAddrSoi { get; set; }
    public string? TaxAddrRoad { get; set; }
    public string? TaxAddrSubdistrict { get; set; }
    public string? TaxAddrDistrict { get; set; }
    public string? TaxAddrProvince { get; set; }
    public string? TaxAddrPostcode { get; set; }
    public string? TaxAddrCountry { get; set; }

    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }

    public string? TenantDbName { get; set; }
    public string? TenantDbHost { get; set; }

    public bool IsHeadOffice { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public string? Remark { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

