using System.Net;
using System.Text;
using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Auth;
using Ida.Application.Features.Hello.Queries;
using Ida.Infrastructure;
using Ida.Infrastructure.Configuration;
using Ida.Infrastructure.Databases;
using Ida.Infrastructure.Security;
using Ida.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Ida.Api;

public static class ApiSetup
{
    public static void ConfigureIdaApi(this WebApplicationBuilder builder, string[] args, DatabaseRuntime runtime)
    {
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
        builder.Configuration.AddIdaSettings(builder.Environment.EnvironmentName, reloadOnChange: true);
        builder.Configuration.AddCommandLine(args);
        var buId = (builder.Configuration["Api:BuId"] ?? builder.Configuration["BU_ID"])?.Trim().ToUpperInvariant();
        var serviceSection = runtime == DatabaseRuntime.Core ? "Api:Core" : $"Api:Tenants:{buId}";
        var urls = builder.Configuration["Api:Urls"] ?? builder.Configuration[$"{serviceSection}:Url"];
        if (string.IsNullOrWhiteSpace(builder.Configuration["urls"]) && !string.IsNullOrWhiteSpace(urls)) builder.WebHost.UseUrls(urls);
    }

    public static DatabaseRuntime ReadApiRuntime(IConfiguration configuration, DatabaseRuntime expected)
    {
        if (expected is not (DatabaseRuntime.Core or DatabaseRuntime.Tenant)) throw new ArgumentOutOfRangeException(nameof(expected));
        var mode = configuration["Api:Mode"];
        if (mode is null || string.Equals(mode, expected.ToString(), StringComparison.OrdinalIgnoreCase)) return expected;
        throw new InvalidOperationException($"Api:Mode must be '{expected}' for this API host.");
    }

    public static IServiceCollection AddIdaApi(this IServiceCollection services, IConfiguration configuration, DatabaseRuntime runtime)
    {
        var serviceApiKey = ServiceApiClient.ReadKey(configuration);
        if (runtime != DatabaseRuntime.Management && (string.IsNullOrWhiteSpace(serviceApiKey) || serviceApiKey.Length < 32))
            throw new InvalidOperationException("Api:ServiceKey / IDA_SERVICE_API_KEY must be at least 32 characters.");

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
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            foreach (var value in configuration.GetSection("Api:KnownProxies").Get<string[]>() ?? [])
            {
                if (!IPAddress.TryParse(value, out var address)) throw new InvalidOperationException("Api:KnownProxies must contain IP addresses.");
                options.KnownProxies.Add(address);
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) options.KnownProxies.Add(address.MapToIPv6());
            }
        });

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

        var origins = configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
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
        var serviceSection = runtime == DatabaseRuntime.Tenant ? $"Api:Tenants:{databaseRegistry.FixedBranch.ConnectionKey}" : "Api:Core";
        var pathBase = app.Configuration["Api:PathBase"] ?? app.Configuration[$"{serviceSection}:PathBase"] ?? app.Configuration["API_PATH_BASE"] ?? (runtime == DatabaseRuntime.Tenant
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
