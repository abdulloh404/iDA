using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.Tax402;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class PitTaxBracketsEndpoints
{
    public static void MapPitTaxBracketsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "pit-tax-brackets")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'pit-tax-brackets'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/pit-tax-brackets")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<PitTaxBracket, PitTaxBracketListItem>(ListQueryString.Read(http)), ct)))
            .WithName("pit-tax-brackets_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<PitTaxBracketListItem>>()
            .RequirePermission("pit-tax-brackets.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<PitTaxBracket, PitTaxBracketDetail>(id), ct)))
            .WithName("pit-tax-brackets_get")
            .Produces<PitTaxBracketDetail>()
            .RequirePermission("pit-tax-brackets.read");

        group.MapPost("/", async (HttpRequest http, PitTaxBracketInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<PitTaxBracket, PitTaxBracketDetail, PitTaxBracketInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/pit-tax-brackets", created);
            })
            .WithName("pit-tax-brackets_create")
            .Produces<PitTaxBracketDetail>(StatusCodes.Status201Created)
            .RequirePermission("pit-tax-brackets.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, PitTaxBracketInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<PitTaxBracket, PitTaxBracketDetail, PitTaxBracketInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("pit-tax-brackets_update")
            .Produces<PitTaxBracketDetail>()
            .RequirePermission("pit-tax-brackets.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<PitTaxBracket>(id), ct);
                return Results.NoContent();
            })
            .WithName("pit-tax-brackets_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("pit-tax-brackets.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<PitTaxBracket>(id), ct)))
            .WithName("pit-tax-brackets_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("pit-tax-brackets.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<PitTaxBracket, PitTaxBracketListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("pit-tax-brackets_export")
            .RequirePermission("pit-tax-brackets.export");
    }
}
