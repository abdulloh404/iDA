using Ida.Api;
using Ida.Infrastructure.Databases;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureIdaApi(args, DatabaseRuntime.Tenant);
var runtime = ApiSetup.ReadApiRuntime(builder.Configuration, DatabaseRuntime.Tenant);
builder.Services.AddIdaApi(builder.Configuration, runtime);

var app = builder.Build();
app.UseIdaApi(runtime);
app.MapTenantEndpoints();
app.Run();
