using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace Ida.Infrastructure.Configuration;

public static class AppSettings
{
    public static IConfigurationBuilder AddIdaSettings(this IConfigurationBuilder builder, string? environment = null, bool reloadOnChange = false, string? settingsDirectory = null)
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
        return builder.AddJsonFile(Path.Combine(settingsDirectory ?? SettingsDirectory(), fileName), optional: false, reloadOnChange: reloadOnChange).AddEnvironmentVariables();
    }

    private static string SettingsDirectory()
    {
        var settingsDirectory = Environment.GetEnvironmentVariable("IDA_SETTINGS_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(settingsDirectory)) return Path.GetFullPath(settingsDirectory);
        return AppContext.BaseDirectory;
    }
}
