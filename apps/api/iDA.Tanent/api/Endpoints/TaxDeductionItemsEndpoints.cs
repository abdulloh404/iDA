using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.DoctorFee402;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class TaxDeductionItemsEndpoints
{
    public static void MapTaxDeductionItemsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "tax-deduction-items")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'tax-deduction-items'.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/tax-deduction-items")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DfTaxDeductionItem, TaxDeductionItemRow>(ListQueryString.Read(http)), ct)))
            .WithName("tax-deduction-items_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<TaxDeductionItemRow>>()
            .RequirePermission("tax-deduction-items.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DfTaxDeductionItem, TaxDeductionItemDetail>(id), ct)))
            .WithName("tax-deduction-items_get")
            .Produces<TaxDeductionItemDetail>()
            .RequirePermission("tax-deduction-items.read");

        group.MapPost("/", async (HttpRequest http, TaxDeductionItemInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DfTaxDeductionItem, TaxDeductionItemDetail, TaxDeductionItemInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/tax-deduction-items", created);
            })
            .WithName("tax-deduction-items_create")
            .Produces<TaxDeductionItemDetail>(StatusCodes.Status201Created)
            .RequirePermission("tax-deduction-items.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, TaxDeductionItemInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DfTaxDeductionItem, TaxDeductionItemDetail, TaxDeductionItemInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("tax-deduction-items_update")
            .Produces<TaxDeductionItemDetail>()
            .RequirePermission("tax-deduction-items.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DfTaxDeductionItem>(id), ct);
                return Results.NoContent();
            })
            .WithName("tax-deduction-items_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("tax-deduction-items.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DfTaxDeductionItem>(id), ct)))
            .WithName("tax-deduction-items_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("tax-deduction-items.read");
    }
}
