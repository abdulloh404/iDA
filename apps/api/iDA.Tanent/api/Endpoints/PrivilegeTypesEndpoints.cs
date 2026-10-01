using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class PrivilegeTypesEndpoints
{
    public static void MapPrivilegeTypesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "privilege-types")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'privilege-types'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/privilege-types")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<MstPrivilegeType, MasterListItem>(ListQueryString.Read(http)), ct)))
            .WithName("privilege-types_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<MasterListItem>>()
            .RequirePermission("privilege-types.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<MstPrivilegeType, MasterDetail>(id), ct)))
            .WithName("privilege-types_get")
            .Produces<MasterDetail>()
            .RequirePermission("privilege-types.read");

        group.MapPost("/", async (HttpRequest http, MasterInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<MstPrivilegeType, MasterDetail, MasterInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/privilege-types", created);
            })
            .WithName("privilege-types_create")
            .Produces<MasterDetail>(StatusCodes.Status201Created)
            .RequirePermission("privilege-types.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, MasterInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<MstPrivilegeType, MasterDetail, MasterInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("privilege-types_update")
            .Produces<MasterDetail>()
            .RequirePermission("privilege-types.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<MstPrivilegeType>(id), ct);
                return Results.NoContent();
            })
            .WithName("privilege-types_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("privilege-types.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<MstPrivilegeType>(id), ct)))
            .WithName("privilege-types_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("privilege-types.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<MstPrivilegeType>(ListQueryString.Read(http)), ct)))
            .WithName("privilege-types_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<MstPrivilegeType, MasterListItem, MasterDetail, MasterInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("privilege-types.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<MstPrivilegeType, MasterListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("privilege-types_export")
            .RequirePermission("privilege-types.export");
    }
}
