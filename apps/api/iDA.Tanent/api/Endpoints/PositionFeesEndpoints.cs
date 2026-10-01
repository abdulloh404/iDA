using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.DoctorFee402;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class PositionFeesEndpoints
{
    public static void MapPositionFeesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "position-fees")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'position-fees'.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/position-fees")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DfPositionFee, PositionFeeListItem>(ListQueryString.Read(http)), ct)))
            .WithName("position-fees_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<PositionFeeListItem>>()
            .RequirePermission("position-fees.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DfPositionFee, PositionFeeDetail>(id), ct)))
            .WithName("position-fees_get")
            .Produces<PositionFeeDetail>()
            .RequirePermission("position-fees.read");

        group.MapPost("/", async (HttpRequest http, PositionFeeInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DfPositionFee, PositionFeeDetail, PositionFeeInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/position-fees", created);
            })
            .WithName("position-fees_create")
            .Produces<PositionFeeDetail>(StatusCodes.Status201Created)
            .RequirePermission("position-fees.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, PositionFeeInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DfPositionFee, PositionFeeDetail, PositionFeeInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("position-fees_update")
            .Produces<PositionFeeDetail>()
            .RequirePermission("position-fees.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DfPositionFee>(id), ct);
                return Results.NoContent();
            })
            .WithName("position-fees_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("position-fees.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DfPositionFee>(id), ct)))
            .WithName("position-fees_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("position-fees.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<DfPositionFee, PositionFeeListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("position-fees_export")
            .RequirePermission("position-fees.export");
    }
}
