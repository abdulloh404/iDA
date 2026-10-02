using System.Net;
using System.Reflection;
using System.Text;
using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Hello.Queries;
using Ida.Infrastructure.Configuration;
using Ida.Infrastructure.Security;
using Ida.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Ida.Api;

public static class CommonApiSetup
{
    public static void ConfigureCommonApi(this WebApplicationBuilder builder, string[] args, string serviceSection) => ConfigureCommonApi(builder, args, _ => serviceSection);

    public static void ConfigureCommonApi(this WebApplicationBuilder builder, string[] args, Func<IConfiguration, string> serviceSection)
    {
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
        builder.Configuration.AddIdaSettings(builder.Environment.EnvironmentName, reloadOnChange: true);
        builder.Configuration.AddCommandLine(args);
        var urls = builder.Configuration["Api:Urls"] ?? builder.Configuration[$"{serviceSection(builder.Configuration)}:Url"];
        if (string.IsNullOrWhiteSpace(builder.Configuration["urls"]) && !string.IsNullOrWhiteSpace(urls)) builder.WebHost.UseUrls(urls);
    }

    public static IServiceCollection AddCommonApi(this IServiceCollection services, IConfiguration configuration, Assembly applicationAssembly, string title, bool requireServiceKey = true)
    {
        var serviceApiKey = ServiceApiClient.ReadKey(configuration);
        if (requireServiceKey && (string.IsNullOrWhiteSpace(serviceApiKey) || serviceApiKey.Length < 32))
            throw new InvalidOperationException("Api:ServiceKey / IDA_SERVICE_API_KEY must be at least 32 characters.");

        services.ConfigureHttpJsonOptions(o => o.SerializerOptions.ConfigureIdaJson());
        services.AddMediatR(c =>
        {
            c.RegisterServicesFromAssemblies(typeof(GetHelloQuery).Assembly, applicationAssembly);
            c.AddOpenBehavior(typeof(CqrsLoggingBehavior<,>));
        });
        services.AddCrudResources(applicationAssembly);
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            var knownProxies = configuration["API_KNOWN_PROXIES"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? configuration.GetSection("Api:KnownProxies").Get<string[]>() ?? [];
            foreach (var value in knownProxies)
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

        var origins = configuration["CORS_ORIGINS"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
        services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = title,
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

    public static void UseCommonApi(this WebApplication app, string? pathBase, Action<WebApplication>? configureTenant = null)
    {
        app.UseForwardedHeaders();
        if (!string.IsNullOrWhiteSpace(pathBase)) app.UsePathBase(pathBase);
        app.UseRouting();
        app.UseCors();
        app.UseMiddleware<ErrorEnvelopeMiddleware>();
        app.UseAuthentication();
        configureTenant?.Invoke(app);
        app.UseAuthorization();
        app.UseSwagger(options => options.PreSerializeFilters.Add((document, request) =>
            document.Servers = [new OpenApiServer { Url = request.PathBase.HasValue ? request.PathBase.Value! : "/" }]));
        app.UseSwaggerUI(options => options.SwaggerEndpoint("v1/swagger.json", "iDA API v1"));
    }
}
