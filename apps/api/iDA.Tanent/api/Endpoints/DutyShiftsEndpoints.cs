using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.DutySchedules;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DutyShiftsEndpoints
{
    public static void MapDutyShiftsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "duty-shifts")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'duty-shifts'.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/duty-shifts")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DutyShift, DutyShiftRow>(ListQueryString.Read(http)), ct)))
            .WithName("duty-shifts_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<DutyShiftRow>>()
            .RequirePermission("duty-shifts.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DutyShift, DutyShiftDetail>(id), ct)))
            .WithName("duty-shifts_get")
            .Produces<DutyShiftDetail>()
            .RequirePermission("duty-shifts.read");

        group.MapPost("/", async (HttpRequest http, DutyShiftInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DutyShift, DutyShiftDetail, DutyShiftInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/duty-shifts", created);
            })
            .WithName("duty-shifts_create")
            .Produces<DutyShiftDetail>(StatusCodes.Status201Created)
            .RequirePermission("duty-shifts.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, DutyShiftInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DutyShift, DutyShiftDetail, DutyShiftInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("duty-shifts_update")
            .Produces<DutyShiftDetail>()
            .RequirePermission("duty-shifts.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DutyShift>(id), ct);
                return Results.NoContent();
            })
            .WithName("duty-shifts_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("duty-shifts.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DutyShift>(id), ct)))
            .WithName("duty-shifts_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("duty-shifts.read");
    }
}
