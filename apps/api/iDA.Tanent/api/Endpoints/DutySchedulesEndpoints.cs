using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.DutySchedules;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DutySchedulesEndpoints
{
    public static void MapDutySchedulesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "duty-schedules")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'duty-schedules'.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/duty-schedules")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DutySchedule, DutyScheduleListItem>(ListQueryString.Read(http)), ct)))
            .WithName("duty-schedules_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<DutyScheduleListItem>>()
            .RequirePermission("duty-schedules.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DutySchedule, DutyScheduleDetail>(id), ct)))
            .WithName("duty-schedules_get")
            .Produces<DutyScheduleDetail>()
            .RequirePermission("duty-schedules.read");

        group.MapPost("/", async (HttpRequest http, DutyScheduleInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DutySchedule, DutyScheduleDetail, DutyScheduleInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/duty-schedules", created);
            })
            .WithName("duty-schedules_create")
            .Produces<DutyScheduleDetail>(StatusCodes.Status201Created)
            .RequirePermission("duty-schedules.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, DutyScheduleInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DutySchedule, DutyScheduleDetail, DutyScheduleInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("duty-schedules_update")
            .Produces<DutyScheduleDetail>()
            .RequirePermission("duty-schedules.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DutySchedule>(id), ct);
                return Results.NoContent();
            })
            .WithName("duty-schedules_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("duty-schedules.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DutySchedule>(id), ct)))
            .WithName("duty-schedules_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("duty-schedules.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<DutySchedule, DutyScheduleListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("duty-schedules_export")
            .RequirePermission("duty-schedules.export");
    }
}
