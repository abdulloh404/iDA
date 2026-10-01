using Ida.Infrastructure.Configuration;
using Ida.Start;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

var hostSettings = new ConfigurationBuilder().AddEnvironmentVariables("ASPNETCORE_").AddEnvironmentVariables("DOTNET_").AddCommandLine(args).Build();
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, EnvironmentName = hostSettings[HostDefaults.EnvironmentKey] ?? "Local" });
builder.Configuration.AddIdaSettings(builder.Environment.EnvironmentName).AddCommandLine(args);
var apiRoot = ApiRuntime.FindApiRoot();
var mode = builder.Configuration["mode"] ?? (File.Exists(Path.Combine(apiRoot, "Ida.Start.csproj")) ? "serve" : "start");
if (mode is not ("dev" or "serve" or "start")) throw new InvalidOperationException("Use --mode dev, serve or start.");
var runtime = ApiRuntime.Read(builder.Configuration);
if (string.IsNullOrWhiteSpace(builder.Configuration["urls"]) && string.IsNullOrWhiteSpace(builder.Configuration["http_ports"]) && string.IsNullOrWhiteSpace(builder.Configuration["https_ports"]))
    builder.WebHost.UseUrls($"http://localhost:{runtime.GatewayPort}");

var routes = runtime.Endpoints.Select(endpoint => new RouteConfig
{
    RouteId = endpoint.Key,
    ClusterId = endpoint.Key,
    Match = new RouteMatch { Path = endpoint.Prefix + "/{**path}" },
    Transforms = [new Dictionary<string, string> { ["RequestHeaderRemove"] = "X-Ida-Service-Key" }],
}).ToArray();
var clusters = runtime.Endpoints.Select(endpoint => new ClusterConfig
{
    ClusterId = endpoint.Key,
    Destinations = new Dictionary<string, DestinationConfig> { [endpoint.Key] = new() { Address = endpoint.Address.AbsoluteUri } },
    HttpRequest = new ForwarderRequestConfig { ActivityTimeout = TimeSpan.FromSeconds(120) },
}).ToArray();
builder.Services.AddReverseProxy().LoadFromMemory(routes, clusters);
builder.Services.AddHostedService(provider => new ServiceSupervisor(apiRoot, mode, runtime.CreateServices(builder.Environment.EnvironmentName, apiRoot), provider.GetRequiredService<IHostApplicationLifetime>(), provider.GetRequiredService<ILogger<ServiceSupervisor>>()));

var app = builder.Build();
app.Use(async (context, next) =>
{
    if (runtime.Endpoints.Any(endpoint => context.Request.Path.StartsWithSegments(endpoint.Prefix + "/api/internal", StringComparison.OrdinalIgnoreCase)))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsJsonAsync(new { error = new { code = "not_found", message = "API route not found." } }, context.RequestAborted);
        return;
    }
    await next(context);
    if (!context.Response.HasStarted && !context.RequestAborted.IsCancellationRequested && context.Features.Get<IForwarderErrorFeature>() is not null)
    {
        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status502BadGateway;
        await context.Response.WriteAsJsonAsync(new { error = new { code = "api_unavailable", message = "API service unavailable." } }, context.RequestAborted);
    }
});
app.MapReverseProxy();
app.MapFallback("/{**path}", async context =>
{
    context.Response.StatusCode = StatusCodes.Status404NotFound;
    await context.Response.WriteAsJsonAsync(new { error = new { code = "not_found", message = "API route not found." } }, context.RequestAborted);
});
app.Lifetime.ApplicationStarted.Register(() =>
{
    foreach (var address in app.Urls)
    {
        Console.WriteLine($"API gateway ({app.Environment.EnvironmentName}): {address}");
        foreach (var endpoint in runtime.Endpoints) Console.WriteLine($"  {endpoint.Prefix}: {address.TrimEnd('/')}{endpoint.Prefix}");
    }
});
await app.RunAsync();
