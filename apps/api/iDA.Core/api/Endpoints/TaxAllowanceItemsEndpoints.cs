using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.Tax402;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class TaxAllowanceItemsEndpoints
{
    public static void MapTaxAllowanceItemsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "tax-allowance-items")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'tax-allowance-items'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/tax-allowance-items")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<TaxAllowanceItem, TaxAllowanceItemListItem>(ListQueryString.Read(http)), ct)))
            .WithName("tax-allowance-items_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<TaxAllowanceItemListItem>>()
            .RequirePermission("tax-allowance-items.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<TaxAllowanceItem, TaxAllowanceItemDetail>(id), ct)))
            .WithName("tax-allowance-items_get")
            .Produces<TaxAllowanceItemDetail>()
            .RequirePermission("tax-allowance-items.read");

        group.MapPost("/", async (HttpRequest http, TaxAllowanceItemInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<TaxAllowanceItem, TaxAllowanceItemDetail, TaxAllowanceItemInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/tax-allowance-items", created);
            })
            .WithName("tax-allowance-items_create")
            .Produces<TaxAllowanceItemDetail>(StatusCodes.Status201Created)
            .RequirePermission("tax-allowance-items.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, TaxAllowanceItemInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<TaxAllowanceItem, TaxAllowanceItemDetail, TaxAllowanceItemInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("tax-allowance-items_update")
            .Produces<TaxAllowanceItemDetail>()
            .RequirePermission("tax-allowance-items.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<TaxAllowanceItem>(id), ct);
                return Results.NoContent();
            })
            .WithName("tax-allowance-items_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("tax-allowance-items.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<TaxAllowanceItem>(id), ct)))
            .WithName("tax-allowance-items_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("tax-allowance-items.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<TaxAllowanceItem>(ListQueryString.Read(http)), ct)))
            .WithName("tax-allowance-items_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<TaxAllowanceItem, TaxAllowanceItemListItem, TaxAllowanceItemDetail, TaxAllowanceItemInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("tax-allowance-items.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<TaxAllowanceItem, TaxAllowanceItemListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("tax-allowance-items_export")
            .RequirePermission("tax-allowance-items.export");
    }
}
