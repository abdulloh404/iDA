using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class TaxTypesEndpoints
{
    public static void MapTaxTypesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "tax-types")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'tax-types'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/tax-types")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<MstTaxType, MasterListItem>(ListQueryString.Read(http)), ct)))
            .WithName("tax-types_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<MasterListItem>>()
            .RequirePermission("tax-types.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<MstTaxType, MasterDetail>(id), ct)))
            .WithName("tax-types_get")
            .Produces<MasterDetail>()
            .RequirePermission("tax-types.read");

        group.MapPost("/", async (HttpRequest http, MasterInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<MstTaxType, MasterDetail, MasterInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/tax-types", created);
            })
            .WithName("tax-types_create")
            .Produces<MasterDetail>(StatusCodes.Status201Created)
            .RequirePermission("tax-types.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, MasterInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<MstTaxType, MasterDetail, MasterInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("tax-types_update")
            .Produces<MasterDetail>()
            .RequirePermission("tax-types.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<MstTaxType>(id), ct);
                return Results.NoContent();
            })
            .WithName("tax-types_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("tax-types.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<MstTaxType>(id), ct)))
            .WithName("tax-types_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("tax-types.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<MstTaxType>(ListQueryString.Read(http)), ct)))
            .WithName("tax-types_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<MstTaxType, MasterListItem, MasterDetail, MasterInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("tax-types.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<MstTaxType, MasterListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("tax-types_export")
            .RequirePermission("tax-types.export");
    }
}
