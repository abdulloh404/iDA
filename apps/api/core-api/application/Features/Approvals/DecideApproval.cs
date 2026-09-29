using Ida.Application.Common;
using Ida.Domain.Bu;
using Ida.Domain.Common;
using MediatR;

namespace Ida.Application.Features.Approvals;

public class DecideApprovalHandler(
    IRepository<DoctorApprovalRequest> requests,
    IRepository<DoctorApprovalStep> steps,
    IQueryExecutor exec,
    IUnitOfWork uow,
    ICurrentUser user,
    IClock clock,
    ISender mediator)
    : ICommandHandler<DecideApprovalCommand, ApprovalRequestDetail>
{
    private static readonly string[] Actions = ["APPROVE", "REJECT", "RETURN"];

    public async Task<ApprovalRequestDetail> Handle(DecideApprovalCommand command,
        CancellationToken ct)
    {
        var action = command.Action?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!Actions.Contains(action))
            throw ApiException.BadRequest("unknown_action",
                "การกระทำต้องเป็น APPROVE, REJECT หรือ RETURN เท่านั้น");

        var request = await exec.FirstOrDefaultAsync(
            requests.Track().Where(e => e.Id == command.Id), ct)
            ?? throw ApprovalQuery.NotFound();

        if (request.CurrentStatus != ApprovalStatus.Pending)
            throw ApiException.Conflict("approval_closed",
                "คำขอนี้ปิดไปแล้ว ไม่สามารถดำเนินการซ้ำได้");

        var open = await exec.ToListAsync(
            steps.Track().Where(s => s.RequestId == command.Id && s.Action == null)
                 .OrderBy(s => s.StepSeq), ct);

        var current = open.FirstOrDefault()
            ?? throw ApiException.Conflict("no_open_step",
                "คำขอนี้ไม่มีขั้นตอนที่รอการตัดสิน");

        if (!ApprovalQuery.MayDecide(current.ApproverRole, user))
            throw ApiException.Forbidden("not_your_step",
                $"ขั้นตอนนี้รอ{ApprovalLabels.ApproverRoleTh(current.ApproverRole)}เป็นผู้ตัดสิน");

        if (action is "REJECT" or "RETURN" && string.IsNullOrWhiteSpace(command.Comment))
            new ValidationFailure()
                .Required("comment", "โปรดระบุเหตุผลให้ผู้ส่งคำขอทราบ")
                .ThrowIfInvalid();

        var now = clock.Now;
        current.Action = action;
        current.ActionAt = now;
        current.ApproverUser = user.UserName;
        current.Comment = command.Comment?.Trim();

        request.CurrentStatus = action switch
        {
            "REJECT" => ApprovalStatus.Rejected,
            "RETURN" => ApprovalStatus.Returned,

            _ when open.Count > 1 => ApprovalStatus.Pending,
            _ => ApprovalStatus.Approved,
        };

        if (request.CurrentStatus != ApprovalStatus.Pending)
            request.ClosedAt = now;

        if (request.CurrentStatus == ApprovalStatus.Approved)
            await ApplyPayloadAsync(request, ct);

        await uow.SaveChangesAsync(ct);

        return await mediator.Send(new GetApprovalRequestQuery(command.Id), ct);
    }

    private Task ApplyPayloadAsync(DoctorApprovalRequest request, CancellationToken ct)
    {
        _ = request;
        _ = ct;
        return Task.CompletedTask;
    }
}

