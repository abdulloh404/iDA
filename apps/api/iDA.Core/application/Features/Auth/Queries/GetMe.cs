using Ida.Application.Common;
using Ida.Domain.Auth;

namespace Ida.Application.Features.Auth.Queries;

public record MeDto(
    SessionUser User,
    string HospitalId,
    IReadOnlyList<HospitalAccess> Hospitals,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public record GetMeQuery : IQuery<MeDto>;

public class GetMeHandler(
    IRepository<AppUser> users,
    IQueryExecutor exec,
    ICurrentUser current,
    ITenantContext tenant,
    SessionBuilder sessions)
    : IQueryHandler<GetMeQuery, MeDto>
{
    public async Task<MeDto> Handle(GetMeQuery query, CancellationToken ct)
    {
        if (!current.IsAuthenticated)
            throw ApiException.Unauthorized();

        var userId = current.UserId;
        var user = await exec.FirstOrDefaultAsync(users.Query().Where(u => u.Id == userId), ct)
            ?? throw ApiException.Unauthorized("user_not_found",
                "ไม่พบบัญชีผู้ใช้ กรุณาเข้าสู่ระบบใหม่");

        var session = await sessions.BuildAsync(user, tenant.HospitalId, ct);

        return new MeDto(session.User, session.HospitalId, session.Hospitals,
            session.Roles, session.Permissions);
    }
}

