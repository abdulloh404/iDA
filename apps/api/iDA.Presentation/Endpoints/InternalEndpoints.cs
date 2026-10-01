using System.Security.Cryptography;
using System.Text;
using Ida.Application.Common;
using Ida.Infrastructure.Databases;
using Ida.Infrastructure.Persistence;
using Ida.Infrastructure.Services;

namespace Ida.Api.Endpoints;

public static class InternalEndpoints
{
    public static IEndpointRouteBuilder MapCoreInternalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/internal/directory").AllowAnonymous().ExcludeFromDescription().AddEndpointFilter<ServiceApiFilter>();
        group.MapPost("/doctors", (ReferenceIds input, ICoreDirectory core, CancellationToken ct) => core.DoctorsAsync(input.Validated(), ct));
        group.MapPost("/doctor-search", (CoreDoctorSearch input, ICoreDirectory core, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(input.Text) || input.Text.Length > 500)
                throw ApiException.BadRequest("invalid_search", "คำค้นต้องมีความยาว 1 ถึง 500 ตัวอักษร");
            return core.FindDoctorIdsAsync(input, ct);
        });
        group.MapPost("/doctor-emails", (ReferenceIds input, ICoreDirectory core, CancellationToken ct) => core.DoctorEmailsAsync(input.Validated(), ct));
        group.MapPost("/doctor-order", (ReferenceOrder input, ICoreDirectory core, CancellationToken ct) => core.OrderDoctorIdsAsync(input.Validated(), input.Descending, ct));
        group.MapPost("/tax-allowance-order", (ReferenceOrder input, ICoreDirectory core, CancellationToken ct) => core.OrderTaxAllowanceIdsAsync(input.Validated(), input.Descending, ct));
        group.MapPost("/banks", (ReferenceIds input, ICoreDirectory core, CancellationToken ct) => core.BanksAsync(input.Validated(), ct));
        group.MapPost("/bank-branches", (ReferenceIds input, ICoreDirectory core, CancellationToken ct) => core.BankBranchesAsync(input.Validated(), ct));
        group.MapPost("/specialties", (ReferenceIds input, ICoreDirectory core, CancellationToken ct) => core.SpecialtiesAsync(input.Validated(), ct));
        group.MapPost("/sub-specialties", (ReferenceIds input, ICoreDirectory core, CancellationToken ct) => core.SubSpecialtiesAsync(input.Validated(), ct));
        group.MapPost("/document-types", (ReferenceIds input, ICoreDirectory core, CancellationToken ct) => core.DocumentTypesAsync(input.Validated(), ct));
        group.MapPost("/tax-allowances", (ReferenceIds input, ICoreDirectory core, CancellationToken ct) => core.TaxAllowancesAsync(input.Validated(), ct));
        group.MapPost("/ingest-definitions", (ICoreDirectory core, CancellationToken ct) => core.IngestDefinitionsAsync(ct));
        return app;
    }

    public static IEndpointRouteBuilder MapTenantInternalEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/internal/references", (CoreReferenceRequest input, ReferenceGuard guard, CancellationToken ct) => guard.CheckCoreReferenceAsync(input, ct))
            .AllowAnonymous().ExcludeFromDescription().AddEndpointFilter<ServiceApiFilter>();
        return app;
    }

    public record ReferenceIds(Guid[]? Ids)
    {
        public Guid[] Validated() => Ids is { Length: <= 500 } ? Ids
            : throw ApiException.BadRequest("invalid_reference_ids", "ระบุรหัสข้อมูลไม่เกิน 500 รายการต่อครั้ง");
    }

    public record ReferenceOrder(Guid[]? Ids, bool Descending)
    {
        public Guid[] Validated() => Ids ?? throw ApiException.BadRequest("invalid_reference_ids", "ต้องระบุรหัสข้อมูลที่ต้องการเรียงลำดับ");
    }
}

public sealed class ServiceApiFilter(IConfiguration configuration, DatabaseRegistry registry, DatabaseContexts contexts) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var expected = configuration[ServiceApiClient.KeySetting];
        var supplied = context.HttpContext.Request.Headers[ServiceApiClient.KeyHeader].ToString();
        if (expected is null || expected.Length < 32 || supplied.Length == 0 || supplied.Length > 4096
            || !CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(expected)), SHA256.HashData(Encoding.UTF8.GetBytes(supplied))))
            throw ApiException.Unauthorized("invalid_service_credentials", "ไม่อนุญาตให้เรียกบริการภายใน");
        if (registry.Runtime == DatabaseRuntime.Tenant) await contexts.InitializeAsync(context.HttpContext.RequestAborted);
        return await next(context);
    }
}
