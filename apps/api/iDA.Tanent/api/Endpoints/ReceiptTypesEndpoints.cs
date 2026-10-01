using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.Accounting;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class ReceiptTypesEndpoints
{
    public static void MapReceiptTypesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "receipt-types")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'receipt-types'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/receipt-types")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<MstReceiptType, ReceiptTypeListItem>(ListQueryString.Read(http)), ct)))
            .WithName("receipt-types_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<ReceiptTypeListItem>>()
            .RequirePermission("receipt-types.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<MstReceiptType, ReceiptTypeDetail>(id), ct)))
            .WithName("receipt-types_get")
            .Produces<ReceiptTypeDetail>()
            .RequirePermission("receipt-types.read");

        group.MapPost("/", async (HttpRequest http, ReceiptTypeInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<MstReceiptType, ReceiptTypeDetail, ReceiptTypeInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/receipt-types", created);
            })
            .WithName("receipt-types_create")
            .Produces<ReceiptTypeDetail>(StatusCodes.Status201Created)
            .RequirePermission("receipt-types.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, ReceiptTypeInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<MstReceiptType, ReceiptTypeDetail, ReceiptTypeInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("receipt-types_update")
            .Produces<ReceiptTypeDetail>()
            .RequirePermission("receipt-types.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<MstReceiptType>(id), ct);
                return Results.NoContent();
            })
            .WithName("receipt-types_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("receipt-types.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<MstReceiptType>(id), ct)))
            .WithName("receipt-types_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("receipt-types.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<MstReceiptType>(ListQueryString.Read(http)), ct)))
            .WithName("receipt-types_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<MstReceiptType, ReceiptTypeListItem, ReceiptTypeDetail, ReceiptTypeInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("receipt-types.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<MstReceiptType, ReceiptTypeListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("receipt-types_export")
            .RequirePermission("receipt-types.export");
    }
}
