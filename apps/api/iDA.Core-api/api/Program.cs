using Ida.Api;
using Ida.Infrastructure.Databases;
using Ida.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureIdaApi(args, DatabaseRuntime.Core);

if (args.Contains("--migrate-databases"))
{
    using var registry = new DatabaseRegistry(builder.Configuration, DatabaseRuntime.Management);
    await new DatabaseProvisioner(registry).InitializeAsync();
    Console.WriteLine("Core and registered BU schemas are ready.");
    return;
}

var runtime = args.Contains("--seed") ? DatabaseRuntime.Management : ApiSetup.ReadApiRuntime(builder.Configuration, DatabaseRuntime.Core);
builder.Services.AddIdaApi(builder.Configuration, runtime);

var app = builder.Build();

if (args.Contains("--seed"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
    return;
}

app.UseIdaApi(runtime);
app.MapCoreEndpoints();
app.Run();
