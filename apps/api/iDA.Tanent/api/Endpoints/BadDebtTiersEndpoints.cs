using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.DoctorFee406;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class BadDebtTiersEndpoints
{
    public static void MapBadDebtTiersEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "bad-debt-tiers")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'bad-debt-tiers'.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/bad-debt-tiers")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DfBadDebtTier, BadDebtTierListItem>(ListQueryString.Read(http)), ct)))
            .WithName("bad-debt-tiers_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<BadDebtTierListItem>>()
            .RequirePermission("bad-debt-tiers.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DfBadDebtTier, BadDebtTierDetail>(id), ct)))
            .WithName("bad-debt-tiers_get")
            .Produces<BadDebtTierDetail>()
            .RequirePermission("bad-debt-tiers.read");

        group.MapPost("/", async (HttpRequest http, BadDebtTierInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DfBadDebtTier, BadDebtTierDetail, BadDebtTierInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/bad-debt-tiers", created);
            })
            .WithName("bad-debt-tiers_create")
            .Produces<BadDebtTierDetail>(StatusCodes.Status201Created)
            .RequirePermission("bad-debt-tiers.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, BadDebtTierInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DfBadDebtTier, BadDebtTierDetail, BadDebtTierInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("bad-debt-tiers_update")
            .Produces<BadDebtTierDetail>()
            .RequirePermission("bad-debt-tiers.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DfBadDebtTier>(id), ct);
                return Results.NoContent();
            })
            .WithName("bad-debt-tiers_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("bad-debt-tiers.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DfBadDebtTier>(id), ct)))
            .WithName("bad-debt-tiers_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("bad-debt-tiers.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<DfBadDebtTier, BadDebtTierListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("bad-debt-tiers_export")
            .RequirePermission("bad-debt-tiers.export");
    }
}
