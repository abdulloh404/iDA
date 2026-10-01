using Ida.Application.Common;
using Ida.Infrastructure.Databases;

namespace Ida.Infrastructure.Security;

public class HttpTenantContext(DatabaseRegistry registry) : ITenantContext
{
    public string HospitalId => registry.FixedBranch.HospitalId!;
    public bool HasTenant => !string.IsNullOrEmpty(HospitalId);
    public IReadOnlyCollection<string> AllowedHospitals => [HospitalId];
}
