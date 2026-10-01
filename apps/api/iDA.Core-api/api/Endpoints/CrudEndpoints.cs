using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Common;
using MediatR;

namespace Ida.Api.Endpoints;

public static class CrudEndpoints
{
    public static RouteGroupBuilder MapCrud<TEntity, TList, TDetail, TInput>(
        this IEndpointRouteBuilder app, CrudResource resource)
        where TEntity : class, IEntity, new()
    {
        var group = app.MapGroup($"/api/master-data/{resource.Name}")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<TEntity, TList>(ListQueryString.Read(http)), ct)))
            .WithName($"{resource.Name}_list")
            .WithDescription(DescribeFilters(resource))
            .Produces<PagedResult<TList>>()
            .RequirePermission(resource.Permission("read"));

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<TEntity, TDetail>(id), ct)))
            .WithName($"{resource.Name}_get")
            .Produces<TDetail>()
            .RequirePermission(resource.Permission("read"));

        group.MapPost("/", async (TInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<TEntity, TDetail, TInput>(input), ct);
                return Results.Created($"/api/master-data/{resource.Name}", created);
            })
            .WithName($"{resource.Name}_create")
            .Produces<TDetail>(StatusCodes.Status201Created)
            .RequirePermission(resource.Permission("write"));

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, TInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<TEntity, TDetail, TInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName($"{resource.Name}_update")
            .Produces<TDetail>()
            .RequirePermission(resource.Permission("write"));

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<TEntity>(id), ct);
                return Results.NoContent();
            })
            .WithName($"{resource.Name}_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission(resource.Permission("delete"));

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<TEntity>(id), ct)))
            .WithName($"{resource.Name}_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission(resource.Permission("read"));

        if (resource.SupportsLookup)
        {
            group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new LookupQuery<TEntity>(ListQueryString.Read(http)), ct)))
                .WithName($"{resource.Name}_lookup")
                .WithDescription(
                    "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                    $"สูงสุด {CrudLookupHandler<TEntity, TList, TDetail, TInput>.MaxOptions} รายการ")
                .Produces<IReadOnlyList<LookupItem>>()
                .RequirePermission(resource.Permission("read"));
        }

        if (resource.SupportsExport)
        {
            group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                {
                    var file = await mediator.Send(
                        new ExportQuery<TEntity, TList>(ListQueryString.Read(http)), ct);
                    return Results.File(file.Content, ExportFile.ContentType, file.FileName);
                })
                .WithName($"{resource.Name}_export")
                .RequirePermission(resource.Permission("export"));
        }

        return group;
    }

    private static string DescribeFilters(CrudResource resource)
    {
        var common = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        return resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {common}"
            : $"ตัวกรอง: {common}, {string.Join(", ", resource.FilterKeys)}";
    }
}

