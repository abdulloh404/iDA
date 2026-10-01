using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.DutyRates;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class HolidayDutyExclusionsEndpoints
{
    public static void MapHolidayDutyExclusionsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "holiday-duty-exclusions")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'holiday-duty-exclusions'. Add one under Ida.Application/Features/DutyRates/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/holiday-duty-exclusions")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DutyHolidayExclusion, HolidayExclusionRow>(ListQueryString.Read(http)), ct)))
            .WithName("holiday-duty-exclusions_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<HolidayExclusionRow>>()
            .RequirePermission("holiday-duty-exclusions.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DutyHolidayExclusion, HolidayExclusionDetail>(id), ct)))
            .WithName("holiday-duty-exclusions_get")
            .Produces<HolidayExclusionDetail>()
            .RequirePermission("holiday-duty-exclusions.read");

        group.MapPost("/", async (HttpRequest http, HolidayExclusionInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DutyHolidayExclusion, HolidayExclusionDetail, HolidayExclusionInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/holiday-duty-exclusions", created);
            })
            .WithName("holiday-duty-exclusions_create")
            .Produces<HolidayExclusionDetail>(StatusCodes.Status201Created)
            .RequirePermission("holiday-duty-exclusions.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, HolidayExclusionInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DutyHolidayExclusion, HolidayExclusionDetail, HolidayExclusionInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("holiday-duty-exclusions_update")
            .Produces<HolidayExclusionDetail>()
            .RequirePermission("holiday-duty-exclusions.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DutyHolidayExclusion>(id), ct);
                return Results.NoContent();
            })
            .WithName("holiday-duty-exclusions_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("holiday-duty-exclusions.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DutyHolidayExclusion>(id), ct)))
            .WithName("holiday-duty-exclusions_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("holiday-duty-exclusions.read");
    }
}
