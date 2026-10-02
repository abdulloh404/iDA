using Ida.Application.Features.Auth;
using Ida.Infrastructure;
using Ida.Infrastructure.Databases;
using Ida.Infrastructure.Services;

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
        if (runtime == DatabaseRuntime.Core)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal) { ServiceApiClient.ReadKey(configuration)! };
            foreach (var tenant in configuration.GetSection("Api:Tenants").GetChildren())
                if (!keys.Add(ServiceApiClient.ReadDestinationKey(configuration, $"{tenant.Path}:ServiceKey")))
                    throw new InvalidOperationException("Core and each Tenant API must have different service keys.");
        }
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
