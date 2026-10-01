using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.Tax402;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class TaxAllowanceTypesEndpoints
{
    public static void MapTaxAllowanceTypesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "tax-allowance-types")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'tax-allowance-types'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/tax-allowance-types")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<TaxAllowanceType, TaxAllowanceTypeListItem>(ListQueryString.Read(http)), ct)))
            .WithName("tax-allowance-types_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<TaxAllowanceTypeListItem>>()
            .RequirePermission("tax-allowance-types.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<TaxAllowanceType, TaxAllowanceTypeDetail>(id), ct)))
            .WithName("tax-allowance-types_get")
            .Produces<TaxAllowanceTypeDetail>()
            .RequirePermission("tax-allowance-types.read");

        group.MapPost("/", async (HttpRequest http, TaxAllowanceTypeInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<TaxAllowanceType, TaxAllowanceTypeDetail, TaxAllowanceTypeInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/tax-allowance-types", created);
            })
            .WithName("tax-allowance-types_create")
            .Produces<TaxAllowanceTypeDetail>(StatusCodes.Status201Created)
            .RequirePermission("tax-allowance-types.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, TaxAllowanceTypeInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<TaxAllowanceType, TaxAllowanceTypeDetail, TaxAllowanceTypeInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("tax-allowance-types_update")
            .Produces<TaxAllowanceTypeDetail>()
            .RequirePermission("tax-allowance-types.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<TaxAllowanceType>(id), ct);
                return Results.NoContent();
            })
            .WithName("tax-allowance-types_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("tax-allowance-types.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<TaxAllowanceType>(id), ct)))
            .WithName("tax-allowance-types_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("tax-allowance-types.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<TaxAllowanceType, TaxAllowanceTypeListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("tax-allowance-types_export")
            .RequirePermission("tax-allowance-types.export");
    }
}
