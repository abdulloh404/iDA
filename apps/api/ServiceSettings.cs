using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace Ida.Start;

internal static class ServiceSettings
{
    public static IReadOnlyList<ServiceDefinition> Create(IReadOnlyDictionary<string, string> values)
    {
        var buIds = values.Keys.Select(key => Regex.Match(key, "^(BU[0-9]+)_", RegexOptions.CultureInvariant))
            .Where(match => match.Success).Select(match => match.Groups[1].Value).Distinct(StringComparer.Ordinal)
            .OrderBy(buId => buId.Length).ThenBy(buId => buId, StringComparer.Ordinal).ToArray();
        string[] required = ["API_URL", "API_KNOWN_PROXIES", "CORS_ORIGINS", "ALLOWED_HOSTS", "CORE_API_URL", "CORE_API_PATH_BASE", "CORE_LISTEN_URL", "API_SERVICE_KEY", "CORE_CONNECTION", "CORE_MIGRATION_CONNECTION", "JWT_KEY", "JWT_ISSUER", "JWT_AUDIENCE", "JWT_LIFETIME_HOURS", "SECURITY_DATA_PROTECTION_KEY", "SECURITY_HASH_SALT", "LOG_LEVEL_DEFAULT", "LOG_LEVEL_MICROSOFT_ASPNETCORE"];
        string[] buSuffixes = ["API_URL", "API_PATH_BASE", "LISTEN_URL", "SERVICE_KEY", "CONNECTION", "MIGRATION_CONNECTION", "INGEST_SERVICE_KEY"];
        var missing = required.Concat(buIds.SelectMany(buId => buSuffixes.Select(suffix => $"{buId}_{suffix}")))
            .Where(key => !values.TryGetValue(key, out var value) || (key is not ("API_KNOWN_PROXIES" or "CORS_ORIGINS") && string.IsNullOrWhiteSpace(value))).ToArray();
        if (missing.Length > 0) throw new InvalidOperationException($"Missing required configuration keys: {string.Join(", ", missing)}.");

        var serviceKeys = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var key in new[] { "API_SERVICE_KEY" }.Concat(buIds.SelectMany(buId => new[] { $"{buId}_SERVICE_KEY", $"{buId}_INGEST_SERVICE_KEY" })))
        {
            if (values[key].Length < 32) throw new InvalidOperationException($"{key} must contain at least 32 characters.");
            if (serviceKeys.TryGetValue(values[key], out var previousKey)) throw new InvalidOperationException($"{previousKey} and {key} must be distinct.");
            serviceKeys.Add(values[key], key);
        }
        if (values["JWT_KEY"].Length < 32) throw new InvalidOperationException("JWT_KEY must contain at least 32 characters.");
        if (!int.TryParse(values["JWT_LIFETIME_HOURS"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var lifetimeHours) || lifetimeHours <= 0)
            throw new InvalidOperationException("JWT_LIFETIME_HOURS must contain a positive integer.");
        if (!Convert.TryFromBase64String(values["SECURITY_DATA_PROTECTION_KEY"], stackalloc byte[32], out var keyBytes) || keyBytes != 32)
            throw new InvalidOperationException("SECURITY_DATA_PROTECTION_KEY must contain a base64 key of exactly 32 bytes.");

        var knownProxies = Split(values["API_KNOWN_PROXIES"]);
        if (knownProxies.Any(value => !IPAddress.TryParse(value, out _))) throw new InvalidOperationException("API_KNOWN_PROXIES must contain IP addresses.");
        var listenEndpoints = new Dictionary<(string Host, int Port), string>();
        RegisterListenUrl(values, "API_URL", listenEndpoints);
        RegisterListenUrl(values, "CORE_LISTEN_URL", listenEndpoints, loopbackOnly: true);
        ValidatePublicEndpoint(values, "CORE_API_URL", "CORE_API_PATH_BASE");
        foreach (var buId in buIds)
        {
            RegisterListenUrl(values, $"{buId}_LISTEN_URL", listenEndpoints, loopbackOnly: true);
            ValidatePublicEndpoint(values, $"{buId}_API_URL", $"{buId}_API_PATH_BASE");
        }
        var routeKeys = new[] { "CORE_API_PATH_BASE" }.Concat(buIds.Select(buId => $"{buId}_API_PATH_BASE")).ToArray();
        for (var index = 0; index < routeKeys.Length; index++)
            foreach (var other in routeKeys.Skip(index + 1))
                if (string.Equals(values[routeKeys[index]], values[other], StringComparison.OrdinalIgnoreCase)
                    || values[routeKeys[index]].StartsWith(values[other] + "/", StringComparison.OrdinalIgnoreCase)
                    || values[other].StartsWith(values[routeKeys[index]] + "/", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"{routeKeys[index]} and {other} must use distinct, non-overlapping path prefixes.");

        var tenants = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var buId in buIds)
            tenants[buId] = Endpoint(values[$"{buId}_API_URL"], values[$"{buId}_API_PATH_BASE"], values[$"{buId}_SERVICE_KEY"]);

        var coreSettings = CommonSettings(values, lifetimeHours);
        coreSettings["Api"] = new Dictionary<string, object?>
        {
            ["Mode"] = "Core",
            ["Urls"] = values["CORE_LISTEN_URL"],
            ["PathBase"] = values["CORE_API_PATH_BASE"],
            ["Core"] = Endpoint(values["CORE_API_URL"], values["CORE_API_PATH_BASE"]),
            ["Tenants"] = tenants,
            ["ServiceKey"] = values["API_SERVICE_KEY"],
            ["KnownProxies"] = knownProxies,
        };
        coreSettings["ConnectionStrings"] = new Dictionary<string, object?>
        {
            ["Core"] = values["CORE_CONNECTION"],
            ["CoreMigration"] = values["CORE_MIGRATION_CONNECTION"],
        };
        var services = new List<ServiceDefinition>
        {
            new("Core", null, "iDA.Core/api/Ida.Api.csproj", "iDA.Core.Api", coreSettings, false, values["CORE_LISTEN_URL"], values["CORE_API_PATH_BASE"]),
        };
        foreach (var buId in buIds)
        {
            var tenantSettings = CommonSettings(values, lifetimeHours);
            tenantSettings["Api"] = new Dictionary<string, object?>
            {
                ["Mode"] = "Tenant",
                ["BuId"] = buId,
                ["Urls"] = values[$"{buId}_LISTEN_URL"],
                ["PathBase"] = values[$"{buId}_API_PATH_BASE"],
                ["Core"] = Endpoint(values["CORE_API_URL"], values["CORE_API_PATH_BASE"], values["API_SERVICE_KEY"]),
                ["ServiceKey"] = values[$"{buId}_SERVICE_KEY"],
                ["KnownProxies"] = knownProxies.ToArray(),
            };
            tenantSettings["ConnectionStrings"] = TenantConnections(values, buId);
            services.Add(new($"Tenant-{buId}", buId, "iDA.Tanent/api/Ida.Tenant.Api.csproj", "iDA.Tenant.Api", tenantSettings, false, values[$"{buId}_LISTEN_URL"], values[$"{buId}_API_PATH_BASE"]));

            var workerSettings = new Dictionary<string, object?>
            {
                ["BU_ID"] = buId,
                ["Api"] = new Dictionary<string, object?>
                {
                    ["Core"] = Endpoint(values["CORE_API_URL"], values["CORE_API_PATH_BASE"], values["API_SERVICE_KEY"]),
                    ["ServiceKey"] = values[$"{buId}_INGEST_SERVICE_KEY"],
                },
                ["ConnectionStrings"] = TenantConnections(values, buId),
                ["Logging"] = Logging(values),
            };
            services.Add(new($"Ingest-{buId}", buId, "IDA.Ingest-worker/Ida.Worker.Ingest.csproj", "Ida.Worker.Ingest", workerSettings, true, null, null));
        }
        return services;
    }

    private static Dictionary<string, object?> CommonSettings(IReadOnlyDictionary<string, string> values, int lifetimeHours) => new()
    {
        ["Jwt"] = new Dictionary<string, object?>
        {
            ["Key"] = values["JWT_KEY"],
            ["Issuer"] = values["JWT_ISSUER"],
            ["Audience"] = values["JWT_AUDIENCE"],
            ["LifetimeHours"] = lifetimeHours,
        },
        ["Security"] = new Dictionary<string, object?>
        {
            ["DataProtectionKey"] = values["SECURITY_DATA_PROTECTION_KEY"],
            ["HashSalt"] = values["SECURITY_HASH_SALT"],
        },
        ["Cors"] = new Dictionary<string, object?> { ["Origins"] = Split(values["CORS_ORIGINS"]) },
        ["Logging"] = Logging(values),
        ["AllowedHosts"] = values["ALLOWED_HOSTS"],
    };

    private static Dictionary<string, object?> Logging(IReadOnlyDictionary<string, string> values) => new()
    {
        ["LogLevel"] = new Dictionary<string, object?>
        {
            ["Default"] = values["LOG_LEVEL_DEFAULT"],
            ["Microsoft.AspNetCore"] = values["LOG_LEVEL_MICROSOFT_ASPNETCORE"],
        },
    };

    private static Dictionary<string, object?> TenantConnections(IReadOnlyDictionary<string, string> values, string buId) => new()
    {
        ["Tenant"] = values[$"{buId}_CONNECTION"],
        ["TenantMigration"] = values[$"{buId}_MIGRATION_CONNECTION"],
    };

    private static Dictionary<string, object?> Endpoint(string url, string pathBase, string? serviceKey = null)
    {
        var endpoint = new Dictionary<string, object?> { ["Url"] = url, ["PathBase"] = pathBase };
        if (serviceKey is not null) endpoint["ServiceKey"] = serviceKey;
        return endpoint;
    }

    private static string[] Split(string value) => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static Uri ReadUrl(IReadOnlyDictionary<string, string> values, string key, bool authorityOnly)
    {
        var value = values[key];
        if (value != value.Trim() || value.Contains('\\') || !Uri.TryCreate(value, UriKind.Absolute, out var address)
            || address.Scheme is not ("http" or "https") || address.Host.Length == 0 || address.Port <= 0
            || address.UserInfo.Length > 0 || address.Query.Length > 0 || address.Fragment.Length > 0
            || (authorityOnly && !Regex.IsMatch(value, "^https?://[^/?#\\\\]+/?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)))
            throw new InvalidOperationException($"{key} must contain an absolute HTTP(S) URL without credentials, query or fragment{(authorityOnly ? " or path" : "")}.");
        return address;
    }

    private static void RegisterListenUrl(IReadOnlyDictionary<string, string> values, string key, Dictionary<(string Host, int Port), string> endpoints, bool loopbackOnly = false)
    {
        var address = ReadUrl(values, key, authorityOnly: true);
        if (loopbackOnly && !address.IsLoopback) throw new InvalidOperationException($"{key} must contain a loopback listening address for Local.");
        var host = address.IsLoopback || address.Host is "0.0.0.0" or "[::]" ? "loopback" : address.IdnHost.ToLowerInvariant();
        var endpoint = (host, address.Port);
        if (endpoints.TryGetValue(endpoint, out var previousKey)) throw new InvalidOperationException($"{previousKey} and {key} must use distinct listening endpoints.");
        endpoints.Add(endpoint, key);
    }

    private static void ValidatePublicEndpoint(IReadOnlyDictionary<string, string> values, string urlKey, string pathKey)
    {
        var pathBase = values[pathKey];
        if (!Regex.IsMatch(pathBase, "^/[A-Za-z0-9_-]+(?:/[A-Za-z0-9_-]+)*$", RegexOptions.CultureInvariant))
            throw new InvalidOperationException($"{pathKey} must contain a path prefix such as /core or /pt1, without a trailing slash or route patterns.");
        var address = ReadUrl(values, urlKey, authorityOnly: false);
        if (Uri.Compare(address, new Uri(values["API_URL"]), UriComponents.SchemeAndServer, UriFormat.UriEscaped, StringComparison.OrdinalIgnoreCase) != 0)
            throw new InvalidOperationException($"{urlKey} must use the same public origin as API_URL for this proxy.");
        var existingPath = address.AbsolutePath.TrimEnd('/');
        if (existingPath.Length > 0 && !string.Equals(existingPath, pathBase.TrimEnd('/'), StringComparison.Ordinal))
            throw new InvalidOperationException($"{urlKey} must use the same path as {pathKey} when a path is included.");
    }
}

internal sealed record ServiceDefinition(string Name, string? BuId, string ProjectPath, string AssemblyName, Dictionary<string, object?> Settings, bool IsWorker, string? ListenUrl, string? PathBase);
