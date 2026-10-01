using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.Tax406;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class InvoiceArCashRulesEndpoints
{
    public static void MapInvoiceArCashRulesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "invoice-ar-cash-rules")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'invoice-ar-cash-rules'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/invoice-ar-cash-rules")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<InvoiceArCashRule, InvoiceArCashRuleListItem>(ListQueryString.Read(http)), ct)))
            .WithName("invoice-ar-cash-rules_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<InvoiceArCashRuleListItem>>()
            .RequirePermission("invoice-ar-cash-rules.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<InvoiceArCashRule, InvoiceArCashRuleDetail>(id), ct)))
            .WithName("invoice-ar-cash-rules_get")
            .Produces<InvoiceArCashRuleDetail>()
            .RequirePermission("invoice-ar-cash-rules.read");

        group.MapPost("/", async (HttpRequest http, InvoiceArCashRuleInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<InvoiceArCashRule, InvoiceArCashRuleDetail, InvoiceArCashRuleInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/invoice-ar-cash-rules", created);
            })
            .WithName("invoice-ar-cash-rules_create")
            .Produces<InvoiceArCashRuleDetail>(StatusCodes.Status201Created)
            .RequirePermission("invoice-ar-cash-rules.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, InvoiceArCashRuleInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<InvoiceArCashRule, InvoiceArCashRuleDetail, InvoiceArCashRuleInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("invoice-ar-cash-rules_update")
            .Produces<InvoiceArCashRuleDetail>()
            .RequirePermission("invoice-ar-cash-rules.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<InvoiceArCashRule>(id), ct);
                return Results.NoContent();
            })
            .WithName("invoice-ar-cash-rules_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("invoice-ar-cash-rules.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<InvoiceArCashRule>(id), ct)))
            .WithName("invoice-ar-cash-rules_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("invoice-ar-cash-rules.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<InvoiceArCashRule, InvoiceArCashRuleListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("invoice-ar-cash-rules_export")
            .RequirePermission("invoice-ar-cash-rules.export");
    }
}
