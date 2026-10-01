using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.DutyRates;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class HolidayDutyRatesEndpoints
{
    public static void MapHolidayDutyRatesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "holiday-duty-rates")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'holiday-duty-rates'. Add one under Ida.Application/Features/DutyRates/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/holiday-duty-rates")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DutyHolidayRate, HolidayRateListItem>(ListQueryString.Read(http)), ct)))
            .WithName("holiday-duty-rates_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<HolidayRateListItem>>()
            .RequirePermission("holiday-duty-rates.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DutyHolidayRate, HolidayRateDetail>(id), ct)))
            .WithName("holiday-duty-rates_get")
            .Produces<HolidayRateDetail>()
            .RequirePermission("holiday-duty-rates.read");

        group.MapPost("/", async (HttpRequest http, HolidayRateInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DutyHolidayRate, HolidayRateDetail, HolidayRateInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/holiday-duty-rates", created);
            })
            .WithName("holiday-duty-rates_create")
            .Produces<HolidayRateDetail>(StatusCodes.Status201Created)
            .RequirePermission("holiday-duty-rates.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, HolidayRateInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DutyHolidayRate, HolidayRateDetail, HolidayRateInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("holiday-duty-rates_update")
            .Produces<HolidayRateDetail>()
            .RequirePermission("holiday-duty-rates.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DutyHolidayRate>(id), ct);
                return Results.NoContent();
            })
            .WithName("holiday-duty-rates_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("holiday-duty-rates.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DutyHolidayRate>(id), ct)))
            .WithName("holiday-duty-rates_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("holiday-duty-rates.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<DutyHolidayRate, HolidayRateListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("holiday-duty-rates_export")
            .RequirePermission("holiday-duty-rates.export");
    }
}
