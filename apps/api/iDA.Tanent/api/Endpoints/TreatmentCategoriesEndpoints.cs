using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.Accounting;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class TreatmentCategoriesEndpoints
{
    public static void MapTreatmentCategoriesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "treatment-categories")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'treatment-categories'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/treatment-categories")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<MstTreatmentCategory, TreatmentCategoryListItem>(ListQueryString.Read(http)), ct)))
            .WithName("treatment-categories_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<TreatmentCategoryListItem>>()
            .RequirePermission("treatment-categories.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<MstTreatmentCategory, TreatmentCategoryDetail>(id), ct)))
            .WithName("treatment-categories_get")
            .Produces<TreatmentCategoryDetail>()
            .RequirePermission("treatment-categories.read");

        group.MapPost("/", async (HttpRequest http, TreatmentCategoryInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<MstTreatmentCategory, TreatmentCategoryDetail, TreatmentCategoryInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/treatment-categories", created);
            })
            .WithName("treatment-categories_create")
            .Produces<TreatmentCategoryDetail>(StatusCodes.Status201Created)
            .RequirePermission("treatment-categories.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, TreatmentCategoryInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<MstTreatmentCategory, TreatmentCategoryDetail, TreatmentCategoryInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("treatment-categories_update")
            .Produces<TreatmentCategoryDetail>()
            .RequirePermission("treatment-categories.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<MstTreatmentCategory>(id), ct);
                return Results.NoContent();
            })
            .WithName("treatment-categories_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("treatment-categories.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<MstTreatmentCategory>(id), ct)))
            .WithName("treatment-categories_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("treatment-categories.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<MstTreatmentCategory>(ListQueryString.Read(http)), ct)))
            .WithName("treatment-categories_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<MstTreatmentCategory, TreatmentCategoryListItem, TreatmentCategoryDetail, TreatmentCategoryInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("treatment-categories.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<MstTreatmentCategory, TreatmentCategoryListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("treatment-categories_export")
            .RequirePermission("treatment-categories.export");
    }
}
