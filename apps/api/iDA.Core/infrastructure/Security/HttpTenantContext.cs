using System.Security.Claims;
using Ida.Application.Common;
using Microsoft.AspNetCore.Http;

namespace Ida.Infrastructure.Security;

public class HttpTenantContext(IHttpContextAccessor accessor) : ITenantContext
{
    public string HospitalId =>
        accessor.HttpContext?.User.FindFirstValue(IdaClaims.HospitalId) ?? string.Empty;

    public bool HasTenant => !string.IsNullOrEmpty(HospitalId);

    public IReadOnlyCollection<string> AllowedHospitals =>
        accessor.HttpContext?.User.FindAll(IdaClaims.Hospitals).Select(c => c.Value).ToArray() ?? [];
}

