using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.General;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class TitlesEndpoints
{
    public static void MapTitlesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "titles")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'titles'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/titles")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<MstTitle, TitleListItem>(ListQueryString.Read(http)), ct)))
            .WithName("titles_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<TitleListItem>>()
            .RequirePermission("titles.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<MstTitle, TitleDetail>(id), ct)))
            .WithName("titles_get")
            .Produces<TitleDetail>()
            .RequirePermission("titles.read");

        group.MapPost("/", async (HttpRequest http, TitleInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<MstTitle, TitleDetail, TitleInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/titles", created);
            })
            .WithName("titles_create")
            .Produces<TitleDetail>(StatusCodes.Status201Created)
            .RequirePermission("titles.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, TitleInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<MstTitle, TitleDetail, TitleInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("titles_update")
            .Produces<TitleDetail>()
            .RequirePermission("titles.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<MstTitle>(id), ct);
                return Results.NoContent();
            })
            .WithName("titles_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("titles.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<MstTitle>(id), ct)))
            .WithName("titles_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("titles.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<MstTitle>(ListQueryString.Read(http)), ct)))
            .WithName("titles_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<MstTitle, TitleListItem, TitleDetail, TitleInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("titles.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<MstTitle, TitleListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("titles_export")
            .RequirePermission("titles.export");
    }
}
