using Ida.Start;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var hostSettings = new ConfigurationBuilder().AddEnvironmentVariables("ASPNETCORE_").AddEnvironmentVariables("DOTNET_").AddCommandLine(args).Build();
var apiRoot = ApiRuntime.FindApiRoot();
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { Args = args, EnvironmentName = hostSettings[HostDefaults.EnvironmentKey] ?? "Local", ContentRootPath = apiRoot, DisableDefaults = true });
builder.Configuration.AddEnvironmentVariables().AddCommandLine(args);
builder.Logging.AddConsole();
var mode = builder.Configuration["mode"] ?? (File.Exists(Path.Combine(apiRoot, "Ida.Start.csproj")) ? "serve" : "start");
if (mode is not ("dev" or "serve" or "start")) throw new InvalidOperationException("Use --mode dev, serve or start.");
var runtime = ApiRuntime.Read(builder.Environment.EnvironmentName, apiRoot, args);
builder.Services.AddHostedService(provider => new ServiceSupervisor(apiRoot, mode, runtime.CreateServices(builder.Environment.EnvironmentName, apiRoot), provider.GetRequiredService<IHostApplicationLifetime>(), provider.GetRequiredService<ILogger<ServiceSupervisor>>()));

using var app = builder.Build();
Console.WriteLine($"API services ({builder.Environment.EnvironmentName}):");
foreach (var endpoint in runtime.Endpoints) Console.WriteLine($"  {endpoint.Prefix}: {endpoint.Address.GetLeftPart(UriPartial.Authority)}{endpoint.Prefix}");
await app.RunAsync();
