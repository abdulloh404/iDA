using System.Text;
using Ida.Api;
using Ida.Api.Auth;
using Ida.Api.Endpoints;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Auth;
using Ida.Application.Features.Hello.Queries;
using Ida.Infrastructure;
using Ida.Infrastructure.Databases;
using Ida.Infrastructure.Persistence;
using Ida.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

System.Globalization.CultureInfo.DefaultThreadCurrentCulture =
    System.Globalization.CultureInfo.InvariantCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture =
    System.Globalization.CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

if (args.Contains("--migrate-databases"))
{
    using var registry = new DatabaseRegistry(builder.Configuration);
    await new DatabaseProvisioner(builder.Configuration, registry).InitializeAsync();
    Console.WriteLine("Core and registered BU schemas are ready.");
    return;
}

builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.ConfigureIdaJson());

builder.Services.AddMediatR(c =>
{
    c.RegisterServicesFromAssemblyContaining<GetHelloQuery>();
    c.AddOpenBehavior(typeof(CqrsLoggingBehavior<,>));
});

builder.Services.AddCrudResources(typeof(GetHelloQuery).Assembly);
builder.Services.AddScoped<SessionBuilder>();

builder.Services.AddInfrastructure(builder.Configuration);

var jwt = JwtTokenService.Read(builder.Configuration);
builder.Services
    .AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {

        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),

            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
              ?? ["http://localhost:3000"];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(origins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "iDA API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "วาง token ที่ได้จาก /api/auth/login",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer",
            },
        }] = [],
    });
});

var app = builder.Build();

if (args.Contains("--seed"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
    return;
}

app.UseCors();
app.UseMiddleware<ErrorEnvelopeMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
        await context.RequestServices.GetRequiredService<DatabaseContexts>().InitializeAsync(context.RequestAborted);
    await next(context);
});
app.UseSwagger();
app.UseSwaggerUI();

app.MapIdaEndpoints();
app.MapAuthEndpoints();
app.MapMasterDataEndpoints();
app.MapDoctorEndpoints();
app.MapShareRateEndpoints();
app.MapDutyRateEndpoints();
app.MapDutyScheduleEndpoints();
app.MapDoctorFee402Endpoints();
app.MapSystemSettingsEndpoints();
app.MapApprovalEndpoints();
app.MapIngestConfigurationEndpoints();
app.MapIngestMonitoringEndpoints();
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", checkedAt = DateTimeOffset.UtcNow })).AllowAnonymous();

app.Run();
