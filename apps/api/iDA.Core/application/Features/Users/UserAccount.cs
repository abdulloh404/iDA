using System.Security.Cryptography;
using Ida.Application.Common;
using Ida.Domain.Auth;
using Ida.Domain.Core;

namespace Ida.Application.Features.Users;

public record UserAccountDto(
    Guid UserId,
    string Username,
    string AuthType,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset? PasswordChangedAt,

    DateTimeOffset? PasswordExpiresAt,

    string PasswordState);

public record GetUserAccountQuery(Guid UserId) : IQuery<UserAccountDto>;

public class GetUserAccountHandler(
    IRepository<AppUser> users,
    IRepository<SysPasswordPolicy> policies,
    IQueryExecutor exec,
    IClock clock)
    : IQueryHandler<GetUserAccountQuery, UserAccountDto>
{
    public async Task<UserAccountDto> Handle(GetUserAccountQuery query, CancellationToken ct)
    {
        var user = await exec.FirstOrDefaultAsync(users.Query()
                .Where(u => u.Id == query.UserId)
                .Select(u => new
                {
                    u.Id, u.Username, u.AuthType, u.LastLoginAt, u.PasswordChangedAt,
                    HasPassword = u.PasswordHash != null && u.PasswordHash != AuthTypes.LockedPassword,
                }),
            ct)
            ?? throw ApiException.NotFound("users_not_found", "ไม่พบข้อมูลผู้ใช้งานที่ระบุ");

        var policy = await PasswordPolicy.LoadAsync(policies, exec, ct);
        var (expiresAt, state) = PasswordPolicy.Evaluate(user.AuthType, user.HasPassword,
            user.PasswordChangedAt, policy, clock.Now);

        return new UserAccountDto(user.Id, user.Username, user.AuthType, user.LastLoginAt,
            user.PasswordChangedAt, expiresAt, state);
    }
}

public record ResetUserPasswordCommand(Guid UserId) : ICommand<ResetUserPasswordResult>;

public record ResetUserPasswordResult(string TemporaryPassword, DateTimeOffset? PasswordExpiresAt);

public class ResetUserPasswordHandler(
    IRepository<AppUser> users,
    IRepository<SysPasswordPolicy> policies,
    IRepository<AuthLog> authLogs,
    IQueryExecutor exec,
    IUnitOfWork uow,
    IPasswordHasher passwords,
    ICurrentUser current,
    IRequestContext request,
    IClock clock)
    : ICommandHandler<ResetUserPasswordCommand, ResetUserPasswordResult>
{

    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
    private const int Length = 12;

    public async Task<ResetUserPasswordResult> Handle(ResetUserPasswordCommand command,
        CancellationToken ct)
    {
        var user = await exec.FirstOrDefaultAsync(
            users.Track().Where(u => u.Id == command.UserId), ct)
            ?? throw ApiException.NotFound("users_not_found", "ไม่พบข้อมูลผู้ใช้งานที่ระบุ");

        if (user.AuthType != AuthTypes.Local)
            throw ApiException.BadRequest("not_local_account",
                "บัญชีประเภท AD ใช้รหัสผ่านของ Active Directory — ตั้งรหัสผ่านจากระบบนี้ไม่ได้");

        var temporary = Generate();
        user.PasswordHash = passwords.Hash(temporary);
        user.PasswordChangedAt = clock.Now;

        authLogs.Add(new AuthLog
        {
            UserId = user.Id,
            Username = user.Username,
            Event = "PASSWORD_RESET",
            Succeeded = true,

            Reason = $"by {current.UserName}",
            ClientIp = request.ClientIp,
            UserAgent = request.UserAgent,
            OccurredAt = clock.Now,
        });
        await uow.SaveChangesAsync(ct);

        var policy = await PasswordPolicy.LoadAsync(policies, exec, ct);
        var (expiresAt, _) = PasswordPolicy.Evaluate(user.AuthType, hasPassword: true,
            user.PasswordChangedAt, policy, clock.Now);
        return new ResetUserPasswordResult(temporary, expiresAt);
    }

    private static string Generate() =>
        string.Create(Length, 0, (span, _) =>
        {
            for (var i = 0; i < span.Length; i++)
                span[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        });
}

internal static class PasswordPolicy
{
    public static async Task<SysPasswordPolicy?> LoadAsync(IRepository<SysPasswordPolicy> policies,
        IQueryExecutor exec, CancellationToken ct) =>
        await exec.FirstOrDefaultAsync(policies.Query(), ct);

    public static (DateTimeOffset? ExpiresAt, string State) Evaluate(string authType,
        bool hasPassword, DateTimeOffset? changedAt, SysPasswordPolicy? policy, DateTimeOffset now)
    {
        if (authType != AuthTypes.Local) return (null, "AD");
        if (!hasPassword) return (null, "NOT_SET");

        if (changedAt is null) return (null, "NO_EXPIRY");

        if (policy is null) return (null, "ACTIVE");

        var expiresAt = changedAt.Value.AddDays(policy.ResetDays);
        var state = now >= expiresAt ? "EXPIRED"
            : now >= expiresAt.AddDays(-policy.WarnDaysBefore) ? "EXPIRING"
            : "ACTIVE";
        return (expiresAt, state);
    }
}

