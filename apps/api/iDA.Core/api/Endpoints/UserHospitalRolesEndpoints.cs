using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Users;
using Ida.Domain.Auth;
using MediatR;

namespace Ida.Api.Endpoints;

public static class UserHospitalRolesEndpoints
{
    public static void MapUserHospitalRolesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "user-hospital-roles")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'user-hospital-roles'.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/user-hospital-roles")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<UserHospitalRole, UserHospitalRoleRow>(ListQueryString.Read(http)), ct)))
            .WithName("user-hospital-roles_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<UserHospitalRoleRow>>()
            .RequirePermission("user-hospital-roles.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<UserHospitalRole, UserHospitalRoleDetail>(id), ct)))
            .WithName("user-hospital-roles_get")
            .Produces<UserHospitalRoleDetail>()
            .RequirePermission("user-hospital-roles.read");

        group.MapPost("/", async (HttpRequest http, UserHospitalRoleInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<UserHospitalRole, UserHospitalRoleDetail, UserHospitalRoleInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/user-hospital-roles", created);
            })
            .WithName("user-hospital-roles_create")
            .Produces<UserHospitalRoleDetail>(StatusCodes.Status201Created)
            .RequirePermission("user-hospital-roles.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, UserHospitalRoleInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<UserHospitalRole, UserHospitalRoleDetail, UserHospitalRoleInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("user-hospital-roles_update")
            .Produces<UserHospitalRoleDetail>()
            .RequirePermission("user-hospital-roles.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<UserHospitalRole>(id), ct);
                return Results.NoContent();
            })
            .WithName("user-hospital-roles_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("user-hospital-roles.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<UserHospitalRole>(id), ct)))
            .WithName("user-hospital-roles_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("user-hospital-roles.read");
    }
}
