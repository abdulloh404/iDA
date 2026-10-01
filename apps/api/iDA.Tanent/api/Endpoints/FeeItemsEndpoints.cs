using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.DoctorFee402;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class FeeItemsEndpoints
{
    public static void MapFeeItemsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "fee-items")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'fee-items'.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/fee-items")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DfFeeItem, FeeItemListItem>(ListQueryString.Read(http)), ct)))
            .WithName("fee-items_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<FeeItemListItem>>()
            .RequirePermission("fee-items.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DfFeeItem, FeeItemDetail>(id), ct)))
            .WithName("fee-items_get")
            .Produces<FeeItemDetail>()
            .RequirePermission("fee-items.read");

        group.MapPost("/", async (HttpRequest http, FeeItemInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DfFeeItem, FeeItemDetail, FeeItemInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/fee-items", created);
            })
            .WithName("fee-items_create")
            .Produces<FeeItemDetail>(StatusCodes.Status201Created)
            .RequirePermission("fee-items.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, FeeItemInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DfFeeItem, FeeItemDetail, FeeItemInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("fee-items_update")
            .Produces<FeeItemDetail>()
            .RequirePermission("fee-items.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DfFeeItem>(id), ct);
                return Results.NoContent();
            })
            .WithName("fee-items_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("fee-items.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DfFeeItem>(id), ct)))
            .WithName("fee-items_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("fee-items.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<DfFeeItem, FeeItemListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("fee-items_export")
            .RequirePermission("fee-items.export");
    }
}
