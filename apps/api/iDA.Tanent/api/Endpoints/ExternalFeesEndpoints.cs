using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.DoctorFee402;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class ExternalFeesEndpoints
{
    public static void MapExternalFeesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "external-fees")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'external-fees'.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/external-fees")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DfExternalFee, ExternalFeeListItem>(ListQueryString.Read(http)), ct)))
            .WithName("external-fees_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<ExternalFeeListItem>>()
            .RequirePermission("external-fees.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DfExternalFee, ExternalFeeDetail>(id), ct)))
            .WithName("external-fees_get")
            .Produces<ExternalFeeDetail>()
            .RequirePermission("external-fees.read");

        group.MapPost("/", async (HttpRequest http, ExternalFeeInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DfExternalFee, ExternalFeeDetail, ExternalFeeInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/external-fees", created);
            })
            .WithName("external-fees_create")
            .Produces<ExternalFeeDetail>(StatusCodes.Status201Created)
            .RequirePermission("external-fees.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, ExternalFeeInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DfExternalFee, ExternalFeeDetail, ExternalFeeInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("external-fees_update")
            .Produces<ExternalFeeDetail>()
            .RequirePermission("external-fees.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DfExternalFee>(id), ct);
                return Results.NoContent();
            })
            .WithName("external-fees_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("external-fees.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DfExternalFee>(id), ct)))
            .WithName("external-fees_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("external-fees.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<DfExternalFee, ExternalFeeListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("external-fees_export")
            .RequirePermission("external-fees.export");
    }
}
