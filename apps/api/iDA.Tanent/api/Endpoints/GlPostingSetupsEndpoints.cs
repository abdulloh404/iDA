using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.Accounting;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class GlPostingSetupsEndpoints
{
    public static void MapGlPostingSetupsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "gl-posting-setups")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'gl-posting-setups'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/gl-posting-setups")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<GlPostingSetup, GlPostingSetupListItem>(ListQueryString.Read(http)), ct)))
            .WithName("gl-posting-setups_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<GlPostingSetupListItem>>()
            .RequirePermission("gl-posting-setups.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<GlPostingSetup, GlPostingSetupDetail>(id), ct)))
            .WithName("gl-posting-setups_get")
            .Produces<GlPostingSetupDetail>()
            .RequirePermission("gl-posting-setups.read");

        group.MapPost("/", async (HttpRequest http, GlPostingSetupInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<GlPostingSetup, GlPostingSetupDetail, GlPostingSetupInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/gl-posting-setups", created);
            })
            .WithName("gl-posting-setups_create")
            .Produces<GlPostingSetupDetail>(StatusCodes.Status201Created)
            .RequirePermission("gl-posting-setups.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, GlPostingSetupInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<GlPostingSetup, GlPostingSetupDetail, GlPostingSetupInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("gl-posting-setups_update")
            .Produces<GlPostingSetupDetail>()
            .RequirePermission("gl-posting-setups.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<GlPostingSetup>(id), ct);
                return Results.NoContent();
            })
            .WithName("gl-posting-setups_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("gl-posting-setups.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<GlPostingSetup>(id), ct)))
            .WithName("gl-posting-setups_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("gl-posting-setups.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<GlPostingSetup, GlPostingSetupListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("gl-posting-setups_export")
            .RequirePermission("gl-posting-setups.export");
    }
}
