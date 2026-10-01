using Ida.Application.Features.Auth;
using Ida.Infrastructure;
using Ida.Infrastructure.Databases;

namespace Ida.Api;

public static class ApiSetup
{
    public static void ConfigureIdaApi(this WebApplicationBuilder builder, string[] args, DatabaseRuntime runtime) =>
        builder.ConfigureCommonApi(args, "Api:Core");

    public static DatabaseRuntime ReadApiRuntime(IConfiguration configuration, DatabaseRuntime expected)
    {
        if (expected != DatabaseRuntime.Core) throw new ArgumentOutOfRangeException(nameof(expected));
        var mode = configuration["Api:Mode"];
        if (mode is null || string.Equals(mode, expected.ToString(), StringComparison.OrdinalIgnoreCase)) return expected;
        throw new InvalidOperationException($"Api:Mode must be '{expected}' for this API host.");
    }

    public static IServiceCollection AddIdaApi(this IServiceCollection services, IConfiguration configuration, DatabaseRuntime runtime)
    {
        services.AddCommonApi(configuration, typeof(SessionBuilder).Assembly, "iDA Core API", runtime != DatabaseRuntime.Management);
        services.AddScoped<SessionBuilder>();
        services.AddInfrastructure(configuration, runtime);
        return services;
    }

    public static void UseIdaApi(this WebApplication app, DatabaseRuntime runtime)
    {
        var pathBase = app.Configuration["Api:PathBase"] ?? app.Configuration["Api:Core:PathBase"] ?? app.Configuration["API_PATH_BASE"] ?? "/core";
        app.UseCommonApi(pathBase);
    }
}
