using Ida.Application.Common;
using Ida.Application.Features.IngestConfiguration;
using Ida.Domain.Common;
using Ida.Domain.Core;
using Ida.Infrastructure.Databases;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ida.Infrastructure.Persistence;

public sealed class CoreDirectory(DatabaseContexts contexts, DatabaseRegistry registry) : ICoreDirectory
{
    private IQueryable<T> Query<T>() where T : class => contexts.Core.Set<T>().AsNoTracking();

    public Task<List<CoreDoctor>> DoctorsAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var selected = ids.Distinct().ToArray();
        return Query<Doctor>().Where(e => selected.Contains(e.Id))
            .Select(e => new CoreDoctor(e.Id, e.DoctorGlobalCode, e.FirstNameTh, e.LastNameTh, e.TaxId)).ToListAsync(ct);
    }

    public Task<List<Guid>> FindDoctorIdsAsync(CoreDoctorSearch search, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(search.Text)) return Task.FromResult(new List<Guid>());
        return Query<Doctor>().Where(e => search.Name && (e.FirstNameTh.Contains(search.Text) || e.LastNameTh.Contains(search.Text))
            || search.GlobalCode && e.DoctorGlobalCode.Contains(search.Text)
            || search.TaxId && e.TaxId != null && e.TaxId.Contains(search.Text)).Select(e => e.Id).ToListAsync(ct);
    }

    public Task<List<CoreDoctorEmail>> DoctorEmailsAsync(IEnumerable<Guid> doctorIds, CancellationToken ct)
    {
        var selected = doctorIds.Distinct().ToArray();
        return Query<DoctorContact>().Where(e => selected.Contains(e.DoctorId) && e.Status == RecordStatus.Active
            && (e.ContactType == "EMAIL" || e.ContactType == "EMAIL_ALT"))
            .Select(e => new CoreDoctorEmail(e.DoctorId, e.ContactType, e.ContactValue, e.IsPrimary)).ToListAsync(ct);
    }

    public Task<List<Guid>> OrderDoctorIdsAsync(Guid[] ids, bool descending, CancellationToken ct)
    {
        var query = Query<Doctor>().Where(e => ids.Contains(e.Id));
        return (descending ? query.OrderByDescending(e => e.DoctorGlobalCode).ThenByDescending(e => e.Id)
            : query.OrderBy(e => e.DoctorGlobalCode).ThenBy(e => e.Id)).Select(e => e.Id).ToListAsync(ct);
    }

    public Task<List<Guid>> OrderTaxAllowanceIdsAsync(Guid[] ids, bool descending, CancellationToken ct)
    {
        var query = Query<TaxAllowanceItem>().Where(e => ids.Contains(e.Id));
        return (descending ? query.OrderByDescending(e => e.AllowanceName).ThenByDescending(e => e.Id)
            : query.OrderBy(e => e.AllowanceName).ThenBy(e => e.Id)).Select(e => e.Id).ToListAsync(ct);
    }

    public Task<List<CoreBank>> BanksAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var selected = ids.Distinct().ToArray();
        return Query<MstBank>().Where(e => selected.Contains(e.Id)).Select(e => new CoreBank(e.Id, e.Code)).ToListAsync(ct);
    }

    public Task<List<CoreBankBranch>> BankBranchesAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var selected = ids.Distinct().ToArray();
        return Query<MstBankBranch>().Where(e => selected.Contains(e.Id))
            .Select(e => new CoreBankBranch(e.Id, e.BranchNameTh, e.Bank == null ? null : e.Bank.BankNameTh)).ToListAsync(ct);
    }

    public Task<List<CoreSpecialty>> SpecialtiesAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var selected = ids.Distinct().ToArray();
        return Query<MstSpecialty>().Where(e => selected.Contains(e.Id)).Select(e => new CoreSpecialty(e.Id, e.SpecialtyNameTh)).ToListAsync(ct);
    }

    public Task<List<CoreSubSpecialty>> SubSpecialtiesAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var selected = ids.Distinct().ToArray();
        return Query<MstSubSpecialty>().Where(e => selected.Contains(e.Id)).Select(e => new CoreSubSpecialty(e.Id, e.SubSpecialtyNameTh)).ToListAsync(ct);
    }

    public Task<List<CoreDocumentType>> DocumentTypesAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var selected = ids.Distinct().ToArray();
        return Query<MstDocumentType>().Where(e => selected.Contains(e.Id)).Select(e => new CoreDocumentType(e.Id, e.DocTypeNameTh)).ToListAsync(ct);
    }

    public Task<List<CoreTaxAllowance>> TaxAllowancesAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var selected = ids.Distinct().ToArray();
        return Query<TaxAllowanceItem>().Where(e => selected.Contains(e.Id)).Select(e => new CoreTaxAllowance(e.Id, e.AllowanceName, e.Amount)).ToListAsync(ct);
    }

    public async Task<List<InterfaceDto>> IngestDefinitionsAsync(CancellationToken ct)
    {
        var definitions = new List<InterfaceDto>();
        await using var core = await registry.CoreSource.OpenConnectionAsync(ct);
        await using var command = new NpgsqlCommand(DatabaseSql.Rewrite("SELECT code,coalesce(display_name,code),source_system,data_category FROM core.ingest_interface_definition ORDER BY source_system,code", core), core);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            definitions.Add(new InterfaceDto(reader.GetString(0), reader.GetString(1), reader.GetString(2), null, reader.GetString(3)));
        return definitions;
    }
}
