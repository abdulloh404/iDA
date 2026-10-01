using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.DutyRates;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class GuaranteeRatesEndpoints
{
    public static void MapGuaranteeRatesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "guarantee-rates")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'guarantee-rates'. Add one under Ida.Application/Features/DutyRates/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/guarantee-rates")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<GuaranteeRate, GuaranteeRateListItem>(ListQueryString.Read(http)), ct)))
            .WithName("guarantee-rates_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<GuaranteeRateListItem>>()
            .RequirePermission("guarantee-rates.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<GuaranteeRate, GuaranteeRateDetail>(id), ct)))
            .WithName("guarantee-rates_get")
            .Produces<GuaranteeRateDetail>()
            .RequirePermission("guarantee-rates.read");

        group.MapPost("/", async (HttpRequest http, GuaranteeRateInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<GuaranteeRate, GuaranteeRateDetail, GuaranteeRateInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/guarantee-rates", created);
            })
            .WithName("guarantee-rates_create")
            .Produces<GuaranteeRateDetail>(StatusCodes.Status201Created)
            .RequirePermission("guarantee-rates.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, GuaranteeRateInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<GuaranteeRate, GuaranteeRateDetail, GuaranteeRateInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("guarantee-rates_update")
            .Produces<GuaranteeRateDetail>()
            .RequirePermission("guarantee-rates.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<GuaranteeRate>(id), ct);
                return Results.NoContent();
            })
            .WithName("guarantee-rates_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("guarantee-rates.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<GuaranteeRate>(id), ct)))
            .WithName("guarantee-rates_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("guarantee-rates.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<GuaranteeRate, GuaranteeRateListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("guarantee-rates_export")
            .RequirePermission("guarantee-rates.export");
    }
}
