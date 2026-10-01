using System.Security.Claims;
using Ida.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Ida.Infrastructure.Security;

public class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid UserId =>
        Guid.TryParse(Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
            ? id
            : Guid.Empty;

    public string UserName =>
        Principal?.FindFirstValue(JwtRegisteredClaimNames.UniqueName) ?? "system";

    public string DisplayName =>
        Principal?.FindFirstValue(IdaClaims.DisplayName) ?? UserName;

    public IReadOnlyCollection<string> Permissions =>
        Principal?.FindAll(IdaClaims.Permission).Select(c => c.Value).ToHashSet(StringComparer.Ordinal)
        ?? [];

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet(StringComparer.Ordinal)
        ?? [];

    public bool Can(string permission) => Permissions.Contains(permission);

    public bool IsInRole(string role) => Roles.Contains(role);
}

public class HttpTenantContext(IHttpContextAccessor accessor) : ITenantContext
{
    public string HospitalId =>
        accessor.HttpContext?.User.FindFirstValue(IdaClaims.HospitalId) ?? string.Empty;

    public bool HasTenant => !string.IsNullOrEmpty(HospitalId);

    public IReadOnlyCollection<string> AllowedHospitals =>
        accessor.HttpContext?.User.FindAll(IdaClaims.Hospitals).Select(c => c.Value).ToArray() ?? [];
}

public class HttpRequestContext(IHttpContextAccessor accessor) : IRequestContext
{
    public string? ClientIp => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public string? UserAgent => accessor.HttpContext?.Request.Headers.UserAgent.ToString();
    public string? TraceId => accessor.HttpContext?.TraceIdentifier;
}
