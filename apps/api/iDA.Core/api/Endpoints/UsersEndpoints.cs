using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Users;
using Ida.Domain.Auth;
using MediatR;

namespace Ida.Api.Endpoints;

public static class UsersEndpoints
{
    public static void MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "users")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'users'.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/users")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<AppUser, UserListItem>(ListQueryString.Read(http)), ct)))
            .WithName("users_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<UserListItem>>()
            .RequirePermission("users.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<AppUser, UserDetail>(id), ct)))
            .WithName("users_get")
            .Produces<UserDetail>()
            .RequirePermission("users.read");

        group.MapPost("/", async (HttpRequest http, UserInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<AppUser, UserDetail, UserInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/users", created);
            })
            .WithName("users_create")
            .Produces<UserDetail>(StatusCodes.Status201Created)
            .RequirePermission("users.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, UserInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<AppUser, UserDetail, UserInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("users_update")
            .Produces<UserDetail>()
            .RequirePermission("users.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<AppUser>(id), ct);
                return Results.NoContent();
            })
            .WithName("users_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("users.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<AppUser>(id), ct)))
            .WithName("users_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("users.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<AppUser, UserListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("users_export")
            .RequirePermission("users.export");
    }
}
