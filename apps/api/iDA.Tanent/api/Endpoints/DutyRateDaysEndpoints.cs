using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.DutyRates;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DutyRateDaysEndpoints
{
    public static void MapDutyRateDaysEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "duty-rate-days")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'duty-rate-days'. Add one under Ida.Application/Features/DutyRates/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/duty-rate-days")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DutyRateDay, DutyRateDayRow>(ListQueryString.Read(http)), ct)))
            .WithName("duty-rate-days_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<DutyRateDayRow>>()
            .RequirePermission("duty-rate-days.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DutyRateDay, DutyRateDayDetail>(id), ct)))
            .WithName("duty-rate-days_get")
            .Produces<DutyRateDayDetail>()
            .RequirePermission("duty-rate-days.read");

        group.MapPost("/", async (HttpRequest http, DutyRateDayInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DutyRateDay, DutyRateDayDetail, DutyRateDayInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/duty-rate-days", created);
            })
            .WithName("duty-rate-days_create")
            .Produces<DutyRateDayDetail>(StatusCodes.Status201Created)
            .RequirePermission("duty-rate-days.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, DutyRateDayInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DutyRateDay, DutyRateDayDetail, DutyRateDayInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("duty-rate-days_update")
            .Produces<DutyRateDayDetail>()
            .RequirePermission("duty-rate-days.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DutyRateDay>(id), ct);
                return Results.NoContent();
            })
            .WithName("duty-rate-days_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("duty-rate-days.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DutyRateDay>(id), ct)))
            .WithName("duty-rate-days_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("duty-rate-days.read");
    }
}
