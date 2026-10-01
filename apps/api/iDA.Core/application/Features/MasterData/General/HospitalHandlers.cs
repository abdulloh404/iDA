using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Common;
using Ida.Domain.Core;

namespace Ida.Application.Features.MasterData.General;

public class ListHospitalsHandler(IRepository<Hospital> repo, IQueryExecutor exec)
    : IQueryHandler<ListHospitalsQuery, PagedResult<HospitalListItem>>
{
    public async Task<PagedResult<HospitalListItem>> Handle(ListHospitalsQuery query,
        CancellationToken ct)
    {
        var r = query.Request;
        var q = HospitalQuery.Search(repo.Query(), r);

        if (r.Status != StatusFilter.All)
        {
            var wanted = r.Status == StatusFilter.Active
                ? RecordStatus.Active
                : RecordStatus.Inactive;
            q = q.Where(e => e.Status == wanted);
        }

        var total = await exec.CountAsync(q, ct);

        q = HospitalQuery.Sort(q, r);
        var page = q.Skip(r.Skip).Take(r.PageSize).Select(HospitalQuery.ToListItem);

        var items = await exec.ToListAsync(page, ct);
        return new PagedResult<HospitalListItem>(items, r.Page, r.PageSize, total);
    }
}

public class GetHospitalHandler(IRepository<Hospital> repo, IQueryExecutor exec)
    : IQueryHandler<GetHospitalQuery, HospitalDetail>
{
    public async Task<HospitalDetail> Handle(GetHospitalQuery query, CancellationToken ct)
    {
        var dto = await exec.FirstOrDefaultAsync(
            repo.Query().Where(e => e.Id == query.Id).Select(HospitalQuery.ToDetail), ct);

        return dto ?? throw HospitalQuery.NotFound();
    }
}

public class CreateHospitalHandler(
    IRepository<Hospital> repo, IQueryExecutor exec, IUnitOfWork uow)
    : ICommandHandler<CreateHospitalCommand, HospitalDetail>
{
    public async Task<HospitalDetail> Handle(CreateHospitalCommand command, CancellationToken ct)
    {
        var input = command.Input;
        var errors = new ValidationFailure();
        HospitalQuery.Validate(input, isCreate: true, errors);
        errors.ThrowIfInvalid();

        var id = input.Id.Trim().ToUpperInvariant();
        if (await exec.AnyAsync(repo.Query().Where(e => e.Id == id), ct))
            throw ApiException.DuplicateCode("id", "รหัสโรงพยาบาลนี้ถูกใช้งานแล้ว");

        await GuardPrefixAsync(repo, exec, input, id, ct);

        var entity = new Hospital { Id = id };
        HospitalQuery.Apply(entity, input);
        repo.Add(entity);
        await uow.SaveChangesAsync(ct);

        return await HospitalQuery.LoadAsync(repo, exec, id, ct);
    }

    internal static async Task GuardPrefixAsync(IRepository<Hospital> repo, IQueryExecutor exec,
        HospitalInput input, string id, CancellationToken ct)
    {
        var prefix = input.DoctorCodePrefix?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(prefix)) return;

        var clash = repo.Query().Where(e => e.Id != id && e.DoctorCodePrefix == prefix);
        if (await exec.AnyAsync(clash, ct))
            throw ApiException.DuplicateCode("doctorCodePrefix",
                "คำนำหน้ารหัสแพทย์นี้ถูกใช้โดยสาขาอื่นแล้ว");
    }
}

public class UpdateHospitalHandler(
    IRepository<Hospital> repo, IQueryExecutor exec, IUnitOfWork uow)
    : ICommandHandler<UpdateHospitalCommand, HospitalDetail>
{
    public async Task<HospitalDetail> Handle(UpdateHospitalCommand command, CancellationToken ct)
    {
        var entity = await exec.FirstOrDefaultAsync(
            repo.Track().Where(e => e.Id == command.Id), ct)
            ?? throw HospitalQuery.NotFound();

        if (!string.IsNullOrEmpty(command.RowVersion))
            repo.SetConcurrencyToken(entity, command.RowVersion);

        var errors = new ValidationFailure();
        HospitalQuery.Validate(command.Input, isCreate: false, errors);
        errors.ThrowIfInvalid();

        await CreateHospitalHandler.GuardPrefixAsync(repo, exec, command.Input, command.Id, ct);

        HospitalQuery.Apply(entity, command.Input);
        await uow.SaveChangesAsync(ct);

        return await HospitalQuery.LoadAsync(repo, exec, command.Id, ct);
    }
}

public class HospitalHistoryHandler(IAuditTrail audit)
    : IQueryHandler<HospitalHistoryQuery, IReadOnlyList<AuditEntryDto>>
{
    public Task<IReadOnlyList<AuditEntryDto>> Handle(HospitalHistoryQuery query,
        CancellationToken ct) =>
        audit.ForAsync(nameof(Hospital), query.Id, ct);
}

public class HospitalLookupHandler(IRepository<Hospital> repo, IQueryExecutor exec)
    : IQueryHandler<HospitalLookupQuery, IReadOnlyList<LookupItem>>
{
    public async Task<IReadOnlyList<LookupItem>> Handle(HospitalLookupQuery query,
        CancellationToken ct)
    {
        var rows = repo.Query()
            .Where(e => e.Status == RecordStatus.Active)
            .OrderBy(e => e.Id)
            .Select(e => new LookupItem(e.Id, e.Id, e.HospitalNameTh));

        return await exec.ToListAsync(rows, ct);
    }
}

public class ExportHospitalsHandler(
    IRepository<Hospital> repo, IQueryExecutor exec, IExcelWriter excel, IClock clock)
    : IQueryHandler<ExportHospitalsQuery, ExportFile>
{
    public async Task<ExportFile> Handle(ExportHospitalsQuery query, CancellationToken ct)
    {
        var q = HospitalQuery.Sort(HospitalQuery.Search(repo.Query(), query.Request),
            query.Request);

        var rows = await exec.ToListAsync(q.Select(HospitalQuery.ToListItem), ct);
        var bytes = excel.Write(rows, HospitalQuery.ExportColumns, "สาขาโรงพยาบาล");
        return new ExportFile(bytes, $"hospitals-{clock.Now:yyyyMMdd-HHmm}.xlsx");
    }
}

