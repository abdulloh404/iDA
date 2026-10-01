using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.DoctorFee402;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class TaxExemptionsEndpoints
{
    public static void MapTaxExemptionsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "tax-exemptions")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'tax-exemptions'.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/tax-exemptions")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DfTaxExemption, TaxExemptionListItem>(ListQueryString.Read(http)), ct)))
            .WithName("tax-exemptions_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<TaxExemptionListItem>>()
            .RequirePermission("tax-exemptions.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DfTaxExemption, TaxExemptionDetail>(id), ct)))
            .WithName("tax-exemptions_get")
            .Produces<TaxExemptionDetail>()
            .RequirePermission("tax-exemptions.read");

        group.MapPost("/", async (HttpRequest http, TaxExemptionInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DfTaxExemption, TaxExemptionDetail, TaxExemptionInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/tax-exemptions", created);
            })
            .WithName("tax-exemptions_create")
            .Produces<TaxExemptionDetail>(StatusCodes.Status201Created)
            .RequirePermission("tax-exemptions.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, TaxExemptionInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DfTaxExemption, TaxExemptionDetail, TaxExemptionInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("tax-exemptions_update")
            .Produces<TaxExemptionDetail>()
            .RequirePermission("tax-exemptions.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DfTaxExemption>(id), ct);
                return Results.NoContent();
            })
            .WithName("tax-exemptions_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("tax-exemptions.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DfTaxExemption>(id), ct)))
            .WithName("tax-exemptions_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("tax-exemptions.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<DfTaxExemption, TaxExemptionListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("tax-exemptions_export")
            .RequirePermission("tax-exemptions.export");
    }
}
