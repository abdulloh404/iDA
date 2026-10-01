using Ida.Api;
using Ida.Infrastructure.Databases;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureIdaApi(args);
builder.Services.AddIdaApi(builder.Configuration, DatabaseRuntime.Tenant);

var app = builder.Build();
app.UseIdaApi(DatabaseRuntime.Tenant);
app.MapTenantEndpoints();
app.Run();
