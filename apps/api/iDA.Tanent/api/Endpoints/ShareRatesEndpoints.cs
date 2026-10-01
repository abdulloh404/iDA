using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.ShareRates;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class ShareRatesEndpoints
{
    public static void MapShareRatesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "share-rates")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'share-rates'. Add one under Ida.Application/Features/ShareRates/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/share-rates")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<ShareRate, ShareRateListItem>(ListQueryString.Read(http)), ct)))
            .WithName("share-rates_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<ShareRateListItem>>()
            .RequirePermission("share-rates.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<ShareRate, ShareRateDetail>(id), ct)))
            .WithName("share-rates_get")
            .Produces<ShareRateDetail>()
            .RequirePermission("share-rates.read");

        group.MapPost("/", async (HttpRequest http, ShareRateInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<ShareRate, ShareRateDetail, ShareRateInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/share-rates", created);
            })
            .WithName("share-rates_create")
            .Produces<ShareRateDetail>(StatusCodes.Status201Created)
            .RequirePermission("share-rates.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, ShareRateInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<ShareRate, ShareRateDetail, ShareRateInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("share-rates_update")
            .Produces<ShareRateDetail>()
            .RequirePermission("share-rates.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<ShareRate>(id), ct);
                return Results.NoContent();
            })
            .WithName("share-rates_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("share-rates.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<ShareRate>(id), ct)))
            .WithName("share-rates_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("share-rates.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<ShareRate, ShareRateListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("share-rates_export")
            .RequirePermission("share-rates.export");
    }
}
