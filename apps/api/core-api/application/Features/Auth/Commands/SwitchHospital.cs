using Ida.Application.Common;
using Ida.Domain.Auth;

namespace Ida.Application.Features.Auth.Commands;

public record SwitchHospitalCommand(string HospitalId) : ICommand<SessionDto>;

public class SwitchHospitalHandler(
    IRepository<AppUser> users,
    IRepository<AuthLog> authLogs,
    IQueryExecutor exec,
    IUnitOfWork uow,
    ICurrentUser current,
    IClock clock,
    IRequestContext request,
    SessionBuilder sessions)
    : ICommandHandler<SwitchHospitalCommand, SessionDto>
{
    public async Task<SessionDto> Handle(SwitchHospitalCommand command, CancellationToken ct)
    {
        if (!current.IsAuthenticated) throw ApiException.Unauthorized();

        if (string.IsNullOrWhiteSpace(command.HospitalId))
            new ValidationFailure()
                .Required("hospitalId", "โปรดเลือกโรงพยาบาล")
                .ThrowIfInvalid();

        var userId = current.UserId;
        var user = await exec.FirstOrDefaultAsync(users.Query().Where(u => u.Id == userId), ct)
            ?? throw ApiException.Unauthorized("user_not_found",
                "ไม่พบบัญชีผู้ใช้ กรุณาเข้าสู่ระบบใหม่");

        var session = await sessions.BuildAsync(user, command.HospitalId.Trim(), ct);

        authLogs.Add(new AuthLog
        {
            UserId = user.Id,
            Username = user.Username,
            Event = "SWITCH_HOSPITAL",
            HospitalId = session.HospitalId,
            Succeeded = true,
            ClientIp = request.ClientIp,
            UserAgent = request.UserAgent,
            OccurredAt = clock.Now,
        });
        await uow.SaveChangesAsync(ct);

        return session;
    }
}

