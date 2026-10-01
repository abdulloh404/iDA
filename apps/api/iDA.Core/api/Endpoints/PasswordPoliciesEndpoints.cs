using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.SystemSettings;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class PasswordPoliciesEndpoints
{
    public static void MapPasswordPoliciesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "password-policies")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'password-policies'.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/password-policies")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<SysPasswordPolicy, PasswordPolicyDetail>(ListQueryString.Read(http)), ct)))
            .WithName("password-policies_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<PasswordPolicyDetail>>()
            .RequirePermission("password-policies.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<SysPasswordPolicy, PasswordPolicyDetail>(id), ct)))
            .WithName("password-policies_get")
            .Produces<PasswordPolicyDetail>()
            .RequirePermission("password-policies.read");

        group.MapPost("/", async (HttpRequest http, PasswordPolicyInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<SysPasswordPolicy, PasswordPolicyDetail, PasswordPolicyInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/password-policies", created);
            })
            .WithName("password-policies_create")
            .Produces<PasswordPolicyDetail>(StatusCodes.Status201Created)
            .RequirePermission("password-policies.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, PasswordPolicyInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<SysPasswordPolicy, PasswordPolicyDetail, PasswordPolicyInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("password-policies_update")
            .Produces<PasswordPolicyDetail>()
            .RequirePermission("password-policies.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<SysPasswordPolicy>(id), ct);
                return Results.NoContent();
            })
            .WithName("password-policies_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("password-policies.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<SysPasswordPolicy>(id), ct)))
            .WithName("password-policies_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("password-policies.read");
    }
}
