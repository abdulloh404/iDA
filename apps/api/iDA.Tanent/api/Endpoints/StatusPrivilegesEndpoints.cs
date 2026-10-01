using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class StatusPrivilegesEndpoints
{
    public static void MapStatusPrivilegesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "status-privileges")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'status-privileges'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/status-privileges")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<MstStatusPrivilege, MasterListItem>(ListQueryString.Read(http)), ct)))
            .WithName("status-privileges_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<MasterListItem>>()
            .RequirePermission("status-privileges.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<MstStatusPrivilege, MasterDetail>(id), ct)))
            .WithName("status-privileges_get")
            .Produces<MasterDetail>()
            .RequirePermission("status-privileges.read");

        group.MapPost("/", async (HttpRequest http, MasterInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<MstStatusPrivilege, MasterDetail, MasterInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/status-privileges", created);
            })
            .WithName("status-privileges_create")
            .Produces<MasterDetail>(StatusCodes.Status201Created)
            .RequirePermission("status-privileges.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, MasterInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<MstStatusPrivilege, MasterDetail, MasterInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("status-privileges_update")
            .Produces<MasterDetail>()
            .RequirePermission("status-privileges.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<MstStatusPrivilege>(id), ct);
                return Results.NoContent();
            })
            .WithName("status-privileges_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("status-privileges.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<MstStatusPrivilege>(id), ct)))
            .WithName("status-privileges_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("status-privileges.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<MstStatusPrivilege>(ListQueryString.Read(http)), ct)))
            .WithName("status-privileges_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<MstStatusPrivilege, MasterListItem, MasterDetail, MasterInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("status-privileges.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<MstStatusPrivilege, MasterListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("status-privileges_export")
            .RequirePermission("status-privileges.export");
    }
}
