using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.DoctorFee402;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class ExternalFeeLinesEndpoints
{
    public static void MapExternalFeeLinesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "external-fee-lines")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'external-fee-lines'.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/external-fee-lines")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DfExternalFeeLine, ExternalFeeLineRow>(ListQueryString.Read(http)), ct)))
            .WithName("external-fee-lines_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<ExternalFeeLineRow>>()
            .RequirePermission("external-fee-lines.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DfExternalFeeLine, ExternalFeeLineDetail>(id), ct)))
            .WithName("external-fee-lines_get")
            .Produces<ExternalFeeLineDetail>()
            .RequirePermission("external-fee-lines.read");

        group.MapPost("/", async (HttpRequest http, ExternalFeeLineInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DfExternalFeeLine, ExternalFeeLineDetail, ExternalFeeLineInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/external-fee-lines", created);
            })
            .WithName("external-fee-lines_create")
            .Produces<ExternalFeeLineDetail>(StatusCodes.Status201Created)
            .RequirePermission("external-fee-lines.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, ExternalFeeLineInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DfExternalFeeLine, ExternalFeeLineDetail, ExternalFeeLineInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("external-fee-lines_update")
            .Produces<ExternalFeeLineDetail>()
            .RequirePermission("external-fee-lines.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DfExternalFeeLine>(id), ct);
                return Results.NoContent();
            })
            .WithName("external-fee-lines_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("external-fee-lines.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DfExternalFeeLine>(id), ct)))
            .WithName("external-fee-lines_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("external-fee-lines.read");
    }
}
