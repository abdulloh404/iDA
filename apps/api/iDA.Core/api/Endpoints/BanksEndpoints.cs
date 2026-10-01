using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.Accounting;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class BanksEndpoints
{
    public static void MapBanksEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "banks")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'banks'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/banks")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<MstBank, BankListItem>(ListQueryString.Read(http)), ct)))
            .WithName("banks_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<BankListItem>>()
            .RequirePermission("banks.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<MstBank, BankDetail>(id), ct)))
            .WithName("banks_get")
            .Produces<BankDetail>()
            .RequirePermission("banks.read");

        group.MapPost("/", async (HttpRequest http, BankInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<MstBank, BankDetail, BankInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/banks", created);
            })
            .WithName("banks_create")
            .Produces<BankDetail>(StatusCodes.Status201Created)
            .RequirePermission("banks.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, BankInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<MstBank, BankDetail, BankInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("banks_update")
            .Produces<BankDetail>()
            .RequirePermission("banks.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<MstBank>(id), ct);
                return Results.NoContent();
            })
            .WithName("banks_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("banks.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<MstBank>(id), ct)))
            .WithName("banks_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("banks.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<MstBank>(ListQueryString.Read(http)), ct)))
            .WithName("banks_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<MstBank, BankListItem, BankDetail, BankInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("banks.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<MstBank, BankListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("banks_export")
            .RequirePermission("banks.export");
    }
}
