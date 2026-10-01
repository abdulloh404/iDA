using Ida.Application.Common;
using Ida.Domain.Auth;
using Ida.Domain.Common;

namespace Ida.Application.Features.Auth.Commands;

public record LoginCommand(string Username, string Password) : ICommand<SessionDto>;

public class LoginHandler(
    IRepository<AppUser> users,
    IRepository<AuthLog> authLogs,
    IQueryExecutor exec,
    IUnitOfWork uow,
    IPasswordHasher passwords,
    IClock clock,
    SessionBuilder sessions,
    IRequestContext request,
    IUserActivity activity,
    IRepository<Ida.Domain.Core.SysPasswordPolicy> policies)
    : ICommandHandler<LoginCommand, SessionDto>
{
    public async Task<SessionDto> Handle(LoginCommand command, CancellationToken ct)
    {
        var errors = new ValidationFailure();
        if (string.IsNullOrWhiteSpace(command.Username))
            errors.Required("username", "โปรดกรอกชื่อผู้ใช้");
        if (string.IsNullOrWhiteSpace(command.Password))
            errors.Required("password", "โปรดกรอกรหัสผ่าน");
        errors.ThrowIfInvalid();

        var username = command.Username.Trim();

        var user = await exec.FirstOrDefaultAsync(
            users.Query().Where(u => u.Username == username), ct);

        if (user is null
            || user.Status != RecordStatus.Active
            || user.AuthType != "LOCAL"
            || user.PasswordHash is null
            || !passwords.Verify(command.Password, user.PasswordHash))
        {
            await RecordAsync(user?.Id, username, "LOGIN_FAILED", succeeded: false,
                reason: user is null ? "unknown_user"
                    : user.Status != RecordStatus.Active ? "inactive"
                    : "bad_password",
                hospitalId: null, ct);

            throw ApiException.Unauthorized("invalid_credentials",
                "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง");
        }

        var policy = await Users.PasswordPolicy.LoadAsync(policies, exec, ct);
        if (Users.PasswordPolicy.Evaluate(user.AuthType, hasPassword: true,
                user.PasswordChangedAt, policy, clock.Now)
                .State == "EXPIRED")
        {
            await RecordAsync(user.Id, username, "LOGIN_FAILED", succeeded: false,
                reason: "password_expired", hospitalId: null, ct);
            throw ApiException.Unauthorized("password_expired",
                "รหัสผ่านหมดอายุแล้ว กรุณาติดต่อผู้ดูแลระบบเพื่อตั้งรหัสผ่านใหม่");
        }

        var session = await sessions.BuildAsync(user, preferredHospitalId: null, ct);

        await activity.MarkLoginAsync(user.Id, clock.Now, ct);
        await RecordAsync(user.Id, username, "LOGIN", succeeded: true, reason: null,
            session.HospitalId, ct);

        return session;
    }

    private async Task RecordAsync(Guid? userId, string username, string @event, bool succeeded,
        string? reason, string? hospitalId, CancellationToken ct)
    {
        authLogs.Add(new AuthLog
        {
            UserId = userId,
            Username = username,
            Event = @event,
            HospitalId = hospitalId,
            Succeeded = succeeded,
            Reason = reason,
            ClientIp = request.ClientIp,
            UserAgent = request.UserAgent,
            OccurredAt = clock.Now,
        });

        await uow.SaveChangesAsync(ct);
    }
}

