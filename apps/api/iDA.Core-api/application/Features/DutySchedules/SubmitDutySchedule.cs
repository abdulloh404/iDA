using Ida.Application.Common;
using Ida.Domain.Bu;
using Ida.Domain.Common;
using MediatR;

namespace Ida.Application.Features.DutySchedules;

public record SubmitDutyScheduleCommand(Guid Id) : ICommand<DutyScheduleDetail>;

public record RejectDutyScheduleCommand(Guid Id, string? Reason) : ICommand<DutyScheduleDetail>;

public class SubmitDutyScheduleHandler(
    IRepository<DutySchedule> schedules,
    IRepository<DutyShift> shifts,
    IQueryExecutor exec,
    IUnitOfWork uow,
    ICurrentUser user,
    IClock clock,
    ISender mediator)
    : ICommandHandler<SubmitDutyScheduleCommand, DutyScheduleDetail>
{
    public async Task<DutyScheduleDetail> Handle(SubmitDutyScheduleCommand command,
        CancellationToken ct)
    {
        var schedule = await DutyScheduleQuery.TrackedAsync(schedules, exec, command.Id, ct);

        if (schedule.Status is DutyScheduleStatus.Submitted or DutyScheduleStatus.Calculated)
            throw ApiException.Conflict("already_submitted",
                $"ตารางเวรนี้อยู่ในสถานะ{DutyScheduleLabels.StatusTh(schedule.Status)}แล้ว");

        var incomplete = await exec.CountAsync(
            shifts.Query().Where(s => s.ScheduleId == schedule.Id &&
                s.Doctors.Count(d => d.DeletedAt == null && (d.NoExam || d.WorkEnd != null))
                    < s.RequiredDoctors), ct);

        if (incomplete > 0)
            throw ApiException.Conflict("schedule_incomplete",
                $"โปรดบันทึกตารางเวรให้ครบถ้วน ยังมี {incomplete} เวรที่ลงเวลาไม่ครบ");

        schedule.Status = DutyScheduleStatus.Submitted;
        schedule.SubmittedAt = clock.Now;
        schedule.SubmittedBy = user.UserName;

        schedule.RejectedAt = null;
        schedule.RejectedBy = null;
        schedule.RejectReason = null;

        await uow.SaveChangesAsync(ct);
        return await mediator.Send(new GetDutyScheduleQuery(schedule.Id), ct);
    }
}

public class RejectDutyScheduleHandler(
    IRepository<DutySchedule> schedules,
    IQueryExecutor exec,
    IUnitOfWork uow,
    ICurrentUser user,
    IClock clock,
    ISender mediator)
    : ICommandHandler<RejectDutyScheduleCommand, DutyScheduleDetail>
{
    public async Task<DutyScheduleDetail> Handle(RejectDutyScheduleCommand command,
        CancellationToken ct)
    {
        var schedule = await DutyScheduleQuery.TrackedAsync(schedules, exec, command.Id, ct);

        if (schedule.Status == DutyScheduleStatus.Calculated)
            throw ApiException.Conflict("calculation_not_withdrawn",
                "โปรดถอนคำนวณรายเดือนก่อน จึงจะตีกลับตารางเวรได้");

        if (schedule.Status != DutyScheduleStatus.Submitted)
            throw ApiException.Conflict("not_submitted",
                "ตีกลับได้เฉพาะตารางเวรที่ส่งให้บัญชีแล้ว");

        if (string.IsNullOrWhiteSpace(command.Reason))
            new ValidationFailure()
                .Required("reason", "โปรดระบุเหตุผลให้สำนักแพทย์ทราบ")
                .ThrowIfInvalid();

        schedule.Status = DutyScheduleStatus.Rejected;
        schedule.RejectedAt = clock.Now;
        schedule.RejectedBy = user.UserName;
        schedule.RejectReason = command.Reason!.Trim();

        await uow.SaveChangesAsync(ct);
        return await mediator.Send(new GetDutyScheduleQuery(schedule.Id), ct);
    }
}

public record GetDutyScheduleQuery(Guid Id) : IQuery<DutyScheduleDetail>;

public class GetDutyScheduleHandler(IRepository<DutySchedule> schedules, IQueryExecutor exec)
    : IQueryHandler<GetDutyScheduleQuery, DutyScheduleDetail>
{
    public async Task<DutyScheduleDetail> Handle(GetDutyScheduleQuery query, CancellationToken ct)
    {
        var spec = new DutyScheduleSpec();
        return await exec.FirstOrDefaultAsync(
            schedules.Query().Where(e => e.Id == query.Id).Select(spec.DetailProjection), ct)
            ?? throw DutyScheduleQuery.NotFound();
    }
}

internal static class DutyScheduleQuery
{
    public static ApiException NotFound() =>
        ApiException.NotFound("duty_schedules_not_found", "ไม่พบตารางเวรที่ระบุ");

    public static async Task<DutySchedule> TrackedAsync(IRepository<DutySchedule> schedules,
        IQueryExecutor exec, Guid id, CancellationToken ct) =>
        await exec.FirstOrDefaultAsync(schedules.Track().Where(e => e.Id == id), ct)
        ?? throw NotFound();
}

