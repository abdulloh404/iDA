using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.Tax402;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class IncomeTypes402Endpoints
{
    public static void MapIncomeTypes402Endpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "income-types-402")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'income-types-402'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/income-types-402")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<MstIncomeType402, IncomeType402ListItem>(ListQueryString.Read(http)), ct)))
            .WithName("income-types-402_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<IncomeType402ListItem>>()
            .RequirePermission("income-types-402.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<MstIncomeType402, IncomeType402Detail>(id), ct)))
            .WithName("income-types-402_get")
            .Produces<IncomeType402Detail>()
            .RequirePermission("income-types-402.read");

        group.MapPost("/", async (HttpRequest http, IncomeType402Input input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<MstIncomeType402, IncomeType402Detail, IncomeType402Input>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/income-types-402", created);
            })
            .WithName("income-types-402_create")
            .Produces<IncomeType402Detail>(StatusCodes.Status201Created)
            .RequirePermission("income-types-402.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, IncomeType402Input input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<MstIncomeType402, IncomeType402Detail, IncomeType402Input>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("income-types-402_update")
            .Produces<IncomeType402Detail>()
            .RequirePermission("income-types-402.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<MstIncomeType402>(id), ct);
                return Results.NoContent();
            })
            .WithName("income-types-402_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("income-types-402.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<MstIncomeType402>(id), ct)))
            .WithName("income-types-402_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("income-types-402.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<MstIncomeType402>(ListQueryString.Read(http)), ct)))
            .WithName("income-types-402_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<MstIncomeType402, IncomeType402ListItem, IncomeType402Detail, IncomeType402Input>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("income-types-402.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<MstIncomeType402, IncomeType402ListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("income-types-402_export")
            .RequirePermission("income-types-402.export");
    }
}
