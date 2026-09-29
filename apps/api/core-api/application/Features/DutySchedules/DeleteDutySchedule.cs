using Ida.Application.Common;
using Ida.Domain.Bu;
using Ida.Domain.Common;
using MediatR;

namespace Ida.Application.Features.DutySchedules;

public record DeleteDutyScheduleCommand(Guid Id) : ICommand<Unit>;

public class DeleteDutyScheduleHandler(
    IRepository<DutySchedule> schedules,
    IRepository<DutyShift> shifts,
    IRepository<DutyShiftDoctor> shiftDoctors,
    IQueryExecutor exec,
    IUnitOfWork uow)
    : ICommandHandler<DeleteDutyScheduleCommand, Unit>
{
    public async Task<Unit> Handle(DeleteDutyScheduleCommand command, CancellationToken ct)
    {
        var schedule = await DutyScheduleQuery.TrackedAsync(schedules, exec, command.Id, ct);

        if (schedule.Status == DutyScheduleStatus.Calculated)
            throw ApiException.EntityInUse(
                "ลบไม่ได้ เพราะตารางเวรนี้ถูกคำนวณรายเดือนไปแล้ว ให้ถอนการคำนวณก่อน");

        var shiftRows = await exec.ToListAsync(
            shifts.Track().Where(s => s.ScheduleId == schedule.Id), ct);
        var shiftIds = shiftRows.Select(s => s.Id).ToHashSet();

        var doctorRows = await exec.ToListAsync(
            shiftDoctors.Track().Where(d => shiftIds.Contains(d.ShiftId)), ct);

        foreach (var row in doctorRows) shiftDoctors.Remove(row);
        foreach (var row in shiftRows) shifts.Remove(row);
        schedules.Remove(schedule);

        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

