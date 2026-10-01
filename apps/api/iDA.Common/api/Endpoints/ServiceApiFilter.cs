using System.Security.Cryptography;
using System.Text;
using Ida.Application.Common;
using Ida.Infrastructure.Persistence;
using Ida.Infrastructure.Services;

namespace Ida.Api.Endpoints;

public sealed class ServiceApiFilter(IConfiguration configuration, IRuntimeDatabaseContexts contexts) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var expected = ServiceApiClient.ReadKey(configuration);
        var supplied = context.HttpContext.Request.Headers[ServiceApiClient.KeyHeader].ToString();
        if (expected is null || expected.Length < 32 || supplied.Length == 0 || supplied.Length > 4096
            || !CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(expected)), SHA256.HashData(Encoding.UTF8.GetBytes(supplied))))
            throw ApiException.Unauthorized("invalid_service_credentials", "ไม่อนุญาตให้เรียกบริการภายใน");
        await contexts.InitializeAsync(context.HttpContext.RequestAborted);
        return await next(context);
    }
}
