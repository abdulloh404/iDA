using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.ShareRates;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class ShareRateExclusionsEndpoints
{
    public static void MapShareRateExclusionsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "share-rate-exclusions")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'share-rate-exclusions'. Add one under Ida.Application/Features/ShareRates/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/share-rate-exclusions")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<ShareRateExclusion, ShareExclusionRow>(ListQueryString.Read(http)), ct)))
            .WithName("share-rate-exclusions_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<ShareExclusionRow>>()
            .RequirePermission("share-rate-exclusions.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<ShareRateExclusion, ShareExclusionDetail>(id), ct)))
            .WithName("share-rate-exclusions_get")
            .Produces<ShareExclusionDetail>()
            .RequirePermission("share-rate-exclusions.read");

        group.MapPost("/", async (HttpRequest http, ShareExclusionInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<ShareRateExclusion, ShareExclusionDetail, ShareExclusionInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/share-rate-exclusions", created);
            })
            .WithName("share-rate-exclusions_create")
            .Produces<ShareExclusionDetail>(StatusCodes.Status201Created)
            .RequirePermission("share-rate-exclusions.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, ShareExclusionInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<ShareRateExclusion, ShareExclusionDetail, ShareExclusionInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("share-rate-exclusions_update")
            .Produces<ShareExclusionDetail>()
            .RequirePermission("share-rate-exclusions.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<ShareRateExclusion>(id), ct);
                return Results.NoContent();
            })
            .WithName("share-rate-exclusions_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("share-rate-exclusions.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<ShareRateExclusion>(id), ct)))
            .WithName("share-rate-exclusions_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("share-rate-exclusions.read");
    }
}
