using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.Tax402;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class AdjustmentTypesEndpoints
{
    public static void MapAdjustmentTypesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "adjustment-types")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'adjustment-types'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/adjustment-types")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<MstAdjustmentType, AdjustmentTypeListItem>(ListQueryString.Read(http)), ct)))
            .WithName("adjustment-types_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<AdjustmentTypeListItem>>()
            .RequirePermission("adjustment-types.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<MstAdjustmentType, AdjustmentTypeDetail>(id), ct)))
            .WithName("adjustment-types_get")
            .Produces<AdjustmentTypeDetail>()
            .RequirePermission("adjustment-types.read");

        group.MapPost("/", async (HttpRequest http, AdjustmentTypeInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<MstAdjustmentType, AdjustmentTypeDetail, AdjustmentTypeInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/adjustment-types", created);
            })
            .WithName("adjustment-types_create")
            .Produces<AdjustmentTypeDetail>(StatusCodes.Status201Created)
            .RequirePermission("adjustment-types.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, AdjustmentTypeInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<MstAdjustmentType, AdjustmentTypeDetail, AdjustmentTypeInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("adjustment-types_update")
            .Produces<AdjustmentTypeDetail>()
            .RequirePermission("adjustment-types.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<MstAdjustmentType>(id), ct);
                return Results.NoContent();
            })
            .WithName("adjustment-types_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("adjustment-types.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<MstAdjustmentType>(id), ct)))
            .WithName("adjustment-types_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("adjustment-types.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<MstAdjustmentType>(ListQueryString.Read(http)), ct)))
            .WithName("adjustment-types_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<MstAdjustmentType, AdjustmentTypeListItem, AdjustmentTypeDetail, AdjustmentTypeInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("adjustment-types.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<MstAdjustmentType, AdjustmentTypeListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("adjustment-types_export")
            .RequirePermission("adjustment-types.export");
    }
}
