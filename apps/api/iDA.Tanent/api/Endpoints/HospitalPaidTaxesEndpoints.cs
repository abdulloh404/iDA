using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.DoctorFee402;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class HospitalPaidTaxesEndpoints
{
    public static void MapHospitalPaidTaxesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "hospital-paid-taxes")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'hospital-paid-taxes'.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/hospital-paid-taxes")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DfHospitalPaidTax, HospitalPaidTaxListItem>(ListQueryString.Read(http)), ct)))
            .WithName("hospital-paid-taxes_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<HospitalPaidTaxListItem>>()
            .RequirePermission("hospital-paid-taxes.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DfHospitalPaidTax, HospitalPaidTaxDetail>(id), ct)))
            .WithName("hospital-paid-taxes_get")
            .Produces<HospitalPaidTaxDetail>()
            .RequirePermission("hospital-paid-taxes.read");

        group.MapPost("/", async (HttpRequest http, HospitalPaidTaxInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DfHospitalPaidTax, HospitalPaidTaxDetail, HospitalPaidTaxInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/hospital-paid-taxes", created);
            })
            .WithName("hospital-paid-taxes_create")
            .Produces<HospitalPaidTaxDetail>(StatusCodes.Status201Created)
            .RequirePermission("hospital-paid-taxes.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, HospitalPaidTaxInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DfHospitalPaidTax, HospitalPaidTaxDetail, HospitalPaidTaxInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("hospital-paid-taxes_update")
            .Produces<HospitalPaidTaxDetail>()
            .RequirePermission("hospital-paid-taxes.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DfHospitalPaidTax>(id), ct);
                return Results.NoContent();
            })
            .WithName("hospital-paid-taxes_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("hospital-paid-taxes.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DfHospitalPaidTax>(id), ct)))
            .WithName("hospital-paid-taxes_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("hospital-paid-taxes.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<DfHospitalPaidTax, HospitalPaidTaxListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("hospital-paid-taxes_export")
            .RequirePermission("hospital-paid-taxes.export");
    }
}
