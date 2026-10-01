using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.DutyRates;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DutyRatesEndpoints
{
    public static void MapDutyRatesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "duty-rates")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'duty-rates'. Add one under Ida.Application/Features/DutyRates/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/duty-rates")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DutyRate, DutyRateListItem>(ListQueryString.Read(http)), ct)))
            .WithName("duty-rates_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<DutyRateListItem>>()
            .RequirePermission("duty-rates.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DutyRate, DutyRateDetail>(id), ct)))
            .WithName("duty-rates_get")
            .Produces<DutyRateDetail>()
            .RequirePermission("duty-rates.read");

        group.MapPost("/", async (HttpRequest http, DutyRateInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DutyRate, DutyRateDetail, DutyRateInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/duty-rates", created);
            })
            .WithName("duty-rates_create")
            .Produces<DutyRateDetail>(StatusCodes.Status201Created)
            .RequirePermission("duty-rates.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, DutyRateInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DutyRate, DutyRateDetail, DutyRateInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("duty-rates_update")
            .Produces<DutyRateDetail>()
            .RequirePermission("duty-rates.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DutyRate>(id), ct);
                return Results.NoContent();
            })
            .WithName("duty-rates_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("duty-rates.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DutyRate>(id), ct)))
            .WithName("duty-rates_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("duty-rates.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<DutyRate, DutyRateListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("duty-rates_export")
            .RequirePermission("duty-rates.export");
    }
}
