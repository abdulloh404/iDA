using System.Text;
using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Auth;
using Ida.Application.Features.Hello.Queries;
using Ida.Infrastructure;
using Ida.Infrastructure.Databases;
using Ida.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Ida.Api;

public static class ApiSetup
{
    public static void ConfigureIdaApi(this WebApplicationBuilder builder, string[] args)
    {
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
        builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
        builder.Configuration.AddEnvironmentVariables();
        builder.Configuration.AddCommandLine(args);
    }

    public static IServiceCollection AddIdaApi(this IServiceCollection services, IConfiguration configuration, DatabaseRuntime runtime)
    {
        var serviceApiKey = configuration["IDA_SERVICE_API_KEY"];
        if (runtime != DatabaseRuntime.Management && (string.IsNullOrWhiteSpace(serviceApiKey) || serviceApiKey.Length < 32))
            throw new InvalidOperationException("IDA_SERVICE_API_KEY must be at least 32 characters.");

        services.ConfigureHttpJsonOptions(o => o.SerializerOptions.ConfigureIdaJson());
        services.AddMediatR(c =>
        {
            c.RegisterServicesFromAssemblyContaining<GetHelloQuery>();
            c.AddOpenBehavior(typeof(CqrsLoggingBehavior<,>));
        });
        services.AddCrudResources(typeof(GetHelloQuery).Assembly);
        services.AddScoped<SessionBuilder>();
        services.AddInfrastructure(configuration, runtime);
        services.Configure<ForwardedHeadersOptions>(options =>
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

        var jwt = JwtTokenService.Read(configuration);
        services.AddAuthentication("Bearer").AddJwtBearer(options =>
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

        services.AddAuthorization();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

        var origins = configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:3000"];
        services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = runtime == DatabaseRuntime.Tenant ? "iDA Tenant API" : "iDA Core API",
                Version = "v1",
            });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "วาง token ที่ได้จาก Core API /api/auth/login",
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

        return services;
    }

    public static void UseIdaApi(this WebApplication app, DatabaseRuntime runtime)
    {
        app.UseForwardedHeaders();
        var databaseRegistry = app.Services.GetRequiredService<DatabaseRegistry>();
        var pathBase = app.Configuration["API_PATH_BASE"] ?? (runtime == DatabaseRuntime.Tenant
            ? app.Configuration[$"{databaseRegistry.FixedBranch.ConnectionKey}_API_PATH"] ?? "/" + databaseRegistry.FixedBranch.ConnectionKey.ToLowerInvariant()
            : "/core");
        if (!string.IsNullOrWhiteSpace(pathBase)) app.UsePathBase(pathBase);
        app.UseRouting();
        app.UseCors();
        app.UseMiddleware<ErrorEnvelopeMiddleware>();
        app.UseAuthentication();
        app.Use(async (context, next) =>
        {
            if (runtime == DatabaseRuntime.Tenant && context.User.Identity?.IsAuthenticated == true)
            {
                var registry = context.RequestServices.GetRequiredService<DatabaseRegistry>();
                var hospitalId = context.User.FindFirst(IdaClaims.HospitalId)?.Value;
                if (!string.Equals(hospitalId, registry.FixedBranch.HospitalId, StringComparison.Ordinal))
                    throw ApiException.Forbidden("tenant_mismatch", "Token ไม่ตรงกับโรงพยาบาลของ Tenant API นี้ กรุณาเปลี่ยนโรงพยาบาลก่อน");
                await context.RequestServices.GetRequiredService<DatabaseContexts>().InitializeAsync(context.RequestAborted);
            }
            await next(context);
        });
        app.UseAuthorization();
        app.UseSwagger(options => options.PreSerializeFilters.Add((document, request) =>
            document.Servers = [new OpenApiServer { Url = request.PathBase.HasValue ? request.PathBase.Value! : "/" }]));
        app.UseSwaggerUI(options => options.SwaggerEndpoint("v1/swagger.json", "iDA API v1"));
    }
}
