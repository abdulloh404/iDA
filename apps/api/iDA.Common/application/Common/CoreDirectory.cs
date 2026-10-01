using Ida.Application.Features.IngestConfiguration;

namespace Ida.Application.Common;

public record CoreDoctor(Guid Id, string DoctorGlobalCode, string FirstNameTh, string LastNameTh, string? TaxId)
{
    public string Name => FirstNameTh + " " + LastNameTh;
}

public record CoreDoctorSearch(string Text, bool Name = false, bool GlobalCode = false, bool TaxId = false);
public record CoreDoctorEmail(Guid DoctorId, string ContactType, string ContactValue, bool IsPrimary);
public record CoreBank(Guid Id, string Code);
public record CoreBankBranch(Guid Id, string BranchNameTh, string? BankName);
public record CoreSpecialty(Guid Id, string SpecialtyNameTh);
public record CoreSubSpecialty(Guid Id, string SubSpecialtyNameTh);
public record CoreDocumentType(Guid Id, string DocTypeNameTh);
public record CoreTaxAllowance(Guid Id, string AllowanceName, decimal Amount);

public interface ICoreDirectory
{
    Task<List<CoreDoctor>> DoctorsAsync(IEnumerable<Guid> ids, CancellationToken ct);
    Task<List<Guid>> FindDoctorIdsAsync(CoreDoctorSearch search, CancellationToken ct);
    Task<List<Guid>> OrderDoctorIdsAsync(Guid[] ids, bool descending, CancellationToken ct);
    Task<List<Guid>> OrderTaxAllowanceIdsAsync(Guid[] ids, bool descending, CancellationToken ct);
    Task<List<CoreDoctorEmail>> DoctorEmailsAsync(IEnumerable<Guid> doctorIds, CancellationToken ct);
    Task<List<CoreBank>> BanksAsync(IEnumerable<Guid> ids, CancellationToken ct);
    Task<List<CoreBankBranch>> BankBranchesAsync(IEnumerable<Guid> ids, CancellationToken ct);
    Task<List<CoreSpecialty>> SpecialtiesAsync(IEnumerable<Guid> ids, CancellationToken ct);
    Task<List<CoreSubSpecialty>> SubSpecialtiesAsync(IEnumerable<Guid> ids, CancellationToken ct);
    Task<List<CoreDocumentType>> DocumentTypesAsync(IEnumerable<Guid> ids, CancellationToken ct);
    Task<List<CoreTaxAllowance>> TaxAllowancesAsync(IEnumerable<Guid> ids, CancellationToken ct);
    Task<List<InterfaceDto>> IngestDefinitionsAsync(CancellationToken ct);
}

public interface ITenantApiDirectory
{
    Task<IReadOnlyDictionary<string, string>> PathsAsync(CancellationToken ct);
}
