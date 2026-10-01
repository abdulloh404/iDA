using Ida.Application.Common;
using Ida.Domain.Auth;
using Ida.Domain.Common;

namespace Ida.Application.Features.Auth;

public record HospitalAccess(string HospitalId, string HospitalName, string RoleCode, string RoleNameTh, string TenantApiPath);

public record SessionUser(
    Guid Id,
    string Username,
    string DisplayName,
    string? Email,
    string? PhotoUrl);

public record SessionDto(
    string Token,
    DateTimeOffset ExpiresAt,
    SessionUser User,
    string HospitalId,
    string TenantApiPath,
    IReadOnlyList<HospitalAccess> Hospitals,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public class SessionBuilder(
    IRepository<UserHospitalRole> assignments,
    IRepository<RolePermission> rolePermissions,
    IQueryExecutor exec,
    ITokenService tokens,
    ITenantApiDirectory tenantApis)
{

    public async Task<SessionDto> BuildAsync(AppUser user, string? preferredHospitalId,
        CancellationToken ct)
    {
        var access = await exec.ToListAsync(
            assignments.Query()

                .Where(a => a.UserId == user.Id && a.Status == RecordStatus.Active &&
                            a.Role!.Status == RecordStatus.Active)
                .Select(a => new
                {
                    a.HospitalId,
                    HospitalName = a.Hospital!.HospitalNameTh,
                    a.RoleId,
                    RoleCode = a.Role!.Code,
                    RoleNameTh = a.Role!.NameTh,
                    a.IsDefault,
                })
                .OrderByDescending(a => a.IsDefault)
                .ThenBy(a => a.HospitalId),
            ct);

        if (access.Count == 0)
            throw ApiException.Forbidden("no_hospital_access",
                "บัญชีผู้ใช้นี้ยังไม่ได้รับสิทธิ์เข้าใช้งานโรงพยาบาลใด กรุณาติดต่อผู้ดูแลระบบ");

        var chosen = preferredHospitalId is null
            ? access[0]
            : access.FirstOrDefault(a => a.HospitalId == preferredHospitalId)
              ?? throw ApiException.Forbidden("hospital_not_allowed",
                  "คุณไม่มีสิทธิ์เข้าใช้งานโรงพยาบาลที่เลือก");

        var permissions = await exec.ToListAsync(
            rolePermissions.Query()
                .Where(rp => rp.RoleId == chosen.RoleId)
                .Select(rp => rp.Permission!.Code)
                .Distinct(),
            ct);

        var paths = await tenantApis.PathsAsync(ct);
        if (!paths.TryGetValue(chosen.HospitalId, out var tenantPath))
            throw new ApiException(503, "tenant_unavailable", "โรงพยาบาลที่เลือกยังไม่ได้ตั้งค่า Tenant API");
        var hospitals = access.Where(a => paths.ContainsKey(a.HospitalId))
            .Select(a => new HospitalAccess(a.HospitalId, a.HospitalName, a.RoleCode, a.RoleNameTh, paths[a.HospitalId]))
            .ToList();

        var issued = tokens.Issue(new TokenSubject(
            user.Id,
            user.Username,
            user.DisplayName,
            chosen.HospitalId,
            [.. hospitals.Select(h => h.HospitalId)],
            [chosen.RoleCode],
            permissions));

        return new SessionDto(
            issued.Token,
            issued.ExpiresAt,
            new SessionUser(user.Id, user.Username, user.DisplayName, user.Email, user.PhotoUrl),
            chosen.HospitalId,
            tenantPath,
            hospitals,
            [chosen.RoleCode],
            permissions);
    }
}
