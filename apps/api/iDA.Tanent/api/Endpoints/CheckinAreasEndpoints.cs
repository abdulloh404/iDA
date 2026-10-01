using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.SystemSettings;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class CheckinAreasEndpoints
{
    public static void MapCheckinAreasEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "checkin-areas")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'checkin-areas'.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/checkin-areas")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<SysCheckinArea, CheckinAreaDetail>(ListQueryString.Read(http)), ct)))
            .WithName("checkin-areas_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<CheckinAreaDetail>>()
            .RequirePermission("checkin-areas.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<SysCheckinArea, CheckinAreaDetail>(id), ct)))
            .WithName("checkin-areas_get")
            .Produces<CheckinAreaDetail>()
            .RequirePermission("checkin-areas.read");

        group.MapPost("/", async (HttpRequest http, CheckinAreaInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<SysCheckinArea, CheckinAreaDetail, CheckinAreaInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/checkin-areas", created);
            })
            .WithName("checkin-areas_create")
            .Produces<CheckinAreaDetail>(StatusCodes.Status201Created)
            .RequirePermission("checkin-areas.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, CheckinAreaInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<SysCheckinArea, CheckinAreaDetail, CheckinAreaInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("checkin-areas_update")
            .Produces<CheckinAreaDetail>()
            .RequirePermission("checkin-areas.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<SysCheckinArea>(id), ct);
                return Results.NoContent();
            })
            .WithName("checkin-areas_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("checkin-areas.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<SysCheckinArea>(id), ct)))
            .WithName("checkin-areas_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("checkin-areas.read");
    }
}
