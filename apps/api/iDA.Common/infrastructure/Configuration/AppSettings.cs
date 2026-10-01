using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace Ida.Infrastructure.Configuration;

public static class AppSettings
{
    public static IConfigurationBuilder AddIdaSettings(this IConfigurationBuilder builder, string? environment = null, bool reloadOnChange = false)
    {
        environment ??= Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var fileName = environment.ToLowerInvariant() switch
        {
            "production" => "appsettings.json",
            "development" => "appsettings.Development.json",
            "local" => "appsettings.local.json",
            _ => throw new InvalidOperationException($"Unsupported API environment '{environment}'. Use Production, Development, or Local.")
        };
        foreach (var source in builder.Sources.OfType<JsonConfigurationSource>().Where(source => Path.GetFileName(source.Path)?.StartsWith("appsettings", StringComparison.OrdinalIgnoreCase) == true).ToArray())
            builder.Sources.Remove(source);
        return builder.AddJsonFile(Path.Combine(SettingsDirectory(), fileName), optional: false, reloadOnChange: reloadOnChange).AddEnvironmentVariables();
    }

    private static string SettingsDirectory()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var workspaceApi = Path.Combine(currentDirectory, "apps", "api");
        if (File.Exists(Path.Combine(workspaceApi, "iDA.sln"))) return workspaceApi;
        for (var directory = new DirectoryInfo(currentDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "iDA.sln"))) return directory.FullName;
        }
        return AppContext.BaseDirectory;
    }
}
