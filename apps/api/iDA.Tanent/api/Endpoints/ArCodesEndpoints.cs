using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.Accounting;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class ArCodesEndpoints
{
    public static void MapArCodesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "ar-codes")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'ar-codes'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/ar-codes")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<MstArCode, ArCodeListItem>(ListQueryString.Read(http)), ct)))
            .WithName("ar-codes_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<ArCodeListItem>>()
            .RequirePermission("ar-codes.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<MstArCode, ArCodeDetail>(id), ct)))
            .WithName("ar-codes_get")
            .Produces<ArCodeDetail>()
            .RequirePermission("ar-codes.read");

        group.MapPost("/", async (HttpRequest http, ArCodeInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<MstArCode, ArCodeDetail, ArCodeInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/ar-codes", created);
            })
            .WithName("ar-codes_create")
            .Produces<ArCodeDetail>(StatusCodes.Status201Created)
            .RequirePermission("ar-codes.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, ArCodeInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<MstArCode, ArCodeDetail, ArCodeInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("ar-codes_update")
            .Produces<ArCodeDetail>()
            .RequirePermission("ar-codes.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<MstArCode>(id), ct);
                return Results.NoContent();
            })
            .WithName("ar-codes_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("ar-codes.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<MstArCode>(id), ct)))
            .WithName("ar-codes_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("ar-codes.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<MstArCode>(ListQueryString.Read(http)), ct)))
            .WithName("ar-codes_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<MstArCode, ArCodeListItem, ArCodeDetail, ArCodeInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("ar-codes.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<MstArCode, ArCodeListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("ar-codes_export")
            .RequirePermission("ar-codes.export");
    }
}
