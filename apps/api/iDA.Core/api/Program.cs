using Ida.Api;
using Ida.Infrastructure.Databases;
using Ida.Infrastructure.Persistence;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureIdaApi(args, DatabaseRuntime.Core);

if (args.Contains("--list-tenant-databases"))
{
    using var registry = new DatabaseRegistry(builder.Configuration, DatabaseRuntime.Management);
    Console.WriteLine(JsonSerializer.Serialize(await registry.ListBranchesAsync(administrator: true), new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    return;
}

if (args.Contains("--migrate-databases"))
{
    using var registry = new DatabaseRegistry(builder.Configuration, DatabaseRuntime.Management);
    await new DatabaseProvisioner(registry).InitializeAsync();
    Console.WriteLine("Core schema and database registry are ready.");
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
