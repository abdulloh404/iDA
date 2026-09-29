using Ida.Application.Common;
using Ida.Domain.Bu;

namespace Ida.Application.Features.Doctors;

public record DoctorScheduleRow(
    long Id, Guid DoctorCodeId, string? ClinicName, DateOnly ScheduleDate, TimeOnly StartTime,
    TimeOnly EndTime, string? RoomNo, string ScheduleType, string SourceSystem,
    DateTimeOffset SyncedAt);

public record DoctorScheduleOffRow(
    long Id, Guid DoctorCodeId, DateOnly OffDateFrom, DateOnly OffDateTo, string? Reason,
    string SourceSystem, DateTimeOffset SyncedAt);

public record DoctorWelfareUsageRow(
    long Id, Guid WelfareId, string PatientHn, string? PatientName, string? RelationName,
    DateOnly VisitDate, string? InvoiceNo, decimal UsedAmount, string? SourceSystem);

public record ListDoctorSchedulesQuery(ListRequest Request)
    : IQuery<PagedResult<DoctorScheduleRow>>;

public record ListDoctorScheduleOffsQuery(ListRequest Request)
    : IQuery<PagedResult<DoctorScheduleOffRow>>;

public record ListDoctorWelfareUsagesQuery(ListRequest Request)
    : IQuery<PagedResult<DoctorWelfareUsageRow>>;

public class ListDoctorSchedulesHandler(
    IRepository<DoctorSchedule> repo, IQueryExecutor exec)
    : IQueryHandler<ListDoctorSchedulesQuery, PagedResult<DoctorScheduleRow>>
{
    public async Task<PagedResult<DoctorScheduleRow>> Handle(
        ListDoctorSchedulesQuery query, CancellationToken ct)
    {
        var r = query.Request;
        var q = repo.Query();

        if (r.Filter("doctorCodeId") is { } id && Guid.TryParse(id, out var codeId))
            q = q.Where(e => e.DoctorCodeId == codeId);

        if (r.Filter("from") is { } from && DateOnly.TryParse(from, out var f))
            q = q.Where(e => e.ScheduleDate >= f);

        if (r.Filter("to") is { } to && DateOnly.TryParse(to, out var t))
            q = q.Where(e => e.ScheduleDate <= t);

        var total = await exec.CountAsync(q, ct);

        var rows = await exec.ToListAsync(
            q.OrderByDescending(e => e.ScheduleDate).ThenBy(e => e.StartTime)
             .Skip(r.Skip).Take(r.PageSize)
             .Select(e => new DoctorScheduleRow(e.Id, e.DoctorCodeId,
                 e.Clinic == null ? null : e.Clinic.NameTh,
                 e.ScheduleDate, e.StartTime, e.EndTime, e.RoomNo, e.ScheduleType,
                 e.SourceSystem, e.SyncedAt)), ct);

        return new PagedResult<DoctorScheduleRow>(rows, r.Page, r.PageSize, total);
    }
}

public class ListDoctorScheduleOffsHandler(
    IRepository<DoctorScheduleOff> repo, IQueryExecutor exec)
    : IQueryHandler<ListDoctorScheduleOffsQuery, PagedResult<DoctorScheduleOffRow>>
{
    public async Task<PagedResult<DoctorScheduleOffRow>> Handle(
        ListDoctorScheduleOffsQuery query, CancellationToken ct)
    {
        var r = query.Request;
        var q = repo.Query();

        if (r.Filter("doctorCodeId") is { } id && Guid.TryParse(id, out var codeId))
            q = q.Where(e => e.DoctorCodeId == codeId);

        var total = await exec.CountAsync(q, ct);

        var rows = await exec.ToListAsync(
            q.OrderByDescending(e => e.OffDateFrom).Skip(r.Skip).Take(r.PageSize)
             .Select(e => new DoctorScheduleOffRow(e.Id, e.DoctorCodeId, e.OffDateFrom,
                 e.OffDateTo, e.Reason, e.SourceSystem, e.SyncedAt)), ct);

        return new PagedResult<DoctorScheduleOffRow>(rows, r.Page, r.PageSize, total);
    }
}

public class ListDoctorWelfareUsagesHandler(
    IRepository<DoctorWelfareUsage> repo, IQueryExecutor exec)
    : IQueryHandler<ListDoctorWelfareUsagesQuery, PagedResult<DoctorWelfareUsageRow>>
{
    public async Task<PagedResult<DoctorWelfareUsageRow>> Handle(
        ListDoctorWelfareUsagesQuery query, CancellationToken ct)
    {
        var r = query.Request;
        var q = repo.Query();

        if (r.Filter("welfareId") is { } id && Guid.TryParse(id, out var welfareId))
            q = q.Where(e => e.WelfareId == welfareId);

        var total = await exec.CountAsync(q, ct);

        var rows = await exec.ToListAsync(
            q.OrderByDescending(e => e.VisitDate).Skip(r.Skip).Take(r.PageSize)
             .Select(e => new DoctorWelfareUsageRow(e.Id, e.WelfareId, e.PatientHn,
                 e.PatientName, e.RelationName, e.VisitDate, e.InvoiceNo, e.UsedAmount,
                 e.SourceSystem)), ct);

        return new PagedResult<DoctorWelfareUsageRow>(rows, r.Page, r.PageSize, total);
    }
}

