using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.DutyRates;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class GuaranteeRateTreatmentsEndpoints
{
    public static void MapGuaranteeRateTreatmentsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "guarantee-rate-treatments")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'guarantee-rate-treatments'. Add one under Ida.Application/Features/DutyRates/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/guarantee-rate-treatments")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<GuaranteeRateTreatment, GuaranteeTreatmentRow>(ListQueryString.Read(http)), ct)))
            .WithName("guarantee-rate-treatments_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<GuaranteeTreatmentRow>>()
            .RequirePermission("guarantee-rate-treatments.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<GuaranteeRateTreatment, GuaranteeTreatmentDetail>(id), ct)))
            .WithName("guarantee-rate-treatments_get")
            .Produces<GuaranteeTreatmentDetail>()
            .RequirePermission("guarantee-rate-treatments.read");

        group.MapPost("/", async (HttpRequest http, GuaranteeTreatmentInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<GuaranteeRateTreatment, GuaranteeTreatmentDetail, GuaranteeTreatmentInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/guarantee-rate-treatments", created);
            })
            .WithName("guarantee-rate-treatments_create")
            .Produces<GuaranteeTreatmentDetail>(StatusCodes.Status201Created)
            .RequirePermission("guarantee-rate-treatments.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, GuaranteeTreatmentInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<GuaranteeRateTreatment, GuaranteeTreatmentDetail, GuaranteeTreatmentInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("guarantee-rate-treatments_update")
            .Produces<GuaranteeTreatmentDetail>()
            .RequirePermission("guarantee-rate-treatments.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<GuaranteeRateTreatment>(id), ct);
                return Results.NoContent();
            })
            .WithName("guarantee-rate-treatments_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("guarantee-rate-treatments.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<GuaranteeRateTreatment>(id), ct)))
            .WithName("guarantee-rate-treatments_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("guarantee-rate-treatments.read");
    }
}
