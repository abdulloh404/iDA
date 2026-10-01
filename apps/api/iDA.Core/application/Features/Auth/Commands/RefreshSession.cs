using Ida.Application.Common;
using Ida.Domain.Auth;
using Ida.Domain.Common;

namespace Ida.Application.Features.Auth.Commands;

public record RefreshSessionCommand : ICommand<SessionDto>;

public class RefreshSessionHandler(
    IRepository<AppUser> users,
    IQueryExecutor exec,
    ICurrentUser current,
    ITenantContext tenant,
    SessionBuilder sessions)
    : ICommandHandler<RefreshSessionCommand, SessionDto>
{
    public async Task<SessionDto> Handle(RefreshSessionCommand command, CancellationToken ct)
    {
        if (!current.IsAuthenticated) throw ApiException.Unauthorized();

        var userId = current.UserId;
        var user = await exec.FirstOrDefaultAsync(users.Query().Where(u => u.Id == userId), ct);
        if (user is null || user.Status != RecordStatus.Active)
            throw ApiException.Unauthorized("session_revoked",
                "บัญชีผู้ใช้ถูกระงับหรือถูกลบ กรุณาเข้าสู่ระบบใหม่");

        try
        {
            return await sessions.BuildAsync(user,
                tenant.HasTenant ? tenant.HospitalId : null, ct);
        }
        catch (ApiException) when (tenant.HasTenant)
        {
            return await sessions.BuildAsync(user, preferredHospitalId: null, ct);
        }
    }
}

