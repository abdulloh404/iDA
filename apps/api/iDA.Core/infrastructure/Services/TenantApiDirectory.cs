using System.Text.RegularExpressions;
using Ida.Application.Common;
using Microsoft.Extensions.Configuration;

namespace Ida.Infrastructure.Services;

public sealed class TenantApiDirectory(IConfiguration configuration) : ITenantApiDirectory
{
    public IReadOnlyDictionary<string, string> Tenants()
    {
        var tenants = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var tenant in configuration.GetSection("Api:Tenants").GetChildren().OrderBy(tenant => tenant.Key.Length).ThenBy(tenant => tenant.Key, StringComparer.Ordinal))
        {
            var key = tenant.Key.ToUpperInvariant();
            var hospitalId = tenant["HospitalId"] ?? configuration[$"{key}_HOSPITAL_ID"] ?? key;
            if (!Regex.IsMatch(key, "^BU[0-9]+$") || string.IsNullOrWhiteSpace(hospitalId) || hospitalId.Length > 20 || tenants.ContainsValue(hospitalId))
                throw new InvalidOperationException($"Api:Tenants:{key} must use a BU connection key and a unique hospital ID of at most 20 characters.");
            tenants.Add(key, hospitalId);
        }
        return tenants;
    }

    public Task<IReadOnlyDictionary<string, string>> PathsAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var paths = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, hospitalId) in Tenants())
        {
            var path = configuration[$"Api:Tenants:{key}:PathBase"] ?? configuration[$"{key}_API_PATH_BASE"] ?? "/" + key.ToLowerInvariant();
            if (!Regex.IsMatch(path, "^/[a-z0-9][a-z0-9-]*$") || path is "/core" or "/api" || paths.ContainsValue(path))
                throw new InvalidOperationException($"Api:Tenants:{key}:PathBase must be a unique path such as /pt1, different from /core and /api.");
            paths.Add(hospitalId, path);
        }
        return Task.FromResult<IReadOnlyDictionary<string, string>>(paths);
    }

    public string Url(string key)
    {
        var url = configuration[$"Api:Tenants:{key}:Url"] ?? configuration[$"{key}_API_URL"]
            ?? throw new InvalidOperationException($"Set Api:Tenants:{key}:Url on the Core API.");
        var pathBase = configuration[$"Api:Tenants:{key}:PathBase"] ?? configuration[$"{key}_API_PATH_BASE"] ?? "/" + key.ToLowerInvariant();
        return ServiceApiClient.WithPathBase(url, pathBase);
    }

    public string ServiceKey(string key) => ServiceApiClient.ReadDestinationKey(configuration, $"Api:Tenants:{key}:ServiceKey");
}
