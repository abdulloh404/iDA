using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.Approvals;

public class ListApprovalRequestsHandler(
    IRepository<DoctorApprovalRequest> repo,
    IQueryExecutor exec,
    ICurrentUser user)
    : IQueryHandler<ListApprovalRequestsQuery, PagedResult<ApprovalRequestListItem>>
{
    public async Task<PagedResult<ApprovalRequestListItem>> Handle(
        ListApprovalRequestsQuery query, CancellationToken ct)
    {
        var r = query.Request;
        var q = ApprovalQuery.Scope(repo.Query(), query.Scope, user);
        q = ApprovalQuery.Filter(q, r);

        var total = await exec.CountAsync(q, ct);

        var (key, descending) = r.ParseSort("-requestedAt");
        q = ApprovalQuery.Sort(q, key, descending);

        var rows = await exec.ToListAsync(
            q.Skip(r.Skip).Take(r.PageSize).Select(ApprovalQuery.ToListRow), ct);

        var items = rows.Select(ApprovalQuery.ToListItem).ToList();
        return new PagedResult<ApprovalRequestListItem>(items, r.Page, r.PageSize, total);
    }
}

public class GetApprovalRequestHandler(
    IRepository<DoctorApprovalRequest> repo,
    IRepository<DoctorApprovalStep> steps,
    IQueryExecutor exec,
    ICurrentUser user)
    : IQueryHandler<GetApprovalRequestQuery, ApprovalRequestDetail>
{
    public async Task<ApprovalRequestDetail> Handle(GetApprovalRequestQuery query,
        CancellationToken ct)
    {
        var row = await exec.FirstOrDefaultAsync(
            repo.Query().Where(e => e.Id == query.Id).Select(ApprovalQuery.ToDetailRow), ct)
            ?? throw ApprovalQuery.NotFound();

        var stepRows = await exec.ToListAsync(
            steps.Query().Where(s => s.RequestId == query.Id)
                .OrderBy(s => s.StepSeq)
                .Select(s => new ApprovalStepDto(s.StepSeq, s.ApproverRole, "",
                    s.ApproverUser, s.Action, s.ActionAt, s.Comment)), ct);

        var named = stepRows
            .Select(s => s with { ApproverRoleNameTh = ApprovalLabels.ApproverRoleTh(s.ApproverRole) })
            .ToList();

        var open = named.FirstOrDefault(s => s.Action is null);

        return new ApprovalRequestDetail(
            row.Id, row.RequestNo, row.RequestType,
            ApprovalLabels.RequestTypeTh(row.RequestType),
            ApprovalQuery.Summarise(row),
            row.TargetTable, row.TargetId, row.DoctorId, row.DoctorName,
            row.Payload, row.RequestedBy, row.RequestedAt, row.ClosedAt, row.Status,
            named,
            row.Status == ApprovalStatus.Pending && open is not null &&
                ApprovalQuery.MayDecide(open.ApproverRole, user));
    }
}

internal record ApprovalRow(
    Guid Id, string RequestNo, string RequestType, string TargetTable, Guid? TargetId,
    Guid? DoctorId, string? DoctorName, string Payload, string RequestedBy,
    DateTimeOffset RequestedAt, DateTimeOffset UpdatedAt, DateTimeOffset? ClosedAt,
    ApprovalStatus Status, string? CurrentStepRole);

internal static class ApprovalQuery
{
    public static ApiException NotFound() =>
        ApiException.NotFound("approval_request_not_found", "ไม่พบคำขอที่ระบุ");

    public static bool MayDecide(string approverRole, ICurrentUser user) =>
        user.IsInRole(approverRole) || user.IsInRole(ApprovalLabels.OverrideRole);

    public static IQueryable<DoctorApprovalRequest> Scope(
        IQueryable<DoctorApprovalRequest> query, ApprovalScope scope, ICurrentUser user)
    {
        switch (scope)
        {
            case ApprovalScope.Mine:
                var me = user.UserName;
                return query.Where(e => e.RequestedBy == me);

            case ApprovalScope.Pending:

                var roles = user.Roles.ToArray();
                var isOverride = user.IsInRole(ApprovalLabels.OverrideRole);
                return query.Where(e =>
                    e.CurrentStatus == ApprovalStatus.Pending &&
                    e.Steps.Where(s => s.Action == null)
                           .OrderBy(s => s.StepSeq)
                           .Take(1)
                           .Any(s => isOverride || roles.Contains(s.ApproverRole)));

            default:
                return query;
        }
    }

    public static IQueryable<DoctorApprovalRequest> Filter(
        IQueryable<DoctorApprovalRequest> query, ListRequest r)
    {
        if (r.Filter("requestType") is { } type)
            query = query.Where(e => e.RequestType == type);

        if (r.Enum<ApprovalStatus>("status") is { } parsed)
            query = query.Where(e => e.CurrentStatus == parsed);

        if (r.Filter("requestedBy") is { } who)
            query = query.Where(e => e.RequestedBy.Contains(who));

        if (r.Filter("requestedFrom") is { } from && DateTimeOffset.TryParse(from, out var f))
            query = query.Where(e => e.RequestedAt >= f);

        if (r.Filter("requestedTo") is { } to && DateTimeOffset.TryParse(to, out var t))
            query = query.Where(e => e.RequestedAt < t.AddDays(1));

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var q = r.Q.Trim();
            query = query.Where(e =>
                e.RequestNo.Contains(q) ||
                e.RequestedBy.Contains(q) ||
                (e.Doctor != null &&
                 (e.Doctor.FirstNameTh.Contains(q) || e.Doctor.LastNameTh.Contains(q))));
        }

        return query;
    }

    public static IQueryable<DoctorApprovalRequest> Sort(
        IQueryable<DoctorApprovalRequest> query, string key, bool descending)
    {
        Expression<Func<DoctorApprovalRequest, object?>> selector = key switch
        {
            "requestNo" => e => e.RequestNo,
            "requestType" => e => e.RequestType,
            "requestedBy" => e => e.RequestedBy,
            "requestedAt" => e => e.RequestedAt,
            "updatedAt" => e => e.UpdatedAt,
            "status" => e => e.CurrentStatus,
            _ => throw ApiException.BadRequest("unknown_sort",
                $"ไม่รองรับการเรียงลำดับด้วยคอลัมน์ '{key}'",
                new { allowed = new[] { "requestNo", "requestType", "requestedBy", "requestedAt", "updatedAt", "status" } }),
        };

        return descending ? query.OrderByDescending(selector) : query.OrderBy(selector);
    }

    public static readonly Expression<Func<DoctorApprovalRequest, ApprovalRow>> ToListRow =
        e => new ApprovalRow(e.Id, e.RequestNo, e.RequestType, e.TargetTable, e.TargetId,
            e.DoctorId,
            e.Doctor == null ? null : e.Doctor.FirstNameTh + " " + e.Doctor.LastNameTh,
            string.Empty, e.RequestedBy, e.RequestedAt, e.UpdatedAt, e.ClosedAt,
            e.CurrentStatus,
            e.Steps.Where(s => s.Action == null).OrderBy(s => s.StepSeq)
                   .Select(s => s.ApproverRole).FirstOrDefault());

    public static readonly Expression<Func<DoctorApprovalRequest, ApprovalRow>> ToDetailRow =
        e => new ApprovalRow(e.Id, e.RequestNo, e.RequestType, e.TargetTable, e.TargetId,
            e.DoctorId,
            e.Doctor == null ? null : e.Doctor.FirstNameTh + " " + e.Doctor.LastNameTh,
            e.Payload, e.RequestedBy, e.RequestedAt, e.UpdatedAt, e.ClosedAt,
            e.CurrentStatus,
            e.Steps.Where(s => s.Action == null).OrderBy(s => s.StepSeq)
                   .Select(s => s.ApproverRole).FirstOrDefault());

    public static ApprovalRequestListItem ToListItem(ApprovalRow row) =>
        new(row.Id, row.RequestNo, row.RequestType,
            ApprovalLabels.RequestTypeTh(row.RequestType),
            Summarise(row), row.RequestedBy, row.RequestedAt, row.UpdatedAt, row.Status,
            row.CurrentStepRole,
            row.CurrentStepRole is null ? null : ApprovalLabels.ApproverRoleTh(row.CurrentStepRole));

    public static string Summarise(ApprovalRow row)
    {
        var what = ApprovalLabels.RequestTypeTh(row.RequestType);
        var verb = row.TargetId is null ? "ขอสร้าง" : "ขอแก้ไข";
        return row.DoctorName is null ? $"{verb}{what}" : $"{verb}{what} — {row.DoctorName}";
    }
}

