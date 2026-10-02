using System.Net;
using System.Net.NetworkInformation;
using Ida.Start;
using Yarp.ReverseProxy.Configuration;

if (args.Any(argument => argument is "--help" or "-h"))
{
    Console.WriteLine("Usage: start-api [dev|serve|start] [--environment Local|Development|Production]");
    Console.WriteLine("Default: dev (dotnet watch), Local. Configuration comes from the workspace root .env.");
    return 0;
}

try
{
    var options = StartOptions.Parse(args);
    var apiDirectory = RootEnvironment.ApiDirectory();
    var envPath = Path.GetFullPath(Path.Combine(apiDirectory, "..", "..", ".env"));
    var values = RootEnvironment.Read(envPath);
    var services = ServiceSettings.Create(values);
    var proxyUrl = values["API_URL"].TrimEnd('/');
    var apis = services.Where(service => !service.IsWorker).ToArray();
    var occupied = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
    foreach (var url in apis.Select(service => service.ListenUrl!).Prepend(proxyUrl))
    {
        var port = new Uri(url).Port;
        if (occupied.Any(endpoint => endpoint.Port == port && (IPAddress.IsLoopback(endpoint.Address) || endpoint.Address.Equals(IPAddress.Any) || endpoint.Address.Equals(IPAddress.IPv6Any))))
            throw new InvalidOperationException($"Port {port} is already in use. No existing process will be stopped.");
    }

    await using var supervisor = new ServiceSupervisor(apiDirectory, options, services);
    supervisor.ValidateProjects();
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [], EnvironmentName = options.EnvironmentName, ContentRootPath = apiDirectory });
    builder.Configuration.Sources.Clear();
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Logging:LogLevel:Default"] = values["LOG_LEVEL_DEFAULT"],
        ["Logging:LogLevel:Microsoft.AspNetCore"] = values["LOG_LEVEL_MICROSOFT_ASPNETCORE"],
        ["AllowedHosts"] = values["ALLOWED_HOSTS"]
    });
    builder.WebHost.UseUrls(proxyUrl);
    var routes = apis.Select(service => new RouteConfig
    {
        RouteId = service.Name,
        ClusterId = service.Name,
        Match = new RouteMatch { Path = service.PathBase + "/{**catchAll}" },
        Transforms = [new Dictionary<string, string> { ["RequestHeaderOriginalHost"] = "true" }]
    }).ToArray();
    var clusters = apis.Select(service => new ClusterConfig
    {
        ClusterId = service.Name,
        Destinations = new Dictionary<string, DestinationConfig> { [service.Name] = new() { Address = service.ListenUrl!.TrimEnd('/') + "/" } }
    }).ToArray();
    builder.Services.AddReverseProxy().LoadFromMemory(routes, clusters);
    await using var app = builder.Build();
    app.MapReverseProxy();
    try
    {
        await app.StartAsync();
        Console.WriteLine($"API proxy ({options.EnvironmentName}): {proxyUrl}");
        foreach (var service in apis) Console.WriteLine($"  {proxyUrl}{service.PathBase} -> {service.ListenUrl!.TrimEnd('/')}{service.PathBase}");
        supervisor.Start(app.Lifetime.ApplicationStopping);
        var shutdown = app.WaitForShutdownAsync();
        var finished = await Task.WhenAny(shutdown, supervisor.Completion);
        return finished == supervisor.Completion ? await supervisor.Completion : 0;
    }
    catch (OperationCanceledException) when (app.Lifetime.ApplicationStopping.IsCancellationRequested)
    {
        return 0;
    }
    finally
    {
        await app.StopAsync();
    }
}
catch (Exception exception) when (exception is InvalidOperationException or IOException or System.ComponentModel.Win32Exception or ArgumentException)
{
    Console.Error.WriteLine($"Start failed: {exception.Message}");
    return 1;
}
