using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Common;
using Ida.Domain.Core;

namespace Ida.Application.Features.MasterData.General;

public record HospitalListItem(
    string Id,
    string NameTh,
    string? NameEn,
    string? ShortName,
    string? BranchNo,
    string? DoctorCodePrefix,
    bool IsHeadOffice,
    RecordStatus Status);

public record HospitalDetail(
    string Id,
    string NameTh,
    string? NameEn,
    string? ShortName,
    string? BranchNo,
    string? DoctorCodePrefix,
    string? GlPostCode,
    string? SetOfBooks,
    string? EkgTreatmentPrefix,
    string? TaxId,
    string? TaxAddrNo,
    string? TaxAddrBuilding,
    string? TaxAddrSoi,
    string? TaxAddrRoad,
    string? TaxAddrSubdistrict,
    string? TaxAddrDistrict,
    string? TaxAddrProvince,
    string? TaxAddrPostcode,
    string? TaxAddrCountry,
    string? ContactEmail,
    string? ContactPhone,
    bool IsHeadOffice,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record HospitalInput(
    string Id,
    string NameTh,
    string? NameEn,
    string? ShortName,
    string? BranchNo,
    string? DoctorCodePrefix,
    string? GlPostCode,
    string? SetOfBooks,
    string? EkgTreatmentPrefix,
    string? TaxId,
    string? TaxAddrNo,
    string? TaxAddrBuilding,
    string? TaxAddrSoi,
    string? TaxAddrRoad,
    string? TaxAddrSubdistrict,
    string? TaxAddrDistrict,
    string? TaxAddrProvince,
    string? TaxAddrPostcode,
    string? TaxAddrCountry,
    string? ContactEmail,
    string? ContactPhone,
    bool IsHeadOffice,
    RecordStatus Status,
    string? Remark);

public record ListHospitalsQuery(ListRequest Request) : IQuery<PagedResult<HospitalListItem>>;

public record GetHospitalQuery(string Id) : IQuery<HospitalDetail>;

public record CreateHospitalCommand(HospitalInput Input) : ICommand<HospitalDetail>;

public record UpdateHospitalCommand(string Id, HospitalInput Input, string? RowVersion)
    : ICommand<HospitalDetail>;

public record HospitalHistoryQuery(string Id) : IQuery<IReadOnlyList<AuditEntryDto>>;

public record ExportHospitalsQuery(ListRequest Request) : IQuery<ExportFile>;

public record HospitalLookupQuery : IQuery<IReadOnlyList<LookupItem>>;

