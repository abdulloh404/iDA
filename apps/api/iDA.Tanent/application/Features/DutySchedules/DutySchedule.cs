using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.DutySchedules;

public record DutyScheduleListItem(
    Guid Id,
    DutyScheduleKind Kind,
    int Year,
    int Month,
    DutyScheduleStatus Status,

    int ShiftCount,

    int CompletedShiftCount,
    decimal TotalWorkHours,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? CalculatedAt);

public record DutyScheduleDetail(
    Guid Id,
    DutyScheduleKind Kind,
    int Year,
    int Month,
    DutyScheduleStatus Status,
    DateTimeOffset? SubmittedAt,
    string? SubmittedBy,
    DateTimeOffset? CalculatedAt,
    DateTimeOffset? RejectedAt,
    string? RejectedBy,
    string? RejectReason,
    string? Remark,
    string RowVersion);

public record DutyScheduleInput(string? Remark);

public sealed class DutyScheduleSpec
    : CrudSpec<DutySchedule, DutyScheduleListItem, DutyScheduleDetail, DutyScheduleInput>
{
    public override string Resource => "duty-schedules";
    public override string DisplayNameTh => "ตารางเวร";
    public override string Module => "duty-schedules";
    public override string DefaultSort => "-period";

    public override IReadOnlyList<string> FilterKeys => ["kind", "year", "month", "scheduleStatus"];

    public override Expression<Func<DutySchedule, DutyScheduleListItem>> ListProjection =>
        e => new DutyScheduleListItem(e.Id, e.Kind, e.Year, e.Month, e.Status,
            e.Shifts.Count(s => s.DeletedAt == null),

            e.Shifts.Count(s => s.DeletedAt == null &&
                s.Doctors.Count(d => d.DeletedAt == null && (d.NoExam || d.WorkEnd != null))
                    >= s.RequiredDoctors),
            e.Shifts.Where(s => s.DeletedAt == null)
                .SelectMany(s => s.Doctors.Where(d => d.DeletedAt == null))
                .Sum(d => (decimal?)d.WorkHours) ?? 0m,
            e.SubmittedAt, e.CalculatedAt);

    public override Expression<Func<DutySchedule, DutyScheduleDetail>> DetailProjection =>
        e => new DutyScheduleDetail(e.Id, e.Kind, e.Year, e.Month, e.Status,
            e.SubmittedAt, e.SubmittedBy, e.CalculatedAt,
            e.RejectedAt, e.RejectedBy, e.RejectReason, e.Remark, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string, Expression<Func<DutySchedule, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DutySchedule, object?>>>
        {

            ["period"] = e => e.Year * 100 + e.Month,
            ["year"] = e => e.Year,
            ["month"] = e => e.Month,

            ["status"] = e => e.Status,
            ["scheduleStatus"] = e => e.Status,
            ["submittedAt"] = e => e.SubmittedAt,
        };

    public override IQueryable<DutySchedule> Search(IQueryable<DutySchedule> q, ListRequest r)
    {
        if (r.Enum<DutyScheduleKind>("kind") is { } kind)
            q = q.Where(e => e.Kind == kind);

        if (r.Filter("year") is { } year && int.TryParse(year, out var y))
            q = q.Where(e => e.Year == y);

        if (r.Filter("month") is { } month && int.TryParse(month, out var m))
            q = q.Where(e => e.Month == m);

        if (r.Enum<DutyScheduleStatus>("scheduleStatus") is { } status)
            q = q.Where(e => e.Status == status);

        return q;
    }

    public override void Apply(DutySchedule e, DutyScheduleInput input, bool isCreate) =>
        e.Remark = input.Remark?.Trim();

    public override Task ValidateAsync(DutySchedule e, DutyScheduleInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {

        if (isCreate)
            errors.Add("year", "use_generate",
                "ตารางเวรถูกสร้างจากปุ่มสร้างตารางเวร ซึ่งจะดึงเวรทั้งเดือนจากอัตราค่าเวรให้เอง");

        return Task.CompletedTask;
    }

    public override async Task<string?> WhyCannotDeleteAsync(DutySchedule e, CancellationToken ct)
    {
        await Task.CompletedTask;

        return e.Status == DutyScheduleStatus.Calculated
            ? "ลบไม่ได้ เพราะตารางเวรนี้ถูกคำนวณรายเดือนไปแล้ว ให้ถอนการคำนวณก่อน"
            : null;
    }

    public override IReadOnlyList<ExcelColumn<DutyScheduleListItem>> ExportColumns =>
    [
        new("ปี", r => r.Year),
        new("เดือน", r => r.Month),
        new("จำนวนเวร", r => r.ShiftCount),
        new("ลงเวลาครบ", r => r.CompletedShiftCount),
        new("ชั่วโมงรวม", r => r.TotalWorkHours),
        new("สถานะ", r => DutyScheduleLabels.StatusTh(r.Status)),
    ];
}

public static class DutyScheduleLabels
{
    public static string StatusTh(DutyScheduleStatus status) => status switch
    {
        DutyScheduleStatus.Draft => "รอดำเนินการ Submit",
        DutyScheduleStatus.Submitted => "ส่งให้บัญชีแล้ว",
        DutyScheduleStatus.Calculated => "คำนวณแล้ว",
        DutyScheduleStatus.Rejected => "ถูกตีกลับ",
        _ => status.ToString(),
    };

    public static string KindTh(DutyScheduleKind kind) => kind switch
    {
        DutyScheduleKind.Duty => "เวรปกติ",
        DutyScheduleKind.GuaranteeHourly => "ประกันรายได้รายชั่วโมง",
        DutyScheduleKind.GuaranteeSession => "ประกันรายได้รายคาบ",
        DutyScheduleKind.GuaranteeMonthly => "ประกันรายได้รายเดือน",
        _ => kind.ToString(),
    };
}

