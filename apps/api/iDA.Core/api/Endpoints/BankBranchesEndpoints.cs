using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.Accounting;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class BankBranchesEndpoints
{
    public static void MapBankBranchesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "bank-branches")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'bank-branches'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/bank-branches")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<MstBankBranch, BankBranchListItem>(ListQueryString.Read(http)), ct)))
            .WithName("bank-branches_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<BankBranchListItem>>()
            .RequirePermission("bank-branches.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<MstBankBranch, BankBranchDetail>(id), ct)))
            .WithName("bank-branches_get")
            .Produces<BankBranchDetail>()
            .RequirePermission("bank-branches.read");

        group.MapPost("/", async (HttpRequest http, BankBranchInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<MstBankBranch, BankBranchDetail, BankBranchInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/bank-branches", created);
            })
            .WithName("bank-branches_create")
            .Produces<BankBranchDetail>(StatusCodes.Status201Created)
            .RequirePermission("bank-branches.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, BankBranchInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<MstBankBranch, BankBranchDetail, BankBranchInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("bank-branches_update")
            .Produces<BankBranchDetail>()
            .RequirePermission("bank-branches.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<MstBankBranch>(id), ct);
                return Results.NoContent();
            })
            .WithName("bank-branches_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("bank-branches.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<MstBankBranch>(id), ct)))
            .WithName("bank-branches_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("bank-branches.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<MstBankBranch>(ListQueryString.Read(http)), ct)))
            .WithName("bank-branches_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<MstBankBranch, BankBranchListItem, BankBranchDetail, BankBranchInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("bank-branches.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<MstBankBranch, BankBranchListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("bank-branches_export")
            .RequirePermission("bank-branches.export");
    }
}
