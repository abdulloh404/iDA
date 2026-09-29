using Ida.Application.Common;
using Ida.Domain.Bu;
using Ida.Domain.Common;

namespace Ida.Application.Features.DoctorFee402;

public record DecideDoctorFeesCommand(

    string Resource,
    IReadOnlyList<Guid> Ids,
    string Action,
    string? Comment) : ICommand<DecideDoctorFeesResult>;

public record DecideDoctorFeesResult(int Decided, ApprovalStatus Status);

public class DecideDoctorFeesHandler(
    IRepository<DfExternalFee> externalFees,
    IRepository<DfExternalFeeLine> externalFeeLines,
    IRepository<DfFeeItem> feeItems,
    IQueryExecutor exec,
    IUnitOfWork uow,
    ICurrentUser user,
    IClock clock)
    : ICommandHandler<DecideDoctorFeesCommand, DecideDoctorFeesResult>
{
    private static readonly Dictionary<string, ApprovalStatus> Actions = new()
    {
        ["APPROVE"] = ApprovalStatus.Approved,
        ["REJECT"] = ApprovalStatus.Rejected,
        ["RETURN"] = ApprovalStatus.Returned,
    };

    public async Task<DecideDoctorFeesResult> Handle(DecideDoctorFeesCommand command,
        CancellationToken ct)
    {
        var action = command.Action?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!Actions.TryGetValue(action, out var target))
            throw ApiException.BadRequest("unknown_action",
                "การดำเนินการต้องเป็น APPROVE, REJECT หรือ RETURN เท่านั้น");

        var ids = command.Ids?.Distinct().ToList() ?? [];
        if (ids.Count == 0)
            new ValidationFailure().Required("ids", "โปรดเลือกรายการที่ต้องการดำเนินการ")
                .ThrowIfInvalid();

        if (action is "REJECT" or "RETURN" && string.IsNullOrWhiteSpace(command.Comment))
            new ValidationFailure().Required("comment", "โปรดระบุเหตุผลให้ผู้บันทึกรายการทราบ")
                .ThrowIfInvalid();

        List<IDecidableFee> rows = command.Resource switch
        {
            "external-fees" => [.. await exec.ToListAsync(
                externalFees.Track().Where(e => ids.Contains(e.Id)), ct)],
            "fee-items" => [.. await exec.ToListAsync(
                feeItems.Track().Where(e => ids.Contains(e.Id)), ct)],
            _ => throw ApiException.BadRequest("unknown_resource",
                "ไม่รองรับการอนุมัติรายการประเภทนี้"),
        };

        if (rows.Count != ids.Count)
            throw ApiException.NotFound("fees_not_found",
                "บางรายการที่เลือกไม่พบในระบบแล้ว กรุณาโหลดรายการใหม่");

        var notPending = rows.Count(r => r.ApprovalStatus != ApprovalStatus.Pending);
        if (notPending > 0)
            throw ApiException.Conflict("not_pending",
                $"มี {notPending} รายการที่ไม่ได้อยู่ในสถานะรออนุมัติ กรุณาโหลดรายการใหม่");

        if (action == "APPROVE" && command.Resource == "external-fees")
        {
            var withLines = await exec.ToListAsync(
                externalFeeLines.Query().Where(l => ids.Contains(l.FeeId))
                    .Select(l => l.FeeId).Distinct(), ct);
            var empty = ids.Count - withLines.Count;
            if (empty > 0)
                throw ApiException.Conflict("no_lines",
                    $"มี {empty} เอกสารที่ยังไม่มีรายการแพทย์ อนุมัติไม่ได้");
        }

        var now = clock.Now;
        foreach (var row in rows)
        {
            row.ApprovalStatus = target;
            row.DecisionComment = command.Comment?.Trim();
            row.DecidedBy = user.UserName;
            row.DecidedAt = now;
        }

        await uow.SaveChangesAsync(ct);
        return new DecideDoctorFeesResult(rows.Count, target);
    }
}

