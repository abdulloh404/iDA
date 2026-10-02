using System.Text.RegularExpressions;
using Ida.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;

namespace Ida.Start;

internal sealed record ApiEndpoint(string Key, string Prefix, Uri Address);

internal sealed record ApiRuntime(ApiEndpoint Core, IReadOnlyList<ApiEndpoint> Tenants)
{
    public IEnumerable<ApiEndpoint> Endpoints => new[] { Core }.Concat(Tenants);

    public static ApiRuntime Read(string environment, string apiRoot, string[] args)
    {
        var coreConfiguration = new ConfigurationBuilder().AddIdaSettings(environment, settingsDirectory: Path.Combine(apiRoot, "iDA.Core/api")).AddCommandLine(args).Build();
        var tenantConfiguration = new ConfigurationBuilder().AddIdaSettings(environment, settingsDirectory: Path.Combine(apiRoot, "iDA.Tanent/api")).AddCommandLine(args).Build();
        var corePrefix = coreConfiguration["API_PATH_BASE"] ?? coreConfiguration["Api:PathBase"] ?? coreConfiguration["Api:Core:PathBase"] ?? "/core";
        if (corePrefix != "/core") throw new InvalidOperationException("Api:Core:PathBase must be /core.");
        var configuredKeys = tenantConfiguration.GetSection("Api:Tenants").GetChildren().Select(section => section.Key.ToUpperInvariant()).Where(IsBu).ToArray();
        var connectionKeys = tenantConfiguration.GetSection("ConnectionStrings").GetChildren().Select(section => section.Key.ToUpperInvariant()).Where(IsBu)
            .Concat(tenantConfiguration.GetChildren().Where(section => Regex.IsMatch(section.Key, "^BU[0-9]+_DB_CONNECTION$", RegexOptions.IgnoreCase) && !string.IsNullOrWhiteSpace(section.Value)).Select(section => section.Key[..^14].ToUpperInvariant()));
        var keys = tenantConfiguration["BU_IDS"] is { Length: > 0 } selected
            ? selected.Split(',').Select(key => key.Trim().ToUpperInvariant()).ToArray()
            : (configuredKeys.Length > 0 ? configuredKeys : connectionKeys).Distinct().Order(StringComparer.Ordinal).ToArray();
        if (keys.Length == 0 || keys.Any(key => !IsBu(key)) || keys.Distinct().Count() != keys.Length)
            throw new InvalidOperationException("Configure Api:Tenants or a unique comma-separated BU_IDS list.");

        var core = new ApiEndpoint("CORE", corePrefix, ReadAddress(coreConfiguration["CORE_API_URL"] ?? coreConfiguration["Api:Urls"] ?? coreConfiguration["Api:Core:Url"], coreConfiguration["CORE_API_PORT"], "Api:Core:Url"));
        var tenants = keys.Select(key =>
        {
            var prefix = tenantConfiguration[$"{key}_API_PATH"] ?? tenantConfiguration[$"Api:Tenants:{key}:PathBase"] ?? "/" + key.ToLowerInvariant();
            if (!Regex.IsMatch(prefix, "^/[a-z0-9][a-z0-9-]*$") || prefix is "/core" or "/api")
                throw new InvalidOperationException($"Api:Tenants:{key}:PathBase must be a tenant path such as /pt1.");
            return new ApiEndpoint(key, prefix, ReadAddress(tenantConfiguration[$"{key}_API_URL"] ?? tenantConfiguration[$"Api:Tenants:{key}:Url"], tenantConfiguration[$"{key}_API_PORT"], $"Api:Tenants:{key}:Url"));
        }).ToArray();

        if (tenants.Select(tenant => tenant.Prefix).Distinct().Count() != tenants.Length)
            throw new InvalidOperationException("Tenant API paths must be unique.");
        var ports = new[] { core.Address.Port }.Concat(tenants.Select(tenant => tenant.Address.Port)).ToArray();
        if (ports.Distinct().Count() != ports.Length)
            throw new InvalidOperationException("Services started together must have distinct internal ports.");
        return new ApiRuntime(core, tenants);
    }

    public IReadOnlyList<ApiService> CreateServices(string environment, string apiRoot)
    {
        var shared = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["DOTNET_ENVIRONMENT"] = environment,
            ["ASPNETCORE_ENVIRONMENT"] = environment,
            ["Api__Core__Url"] = Core.Address.GetLeftPart(UriPartial.Authority),
            ["Api__Core__PathBase"] = Core.Prefix,
        };
        foreach (var tenant in Tenants)
        {
            shared[$"Api__Tenants__{tenant.Key}__Url"] = tenant.Address.GetLeftPart(UriPartial.Authority);
            shared[$"Api__Tenants__{tenant.Key}__PathBase"] = tenant.Prefix;
        }

        Dictionary<string, string> EnvironmentFor(ApiEndpoint endpoint, bool ingest = false)
        {
            var env = new Dictionary<string, string>(shared, StringComparer.OrdinalIgnoreCase)
            {
                ["IDA_SETTINGS_DIRECTORY"] = Path.Combine(apiRoot, ingest ? "IDA.Ingest-worker" : endpoint.Key == "CORE" ? "iDA.Core/api" : "iDA.Tanent/api"),
                ["Api__Mode"] = endpoint.Key == "CORE" ? "Core" : "Tenant",
                ["Api__PathBase"] = endpoint.Prefix,
            };
            if (endpoint.Key != "CORE")
            {
                env["Api__BuId"] = endpoint.Key;
                env["BU_ID"] = endpoint.Key;
            }
            if (!ingest) env["ASPNETCORE_URLS"] = endpoint.Address.GetLeftPart(UriPartial.Authority);
            return env;
        }

        var services = new List<ApiService>
        {
            new("core", "Core API", "iDA.Core/api", "Ida.Api.csproj", "iDA.Core.Api.dll", ["--urls", Core.Address.GetLeftPart(UriPartial.Authority)], EnvironmentFor(Core)),
        };
        foreach (var tenant in Tenants)
        {
            services.Add(new ApiService($"tenant-{tenant.Key}", $"Tenant API {tenant.Key}", "iDA.Tanent/api", "Ida.Tenant.Api.csproj", "iDA.Tenant.Api.dll", ["--urls", tenant.Address.GetLeftPart(UriPartial.Authority)], EnvironmentFor(tenant)));
            services.Add(new ApiService($"ingest-{tenant.Key}", $"Ingest {tenant.Key}", "IDA.Ingest-worker", "Ida.Worker.Ingest.csproj", "Ida.Worker.Ingest.dll", ["serve"], EnvironmentFor(tenant, true)));
        }
        return services;
    }

    public static string FindApiRoot()
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "Ida.Start.csproj"))) return directory.FullName;
        return AppContext.BaseDirectory;
    }

    private static bool IsBu(string value) => Regex.IsMatch(value, "^BU[0-9]+$");

    private static int ReadPort(string value, string name)
    {
        if (!int.TryParse(value, out var port) || port is < 1 or > 65535)
            throw new InvalidOperationException($"{name} must be an integer between 1 and 65535.");
        return port;
    }

    private static Uri ReadAddress(string? value, string? overridePort, string name)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var address) || address.Scheme != "http" || address.UserInfo.Length > 0 || address.AbsolutePath != "/" || address.Query.Length > 0 || address.Fragment.Length > 0)
            throw new InvalidOperationException($"{name} must be an absolute HTTP origin without credentials, a path, query or fragment.");
        if (overridePort is not null) address = new UriBuilder(address) { Port = ReadPort(overridePort, name) }.Uri;
        return address;
    }
}
