using Ida.Api;
using Ida.Infrastructure.Configuration;
using Ida.Infrastructure.Databases;

var migrate = args.Contains("--migrate-databases", StringComparer.Ordinal);
var verifySecurity = args.Contains("--verify-security", StringComparer.Ordinal);
if (migrate || verifySecurity)
{
    var configuration = new ConfigurationBuilder().AddIdaSettings()
        .AddCommandLine(args.Where(argument => argument is not ("--migrate-databases" or "--verify-security")).ToArray()).Build();
    using var registry = new DatabaseRegistry(configuration);
    if (migrate) await new DatabaseProvisioner(registry).InitializeTenantAsync();
    if (verifySecurity) await new TenantSecurityVerifier(registry).VerifyAsync();
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureIdaApi(args, DatabaseRuntime.Tenant);
var runtime = ApiSetup.ReadApiRuntime(builder.Configuration, DatabaseRuntime.Tenant);
builder.Services.AddIdaApi(builder.Configuration, runtime);

var app = builder.Build();
app.UseIdaApi(runtime);
app.MapTenantEndpoints();
app.Run();
