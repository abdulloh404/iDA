using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.DutyRates;

public record DutyRateListItem(
    Guid Id,
    string? DepartmentName,
    DutyRoom Room,
    string? RoomOther,
    TimeOnly StartTime,
    TimeOnly EndTime,
    DutyPayKind PayKind,
    int DayCount,
    RecordStatus Status);

public record DutyRateDetail(
    Guid Id,
    Guid DepartmentId,
    DutyRoom Room,
    string? RoomOther,
    TimeOnly StartTime,
    TimeOnly EndTime,
    DutyPayKind PayKind,
    RecordStatus Status,
    string? Remark,
    string RowVersion);

public record DutyRateInput(
    Guid DepartmentId,
    DutyRoom Room,
    string? RoomOther,
    TimeOnly StartTime,
    TimeOnly EndTime,
    DutyPayKind PayKind,
    RecordStatus Status,
    string? Remark);

public sealed class DutyRateSpec
    : CrudSpec<DutyRate, DutyRateListItem, DutyRateDetail, DutyRateInput>
{
    public override string Resource => "duty-rates";
    public override string DisplayNameTh => "อัตราค่าแพทย์เวร";
    public override string Module => "duty-rates";
    public override string DefaultSort => "startTime";

    public override IReadOnlyList<string> FilterKeys => ["departmentId", "room", "payKind"];

    public override Expression<Func<DutyRate, DutyRateListItem>> ListProjection =>
        e => new DutyRateListItem(e.Id,
            e.Department == null ? null : e.Department.NameTh,
            e.Room, e.RoomOther, e.StartTime, e.EndTime, e.PayKind,
            e.Days.Count(d => d.DeletedAt == null),
            e.Status);

    public override Expression<Func<DutyRate, DutyRateDetail>> DetailProjection =>
        e => new DutyRateDetail(e.Id, e.DepartmentId, e.Room, e.RoomOther, e.StartTime,
            e.EndTime, e.PayKind, e.Status, e.Remark, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string, Expression<Func<DutyRate, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DutyRate, object?>>>
        {
            ["departmentName"] = e => e.Department == null ? null : e.Department.NameTh,
            ["room"] = e => e.Room,
            ["startTime"] = e => e.StartTime,
            ["endTime"] = e => e.EndTime,
            ["status"] = e => e.Status,
        };

    public override IQueryable<DutyRate> Search(IQueryable<DutyRate> q, ListRequest r)
    {
        if (r.Filter("departmentId") is { } depId && Guid.TryParse(depId, out var dId))
            q = q.Where(e => e.DepartmentId == dId);

        if (r.Enum<DutyRoom>("room") is { } room)
            q = q.Where(e => e.Room == room);

        if (r.Enum<DutyPayKind>("payKind") is { } kind)
            q = q.Where(e => e.PayKind == kind);

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var text = r.Q.Trim();
            q = q.Where(e =>
                (e.RoomOther != null && e.RoomOther.Contains(text)) ||
                (e.Department != null && e.Department.NameTh.Contains(text)));
        }

        return q;
    }

    public override void Apply(DutyRate e, DutyRateInput input, bool isCreate)
    {
        e.DepartmentId = input.DepartmentId;
        e.Room = input.Room;

        e.RoomOther = input.Room == DutyRoom.Other ? input.RoomOther?.Trim() : null;
        e.StartTime = input.StartTime;
        e.EndTime = input.EndTime;
        e.PayKind = input.PayKind;
        e.Status = input.Status;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(DutyRate e, DutyRateInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.DepartmentId, "departmentId", "แผนก");

        if (input.Room == DutyRoom.Other)
            MasterFieldRules.Required(errors, input.RoomOther, "roomOther", "ชื่อห้อง/เวร");

        if (input.StartTime == input.EndTime)
            errors.Add("endTime", "range", "เวลาสิ้นสุดต้องไม่เท่ากับเวลาเริ่มต้น");

        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(DutyRate e, DutyRateInput input,
        bool isCreate, ValidationFailure errors, IRepository<DutyRate> repo,
        IQueryExecutor exec, CancellationToken ct)
    {
        if (input.StartTime == input.EndTime) return;

        var siblings = await exec.ToListAsync(
            repo.Query()
                .Where(o => o.Id != e.Id &&
                            o.DepartmentId == input.DepartmentId &&
                            o.Room == input.Room)
                .Select(o => new { o.StartTime, o.EndTime }),
            ct);

        if (siblings.Any(o => Overlaps(input.StartTime, input.EndTime, o.StartTime, o.EndTime)))
            errors.Add("startTime", "overlap",
                "มีอัตราค่าเวรของแผนกและห้องเดียวกันที่ช่วงเวลาทับกันอยู่แล้ว");
    }

    private static bool Overlaps(TimeOnly aFrom, TimeOnly aTo, TimeOnly bFrom, TimeOnly bTo)
    {
        foreach (var (a1, a2) in Split(aFrom, aTo))
            foreach (var (b1, b2) in Split(bFrom, bTo))
                if (a1 < b2 && b1 < a2) return true;

        return false;
    }

    private static IEnumerable<(TimeOnly From, TimeOnly To)> Split(TimeOnly from, TimeOnly to)
    {
        if (from < to)
        {
            yield return (from, to);
            yield break;
        }

        yield return (from, TimeOnly.MaxValue);
        yield return (TimeOnly.MinValue, to);
    }

    public override IReadOnlyList<ExcelColumn<DutyRateListItem>> ExportColumns =>
    [
        new("แผนก", r => r.DepartmentName),
        new("ห้อง/เวร", r => RoomTh(r.Room, r.RoomOther)),
        new("เวลาเริ่มต้น", r => r.StartTime.ToString("HH:mm")),
        new("เวลาสิ้นสุด", r => r.EndTime.ToString("HH:mm")),
        new("ประเภทการจ่ายค่าเวร", r => PayKindTh(r.PayKind)),
        new("จำนวนวันที่กำหนดอัตรา", r => r.DayCount, "#,##0"),
        new("สถานะ", r => MasterFieldRules.StatusTh(r.Status)),
    ];

    public static string RoomTh(DutyRoom room, string? other) => room switch
    {
        DutyRoom.Other => string.IsNullOrWhiteSpace(other) ? "อื่น ๆ" : other,
        _ => "ห้อง/เวร " + ((int)room + 1),
    };

    public static string PayKindTh(DutyPayKind kind) => kind switch
    {
        DutyPayKind.Normal => "ปกติ",
        DutyPayKind.OnTop => "On Top",
        DutyPayKind.LumpSum => "เหมาจ่าย",
        _ => "Surplus",
    };
}

public record DutyRateDayRow(
    Guid Id, Guid DutyRateId, short DayOfWeek, string DayNameTh,
    decimal? NonBoardHourlyAmount, decimal? BoardHourlyAmount);

public record DutyRateDayDetail(
    Guid Id, Guid DutyRateId, short DayOfWeek,
    decimal? NonBoardHourlyAmount, decimal? BoardHourlyAmount, string RowVersion);

public record DutyRateDayInput(
    Guid DutyRateId, short DayOfWeek,
    decimal? NonBoardHourlyAmount, decimal? BoardHourlyAmount);

public sealed class DutyRateDaySpec
    : CrudSpec<DutyRateDay, DutyRateDayRow, DutyRateDayDetail, DutyRateDayInput>
{
    public override string Resource => "duty-rate-days";
    public override string DisplayNameTh => "อัตราค่าเวรรายวัน";
    public override string Module => "duty-rates";
    public override string DefaultSort => "dayOfWeek";

    public override IReadOnlyList<string> FilterKeys => ["dutyRateId"];

    public override Expression<Func<DutyRateDay, DutyRateDayRow>> ListProjection =>
        e => new DutyRateDayRow(e.Id, e.DutyRateId, e.DayOfWeek,
            DayNames[e.DayOfWeek], e.NonBoardHourlyAmount, e.BoardHourlyAmount);

    public override Expression<Func<DutyRateDay, DutyRateDayDetail>> DetailProjection =>
        e => new DutyRateDayDetail(e.Id, e.DutyRateId, e.DayOfWeek,
            e.NonBoardHourlyAmount, e.BoardHourlyAmount, e.RowVersion.ToString());

    public override IReadOnlyDictionary<string,
        Expression<Func<DutyRateDay, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DutyRateDay, object?>>>
        {
            ["dayOfWeek"] = e => e.DayOfWeek,
        };

    public override IQueryable<DutyRateDay> Search(IQueryable<DutyRateDay> q, ListRequest r) =>
        r.Filter("dutyRateId") is { } id && Guid.TryParse(id, out var rateId)
            ? q.Where(e => e.DutyRateId == rateId)
            : q;

    public override void Apply(DutyRateDay e, DutyRateDayInput input, bool isCreate)
    {
        if (isCreate) e.DutyRateId = input.DutyRateId;
        e.DayOfWeek = input.DayOfWeek;
        e.NonBoardHourlyAmount = input.NonBoardHourlyAmount;
        e.BoardHourlyAmount = input.BoardHourlyAmount;
    }

    public override Task ValidateAsync(DutyRateDay e, DutyRateDayInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.DutyRateId, "dutyRateId", "อัตราค่าเวร");

        if (input.DayOfWeek is < 0 or > 6)
            errors.Add("dayOfWeek", "range", "โปรดเลือกวันในสัปดาห์");

        if (input.NonBoardHourlyAmount is null && input.BoardHourlyAmount is null)
            errors.Required("nonBoardHourlyAmount", "โปรดระบุอัตราอย่างน้อยหนึ่งช่อง");

        if (input.NonBoardHourlyAmount is < 0)
            errors.Add("nonBoardHourlyAmount", "range", "อัตราต้องไม่ติดลบ");
        if (input.BoardHourlyAmount is < 0)
            errors.Add("boardHourlyAmount", "range", "อัตราต้องไม่ติดลบ");

        return Task.CompletedTask;
    }

    public override async Task ValidateAgainstDataAsync(DutyRateDay e, DutyRateDayInput input,
        bool isCreate, ValidationFailure errors, IRepository<DutyRateDay> repo,
        IQueryExecutor exec, CancellationToken ct)
    {
        var clash = repo.Query().Where(o =>
            o.Id != e.Id && o.DutyRateId == input.DutyRateId && o.DayOfWeek == input.DayOfWeek);

        if (await exec.AnyAsync(clash, ct))
            errors.Duplicate("dayOfWeek", "วันนี้มีอัตราอยู่แล้วในชุดนี้");
    }

    public static readonly string[] DayNames =
        ["อาทิตย์", "จันทร์", "อังคาร", "พุธ", "พฤหัสบดี", "ศุกร์", "เสาร์"];
}

