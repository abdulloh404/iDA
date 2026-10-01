using System.Globalization;
using System.Text.RegularExpressions;

namespace Ida.Start;

internal sealed record ApiEndpoint(string Key, string Prefix, Uri Address);

internal sealed record ApiRuntime(int GatewayPort, ApiEndpoint Core, IReadOnlyList<ApiEndpoint> Tenants)
{
    public IEnumerable<ApiEndpoint> Endpoints => new[] { Core }.Concat(Tenants);

    public static ApiRuntime Read(IConfiguration configuration)
    {
        var gatewayPort = ReadPort(configuration["API_PORT"] ?? configuration["Api:GatewayPort"], 3100, "Api:GatewayPort");
        var corePrefix = configuration["API_PATH_BASE"] ?? configuration["Api:Core:PathBase"] ?? "/core";
        if (corePrefix != "/core") throw new InvalidOperationException("Api:Core:PathBase must be /core.");
        var configuredKeys = configuration.GetSection("Api:Tenants").GetChildren().Select(section => section.Key.ToUpperInvariant()).Where(IsBu).ToArray();
        var connectionKeys = configuration.GetSection("ConnectionStrings").GetChildren().Select(section => section.Key.ToUpperInvariant()).Where(IsBu)
            .Concat(configuration.GetChildren().Where(section => Regex.IsMatch(section.Key, "^BU[0-9]+_DB_CONNECTION$", RegexOptions.IgnoreCase) && !string.IsNullOrWhiteSpace(section.Value)).Select(section => section.Key[..^14].ToUpperInvariant()));
        var keys = configuration["BU_IDS"] is { Length: > 0 } selected
            ? selected.Split(',').Select(key => key.Trim().ToUpperInvariant()).ToArray()
            : (configuredKeys.Length > 0 ? configuredKeys : connectionKeys).Distinct().Order(StringComparer.Ordinal).ToArray();
        if (keys.Length == 0 || keys.Any(key => !IsBu(key)) || keys.Distinct().Count() != keys.Length)
            throw new InvalidOperationException("Configure Api:Tenants or a unique comma-separated BU_IDS list.");

        var core = new ApiEndpoint("CORE", corePrefix, ReadAddress(configuration["CORE_API_URL"] ?? configuration["Api:Core:Url"], configuration["CORE_API_PORT"], gatewayPort + 1, gatewayPort, "Api:Core:Url"));
        var tenants = keys.Select((key, index) =>
        {
            var prefix = configuration[$"{key}_API_PATH"] ?? configuration[$"Api:Tenants:{key}:PathBase"] ?? "/" + key.ToLowerInvariant();
            if (!Regex.IsMatch(prefix, "^/[a-z0-9][a-z0-9-]*$") || prefix is "/core" or "/api")
                throw new InvalidOperationException($"Api:Tenants:{key}:PathBase must be a tenant path such as /pt1.");
            return new ApiEndpoint(key, prefix, ReadAddress(configuration[$"{key}_API_URL"] ?? configuration[$"Api:Tenants:{key}:Url"], configuration[$"{key}_API_PORT"], gatewayPort + index + 2, gatewayPort, $"Api:Tenants:{key}:Url"));
        }).ToArray();

        if (tenants.Select(tenant => tenant.Prefix).Distinct().Count() != tenants.Length)
            throw new InvalidOperationException("Tenant API paths must be unique.");
        var ports = new[] { gatewayPort, core.Address.Port }.Concat(tenants.Select(tenant => tenant.Address.Port)).ToArray();
        if (ports.Distinct().Count() != ports.Length)
            throw new InvalidOperationException("Services started together must have distinct internal ports.");
        return new ApiRuntime(gatewayPort, core, tenants);
    }

    public IReadOnlyList<ApiService> CreateServices(string environment, string apiRoot)
    {
        var shared = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["DOTNET_ENVIRONMENT"] = environment,
            ["ASPNETCORE_ENVIRONMENT"] = environment,
            ["IDA_SETTINGS_DIRECTORY"] = apiRoot,
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

    private static int ReadPort(string? value, int fallback, string name)
    {
        if (!int.TryParse(value ?? fallback.ToString(CultureInfo.InvariantCulture), out var port) || port is < 1 or > 65535)
            throw new InvalidOperationException($"{name} must be an integer between 1 and 65535.");
        return port;
    }

    private static Uri ReadAddress(string? value, string? overridePort, int fallbackPort, int gatewayPort, string name)
    {
        var servicePort = ReadPort(overridePort, fallbackPort, name);
        if (!Uri.TryCreate(value ?? $"http://localhost:{servicePort}", UriKind.Absolute, out var address) || address.Scheme != "http" || address.UserInfo.Length > 0 || address.AbsolutePath != "/" || address.Query.Length > 0 || address.Fragment.Length > 0)
            throw new InvalidOperationException($"{name} must be an absolute HTTP origin without credentials, a path, query or fragment.");
        if (address.IsLoopback && address.Port == gatewayPort) address = new UriBuilder(address) { Port = servicePort }.Uri;
        return address;
    }
}
