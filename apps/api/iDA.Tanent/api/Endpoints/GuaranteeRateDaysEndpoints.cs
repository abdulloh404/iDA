using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.DutyRates;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class GuaranteeRateDaysEndpoints
{
    public static void MapGuaranteeRateDaysEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "guarantee-rate-days")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'guarantee-rate-days'. Add one under Ida.Application/Features/DutyRates/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/guarantee-rate-days")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<GuaranteeRateDay, GuaranteeDayRow>(ListQueryString.Read(http)), ct)))
            .WithName("guarantee-rate-days_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<GuaranteeDayRow>>()
            .RequirePermission("guarantee-rate-days.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<GuaranteeRateDay, GuaranteeDayDetail>(id), ct)))
            .WithName("guarantee-rate-days_get")
            .Produces<GuaranteeDayDetail>()
            .RequirePermission("guarantee-rate-days.read");

        group.MapPost("/", async (HttpRequest http, GuaranteeDayInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<GuaranteeRateDay, GuaranteeDayDetail, GuaranteeDayInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/guarantee-rate-days", created);
            })
            .WithName("guarantee-rate-days_create")
            .Produces<GuaranteeDayDetail>(StatusCodes.Status201Created)
            .RequirePermission("guarantee-rate-days.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, GuaranteeDayInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<GuaranteeRateDay, GuaranteeDayDetail, GuaranteeDayInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("guarantee-rate-days_update")
            .Produces<GuaranteeDayDetail>()
            .RequirePermission("guarantee-rate-days.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<GuaranteeRateDay>(id), ct);
                return Results.NoContent();
            })
            .WithName("guarantee-rate-days_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("guarantee-rate-days.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<GuaranteeRateDay>(id), ct)))
            .WithName("guarantee-rate-days_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("guarantee-rate-days.read");
    }
}
