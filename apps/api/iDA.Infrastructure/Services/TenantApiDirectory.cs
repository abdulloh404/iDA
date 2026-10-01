using System.Text.RegularExpressions;
using Ida.Application.Common;
using Ida.Infrastructure.Databases;
using Microsoft.Extensions.Configuration;

namespace Ida.Infrastructure.Services;

public sealed class TenantApiDirectory(DatabaseRegistry registry, IConfiguration configuration) : ITenantApiDirectory
{
    public async Task<IReadOnlyDictionary<string, string>> PathsAsync(CancellationToken ct)
    {
        var paths = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var branch in await registry.ListBranchesAsync(ct))
        {
            var path = configuration[$"{branch.ConnectionKey}_API_PATH"] ?? "/" + branch.ConnectionKey.ToLowerInvariant();
            if (!Regex.IsMatch(path, "^/[a-z0-9][a-z0-9-]*$") || path is "/core" or "/api" || paths.ContainsValue(path))
                throw new InvalidOperationException($"{branch.ConnectionKey}_API_PATH must be a unique path such as /pt1, different from /core and /api.");
            paths.Add(branch.HospitalId!, path);
        }
        return paths;
    }

    public string Url(DatabaseEndpoint branch) => configuration[$"{branch.ConnectionKey}_API_URL"]
        ?? throw new InvalidOperationException($"Set {branch.ConnectionKey}_API_URL on the Core API.");
}
