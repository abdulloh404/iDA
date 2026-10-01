using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.Tax402;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class ExpenseTypesEndpoints
{
    public static void MapExpenseTypesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "expense-types")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'expense-types'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/expense-types")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<MstExpenseType, ExpenseTypeListItem>(ListQueryString.Read(http)), ct)))
            .WithName("expense-types_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<ExpenseTypeListItem>>()
            .RequirePermission("expense-types.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<MstExpenseType, ExpenseTypeDetail>(id), ct)))
            .WithName("expense-types_get")
            .Produces<ExpenseTypeDetail>()
            .RequirePermission("expense-types.read");

        group.MapPost("/", async (HttpRequest http, ExpenseTypeInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<MstExpenseType, ExpenseTypeDetail, ExpenseTypeInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/expense-types", created);
            })
            .WithName("expense-types_create")
            .Produces<ExpenseTypeDetail>(StatusCodes.Status201Created)
            .RequirePermission("expense-types.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, ExpenseTypeInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<MstExpenseType, ExpenseTypeDetail, ExpenseTypeInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("expense-types_update")
            .Produces<ExpenseTypeDetail>()
            .RequirePermission("expense-types.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<MstExpenseType>(id), ct);
                return Results.NoContent();
            })
            .WithName("expense-types_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("expense-types.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<MstExpenseType>(id), ct)))
            .WithName("expense-types_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("expense-types.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<MstExpenseType>(ListQueryString.Read(http)), ct)))
            .WithName("expense-types_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<MstExpenseType, ExpenseTypeListItem, ExpenseTypeDetail, ExpenseTypeInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("expense-types.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<MstExpenseType, ExpenseTypeListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("expense-types_export")
            .RequirePermission("expense-types.export");
    }
}
