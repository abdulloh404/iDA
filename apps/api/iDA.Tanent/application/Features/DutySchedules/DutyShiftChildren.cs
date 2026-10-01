using System.Linq.Expressions;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.DutySchedules;

public record DutyShiftRow(
    Guid Id,
    Guid ScheduleId,
    DateOnly ShiftDate,
    string? DepartmentName,
    string? ClinicName,
    string RoomLabel,
    DutyPayKind PayKind,
    TimeOnly StartTime,
    TimeOnly EndTime,
    decimal? HourlyAmount,
    int RequiredDoctors,

    int DoctorCount,
    int NoExamCount,
    int CompletedCount,
    decimal WorkHours);

public record DutyShiftDetail(
    Guid Id,
    Guid ScheduleId,
    DateOnly ShiftDate,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? ClinicId,
    string? ClinicName,
    string RoomLabel,
    DutyPayKind PayKind,
    TimeOnly StartTime,
    TimeOnly EndTime,
    decimal? HourlyAmount,
    int RequiredDoctors,
    string RowVersion);

public record DutyShiftInput(
    Guid ScheduleId,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int RequiredDoctors,
    decimal? HourlyAmount);

public sealed class DutyShiftSpec
    : CrudSpec<DutyShift, DutyShiftRow, DutyShiftDetail, DutyShiftInput>
{
    public override string Resource => "duty-shifts";
    public override string DisplayNameTh => "เวร";
    public override string Module => "duty-schedules";
    public override string DefaultSort => "shiftDate";

    public override IReadOnlyList<string> FilterKeys => ["scheduleId", "dateFrom", "dateTo"];

    public override Expression<Func<DutyShift, DutyShiftRow>> ListProjection =>
        e => new DutyShiftRow(e.Id, e.ScheduleId, e.ShiftDate,
            e.Department == null ? null : e.Department.NameTh,
            e.Clinic == null ? null : e.Clinic.NameTh,
            e.RoomLabel, e.PayKind, e.StartTime, e.EndTime, e.HourlyAmount, e.RequiredDoctors,
            e.Doctors.Count(d => d.DeletedAt == null),
            e.Doctors.Count(d => d.DeletedAt == null && d.NoExam),
            e.Doctors.Count(d => d.DeletedAt == null && (d.NoExam || d.WorkEnd != null)),
            e.Doctors.Where(d => d.DeletedAt == null).Sum(d => (decimal?)d.WorkHours) ?? 0m);

    public override Expression<Func<DutyShift, DutyShiftDetail>> DetailProjection =>
        e => new DutyShiftDetail(e.Id, e.ScheduleId, e.ShiftDate,
            e.DepartmentId, e.Department == null ? null : e.Department.NameTh,
            e.ClinicId, e.Clinic == null ? null : e.Clinic.NameTh,
            e.RoomLabel, e.PayKind, e.StartTime, e.EndTime, e.HourlyAmount, e.RequiredDoctors,
            e.RowVersion.ToString());

    public override IReadOnlyDictionary<string, Expression<Func<DutyShift, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DutyShift, object?>>>
        {
            ["shiftDate"] = e => e.ShiftDate,
            ["startTime"] = e => e.StartTime,
            ["roomLabel"] = e => e.RoomLabel,
            ["departmentName"] = e => e.Department == null ? null : e.Department.NameTh,
        };

    public override IQueryable<DutyShift> Search(IQueryable<DutyShift> q, ListRequest r)
    {
        if (r.Filter("scheduleId") is { } sid && Guid.TryParse(sid, out var scheduleId))
            q = q.Where(e => e.ScheduleId == scheduleId);

        if (r.Filter("dateFrom") is { } from && DateOnly.TryParse(from, out var dateFrom))
            q = q.Where(e => e.ShiftDate >= dateFrom);

        if (r.Filter("dateTo") is { } to && DateOnly.TryParse(to, out var dateTo))
            q = q.Where(e => e.ShiftDate <= dateTo);

        if (!string.IsNullOrWhiteSpace(r.Q))
        {
            var text = r.Q.Trim();
            q = q.Where(e => e.RoomLabel.Contains(text) ||
                (e.Department != null && e.Department.NameTh.Contains(text)));
        }

        return q;
    }

    public override void Apply(DutyShift e, DutyShiftInput input, bool isCreate)
    {
        if (isCreate) e.ScheduleId = input.ScheduleId;
        e.StartTime = input.StartTime;
        e.EndTime = input.EndTime;
        e.RequiredDoctors = input.RequiredDoctors;
        e.HourlyAmount = input.HourlyAmount;
    }

    public override Task ValidateAsync(DutyShift e, DutyShiftInput input, bool isCreate,
        ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.ScheduleId, "scheduleId", "ตารางเวร");

        if (input.RequiredDoctors < 1)
            errors.Add("requiredDoctors", "min", "จำนวนแพทย์ต่อเวรต้องอย่างน้อย 1 คน");

        if (input.StartTime == input.EndTime)
            errors.Add("endTime", "invalid_range", "เวลาสิ้นสุดต้องไม่เท่ากับเวลาเริ่ม");

        return Task.CompletedTask;
    }
}

public record DutyShiftDoctorRow(
    Guid Id,
    Guid ShiftId,
    Guid DoctorCodeId,
    string? DoctorCode,
    string? DoctorName,
    bool NoExam,
    DateTimeOffset? WorkStart,
    DateTimeOffset? WorkEnd,
    decimal? WorkHours,
    decimal? DeductAmount,
    string? Remark);

public record DutyShiftDoctorDetail(
    Guid Id,
    Guid ShiftId,
    Guid DoctorCodeId,

    string? DoctorCode,
    string? DoctorName,
    bool NoExam,
    DateTimeOffset? WorkStart,
    DateTimeOffset? WorkEnd,
    decimal? WorkHours,
    decimal? DeductAmount,
    string? Remark,
    string RowVersion);

public record DutyShiftDoctorInput(
    Guid ShiftId,
    Guid DoctorCodeId,
    bool NoExam,
    DateTimeOffset? WorkStart,
    DateTimeOffset? WorkEnd,
    decimal? WorkHours,
    decimal? DeductAmount,
    string? Remark);

public sealed class DutyShiftDoctorSpec
    : CrudSpec<DutyShiftDoctor, DutyShiftDoctorRow, DutyShiftDoctorDetail, DutyShiftDoctorInput>
{
    public override string Resource => "duty-shift-doctors";
    public override string DisplayNameTh => "การลงเวลาแพทย์";
    public override string Module => "duty-schedules";
    public override string DefaultSort => "doctorCode";

    public override IReadOnlyList<string> FilterKeys => ["shiftId", "doctorCodeId"];

    public override Expression<Func<DutyShiftDoctor, DutyShiftDoctorRow>> ListProjection =>
        e => new DutyShiftDoctorRow(e.Id, e.ShiftId, e.DoctorCodeId,
            e.DoctorCode == null ? null : e.DoctorCode.Code,
            null,
            e.NoExam, e.WorkStart, e.WorkEnd, e.WorkHours, e.DeductAmount, e.Remark);

    public override Expression<Func<DutyShiftDoctor, DutyShiftDoctorDetail>> DetailProjection =>
        e => new DutyShiftDoctorDetail(e.Id, e.ShiftId, e.DoctorCodeId,
            e.DoctorCode == null ? null : e.DoctorCode.Code,
            null,
            e.NoExam, e.WorkStart, e.WorkEnd, e.WorkHours, e.DeductAmount, e.Remark,
            e.RowVersion.ToString());

    public override async Task<IReadOnlyList<DutyShiftDoctorRow>> EnrichListAsync(
        IReadOnlyList<DutyShiftDoctorRow> items, ICrudRelatedData related, CancellationToken ct)
    {
        var names = await DoctorNamesAsync(items.Select(e => e.DoctorCodeId), related, ct);
        return items.Select(e => e with { DoctorName = names.GetValueOrDefault(e.DoctorCodeId) }).ToList();
    }

    public override async Task<DutyShiftDoctorDetail> EnrichDetailAsync(DutyShiftDoctorDetail item,
        ICrudRelatedData related, CancellationToken ct)
    {
        var names = await DoctorNamesAsync([item.DoctorCodeId], related, ct);
        return item with { DoctorName = names.GetValueOrDefault(item.DoctorCodeId) };
    }

    private static async Task<Dictionary<Guid, string>> DoctorNamesAsync(
        IEnumerable<Guid> doctorCodeIds, ICrudRelatedData related, CancellationToken ct)
    {
        var codeIds = doctorCodeIds.Distinct().ToArray();
        var codes = await related.ToListAsync(related.Query<DoctorCode>()
            .Where(e => codeIds.Contains(e.Id)).Select(e => new { e.Id, e.DoctorId }), ct);
        var doctorIds = codes.Select(e => e.DoctorId).Distinct().ToArray();
        var doctors = await related.Core.DoctorsAsync(doctorIds, ct);
        var names = doctors.ToDictionary(e => e.Id, e => e.Name);
        return codes.Where(e => names.ContainsKey(e.DoctorId)).ToDictionary(e => e.Id, e => names[e.DoctorId]);
    }

    public override IReadOnlyDictionary<string,
        Expression<Func<DutyShiftDoctor, object?>>> Sortable =>
        new Dictionary<string, Expression<Func<DutyShiftDoctor, object?>>>
        {
            ["doctorCode"] = e => e.DoctorCode == null ? null : e.DoctorCode.Code,
            ["workStart"] = e => e.WorkStart,
            ["workHours"] = e => e.WorkHours,
        };

    public override IQueryable<DutyShiftDoctor> Search(IQueryable<DutyShiftDoctor> q, ListRequest r)
    {
        if (r.Filter("shiftId") is { } sid && Guid.TryParse(sid, out var shiftId))
            q = q.Where(e => e.ShiftId == shiftId);

        if (r.Filter("doctorCodeId") is { } did && Guid.TryParse(did, out var doctorCodeId))
            q = q.Where(e => e.DoctorCodeId == doctorCodeId);

        return q;
    }

    public override void Apply(DutyShiftDoctor e, DutyShiftDoctorInput input, bool isCreate)
    {
        if (isCreate) e.ShiftId = input.ShiftId;
        e.DoctorCodeId = input.DoctorCodeId;
        e.NoExam = input.NoExam;
        e.WorkStart = input.WorkStart;
        e.WorkEnd = input.WorkEnd;

        e.WorkHours = input.NoExam
            ? 0m
            : input.WorkHours ?? Hours(input.WorkStart, input.WorkEnd);
        e.DeductAmount = input.DeductAmount;
        e.Remark = input.Remark?.Trim();
    }

    public override Task ValidateAsync(DutyShiftDoctor e, DutyShiftDoctorInput input,
        bool isCreate, ValidationFailure errors, CancellationToken ct)
    {
        MasterFieldRules.RequiredId(errors, input.ShiftId, "shiftId", "เวร");
        MasterFieldRules.RequiredId(errors, input.DoctorCodeId, "doctorCodeId", "แพทย์เข้าทำงาน");

        if (!input.NoExam && input.WorkStart is not null && input.WorkEnd is null)
            errors.Required("workEnd", "โปรดระบุเวลาสิ้นสุดการทำงาน");

        if (input.WorkStart is { } start && input.WorkEnd is { } end && end <= start)
            errors.Add("workEnd", "invalid_range", "เวลาสิ้นสุดต้องหลังเวลาเริ่มทำงาน");

        if (input.DeductAmount is > 0 && string.IsNullOrWhiteSpace(input.Remark))
            errors.Required("remark", "โปรดระบุหมายเหตุเมื่อมีการหักเงิน");

        return Task.CompletedTask;
    }

    private static decimal? Hours(DateTimeOffset? start, DateTimeOffset? end) =>
        start is { } s && end is { } e && e > s
            ? Math.Round((decimal)(e - s).TotalHours, 2, MidpointRounding.AwayFromZero)
            : null;
}
