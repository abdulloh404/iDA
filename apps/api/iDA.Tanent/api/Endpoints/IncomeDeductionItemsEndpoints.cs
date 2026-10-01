using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.Accounting;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class IncomeDeductionItemsEndpoints
{
    public static void MapIncomeDeductionItemsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "income-deduction-items")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'income-deduction-items'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/income-deduction-items")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<MstIncomeDeductionItem, IncomeDeductionItemListItem>(ListQueryString.Read(http)), ct)))
            .WithName("income-deduction-items_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<IncomeDeductionItemListItem>>()
            .RequirePermission("income-deduction-items.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<MstIncomeDeductionItem, IncomeDeductionItemDetail>(id), ct)))
            .WithName("income-deduction-items_get")
            .Produces<IncomeDeductionItemDetail>()
            .RequirePermission("income-deduction-items.read");

        group.MapPost("/", async (HttpRequest http, IncomeDeductionItemInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<MstIncomeDeductionItem, IncomeDeductionItemDetail, IncomeDeductionItemInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/income-deduction-items", created);
            })
            .WithName("income-deduction-items_create")
            .Produces<IncomeDeductionItemDetail>(StatusCodes.Status201Created)
            .RequirePermission("income-deduction-items.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, IncomeDeductionItemInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<MstIncomeDeductionItem, IncomeDeductionItemDetail, IncomeDeductionItemInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("income-deduction-items_update")
            .Produces<IncomeDeductionItemDetail>()
            .RequirePermission("income-deduction-items.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<MstIncomeDeductionItem>(id), ct);
                return Results.NoContent();
            })
            .WithName("income-deduction-items_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("income-deduction-items.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<MstIncomeDeductionItem>(id), ct)))
            .WithName("income-deduction-items_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("income-deduction-items.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<MstIncomeDeductionItem>(ListQueryString.Read(http)), ct)))
            .WithName("income-deduction-items_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<MstIncomeDeductionItem, IncomeDeductionItemListItem, IncomeDeductionItemDetail, IncomeDeductionItemInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("income-deduction-items.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<MstIncomeDeductionItem, IncomeDeductionItemListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("income-deduction-items_export")
            .RequirePermission("income-deduction-items.export");
    }
}
