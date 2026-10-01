using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.DutySchedules;

public record GenerateDutyScheduleCommand(DutyScheduleKind Kind, int Year, int Month)
    : ICommand<DutyScheduleDetail>;

public class GenerateDutyScheduleHandler(
    IRepository<DutySchedule> schedules,
    IRepository<DutyShift> shifts,
    IRepository<DutyShiftDoctor> shiftDoctors,
    IRepository<DutyRate> dutyRates,
    IRepository<DutyRateDay> dutyRateDays,
    IRepository<GuaranteeRate> guaranteeRates,
    IRepository<GuaranteeRateDay> guaranteeRateDays,
    IQueryExecutor exec,
    IUnitOfWork uow)
    : ICommandHandler<GenerateDutyScheduleCommand, DutyScheduleDetail>
{
    public async Task<DutyScheduleDetail> Handle(GenerateDutyScheduleCommand command,
        CancellationToken ct)
    {
        var errors = new ValidationFailure();
        if (command.Month is < 1 or > 12)
            errors.Add("month", "invalid", "เดือนต้องอยู่ระหว่าง 1 ถึง 12");

        if (command.Year is < 2000 or > 2100)
            errors.Add("year", "invalid", "ปีต้องเป็น ค.ศ. และอยู่ระหว่าง 2000 ถึง 2100");
        errors.ThrowIfInvalid();

        var exists = await exec.AnyAsync(
            schedules.Query().Where(e =>
                e.Kind == command.Kind && e.Year == command.Year && e.Month == command.Month), ct);
        if (exists)
            throw ApiException.Conflict("duty_schedule_exists",
                $"มีตารางเวร{DutyScheduleLabels.KindTh(command.Kind)}ของเดือนนี้อยู่แล้ว");

        var schedule = new DutySchedule
        {
            Kind = command.Kind,
            Year = command.Year,
            Month = command.Month,
            Status = DutyScheduleStatus.Draft,
        };
        schedules.Add(schedule);

        var days = DaysOf(command.Year, command.Month);
        var created = command.Kind == DutyScheduleKind.Duty
            ? await GenerateFromDutyRatesAsync(schedule, days, ct)
            : await GenerateFromGuaranteeRatesAsync(schedule, command.Kind, days, ct);

        await uow.SaveChangesAsync(ct);

        return new DutyScheduleDetail(schedule.Id, schedule.Kind, schedule.Year, schedule.Month,
            schedule.Status, null, null, null, null, null, null,

            created == 0
                ? "ยังไม่มีอัตราที่ตรงกับเดือนนี้ จึงยังไม่มีเวรในตาราง"
                : null,
            schedule.RowVersion.ToString());
    }

    private async Task<int> GenerateFromDutyRatesAsync(DutySchedule schedule,
        IReadOnlyList<DateOnly> days, CancellationToken ct)
    {
        var rates = await exec.ToListAsync(
            dutyRates.Query().Where(e => e.Status == RecordStatus.Active), ct);
        if (rates.Count == 0) return 0;

        var rateIds = rates.Select(r => r.Id).ToHashSet();
        var dayRates = await exec.ToListAsync(
            dutyRateDays.Query().Where(e => rateIds.Contains(e.DutyRateId)), ct);

        var byRate = dayRates
            .GroupBy(d => d.DutyRateId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(d => (int)d.DayOfWeek));

        var created = 0;
        foreach (var rate in rates)
        {
            if (!byRate.TryGetValue(rate.Id, out var perDay)) continue;

            foreach (var date in days)
            {
                if (!perDay.TryGetValue((int)date.DayOfWeek, out var day)) continue;

                var amount = day.BoardHourlyAmount ?? day.NonBoardHourlyAmount;
                if (amount is null or 0) continue;

                shifts.Add(new DutyShift
                {
                    Schedule = schedule,
                    ShiftDate = date,
                    DepartmentId = rate.DepartmentId,
                    DutyRateId = rate.Id,
                    RoomLabel = RoomLabel(rate),
                    PayKind = rate.PayKind,
                    StartTime = rate.StartTime,
                    EndTime = rate.EndTime,
                    HourlyAmount = amount,
                });
                created++;
            }
        }

        return created;
    }

    private async Task<int> GenerateFromGuaranteeRatesAsync(DutySchedule schedule,
        DutyScheduleKind kind, IReadOnlyList<DateOnly> days, CancellationToken ct)
    {
        var guaranteeKind = kind switch
        {
            DutyScheduleKind.GuaranteeHourly => GuaranteeKind.Hourly,
            DutyScheduleKind.GuaranteeSession => GuaranteeKind.PerSession,
            DutyScheduleKind.GuaranteeMonthly => GuaranteeKind.Monthly,
            _ => throw ApiException.BadRequest("unknown_kind", "ชนิดของตารางเวรไม่ถูกต้อง"),
        };

        var first = days[0];
        var last = days[^1];

        var rates = await exec.ToListAsync(
            guaranteeRates.Query().Where(e =>
                e.Kind == guaranteeKind &&
                e.Status == RecordStatus.Active &&

                e.StartDate <= last &&
                (e.EndDate == null || e.EndDate >= first)), ct);
        if (rates.Count == 0) return 0;

        var rateIds = rates.Select(r => r.Id).ToHashSet();
        var dayRates = await exec.ToListAsync(
            guaranteeRateDays.Query().Where(e => rateIds.Contains(e.GuaranteeRateId)), ct);

        var byRate = dayRates
            .GroupBy(d => d.GuaranteeRateId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(d => (int)d.DayOfWeek));

        var created = 0;
        foreach (var rate in rates)
        {
            if (!byRate.TryGetValue(rate.Id, out var perDay)) continue;

            foreach (var date in days)
            {
                if (date < rate.StartDate || (rate.EndDate is { } end && date > end)) continue;
                if (!perDay.TryGetValue((int)date.DayOfWeek, out var day)) continue;

                if (day.IsExcluded) continue;

                var shift = new DutyShift
                {
                    Schedule = schedule,
                    ShiftDate = date,
                    DepartmentId = rate.DepartmentId,
                    GuaranteeRateId = rate.Id,
                    RoomLabel = DutyScheduleLabels.KindTh(kind),
                    PayKind = DutyPayKind.Normal,
                    StartTime = day.StartTime ?? new TimeOnly(8, 0),
                    EndTime = day.EndTime ?? new TimeOnly(16, 0),
                    HourlyAmount = day.IncomeAmount ?? rate.IncomeAmount,
                };
                shifts.Add(shift);

                shiftDoctors.Add(new DutyShiftDoctor
                {
                    Shift = shift,
                    DoctorCodeId = rate.DoctorCodeId,
                });
                created++;
            }
        }

        return created;
    }

    private static string RoomLabel(DutyRate rate) =>
        rate.Room == DutyRoom.Other
            ? rate.RoomOther ?? "อื่น ๆ"
            : rate.Room switch
            {
                DutyRoom.Room1 => "ห้อง 1",
                DutyRoom.Room2 => "ห้อง 2",
                DutyRoom.Room3 => "ห้อง 3",
                DutyRoom.Room4 => "ห้อง 4",
                DutyRoom.Room5 => "ห้อง 5",
                _ => rate.Room.ToString(),
            };

    private static IReadOnlyList<DateOnly> DaysOf(int year, int month) =>
        [.. Enumerable.Range(1, DateTime.DaysInMonth(year, month))
            .Select(day => new DateOnly(year, month, day))];
}

