using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Users;
using Ida.Domain.Auth;
using MediatR;

namespace Ida.Api.Endpoints;

public static class RolesEndpoints
{
    public static void MapRolesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "roles")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'roles'.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/roles")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<Role, RoleListItem>(ListQueryString.Read(http)), ct)))
            .WithName("roles_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<RoleListItem>>()
            .RequirePermission("roles.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<Role, RoleDetail>(id), ct)))
            .WithName("roles_get")
            .Produces<RoleDetail>()
            .RequirePermission("roles.read");

        group.MapPost("/", async (HttpRequest http, RoleInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<Role, RoleDetail, RoleInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/roles", created);
            })
            .WithName("roles_create")
            .Produces<RoleDetail>(StatusCodes.Status201Created)
            .RequirePermission("roles.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, RoleInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<Role, RoleDetail, RoleInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("roles_update")
            .Produces<RoleDetail>()
            .RequirePermission("roles.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<Role>(id), ct);
                return Results.NoContent();
            })
            .WithName("roles_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("roles.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<Role>(id), ct)))
            .WithName("roles_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("roles.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<Role>(ListQueryString.Read(http)), ct)))
            .WithName("roles_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<Role, RoleListItem, RoleDetail, RoleInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("roles.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<Role, RoleListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("roles_export")
            .RequirePermission("roles.export");
    }
}
