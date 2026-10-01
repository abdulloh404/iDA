using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class ShareCategoriesEndpoints
{
    public static void MapShareCategoriesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "share-categories")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'share-categories'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/share-categories")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<MstShareCategory, MasterListItem>(ListQueryString.Read(http)), ct)))
            .WithName("share-categories_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<MasterListItem>>()
            .RequirePermission("share-categories.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<MstShareCategory, MasterDetail>(id), ct)))
            .WithName("share-categories_get")
            .Produces<MasterDetail>()
            .RequirePermission("share-categories.read");

        group.MapPost("/", async (HttpRequest http, MasterInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<MstShareCategory, MasterDetail, MasterInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/share-categories", created);
            })
            .WithName("share-categories_create")
            .Produces<MasterDetail>(StatusCodes.Status201Created)
            .RequirePermission("share-categories.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, MasterInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<MstShareCategory, MasterDetail, MasterInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("share-categories_update")
            .Produces<MasterDetail>()
            .RequirePermission("share-categories.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<MstShareCategory>(id), ct);
                return Results.NoContent();
            })
            .WithName("share-categories_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("share-categories.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<MstShareCategory>(id), ct)))
            .WithName("share-categories_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("share-categories.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<MstShareCategory>(ListQueryString.Read(http)), ct)))
            .WithName("share-categories_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<MstShareCategory, MasterListItem, MasterDetail, MasterInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("share-categories.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<MstShareCategory, MasterListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("share-categories_export")
            .RequirePermission("share-categories.export");
    }
}
