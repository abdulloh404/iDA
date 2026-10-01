using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.Tax406;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class InvoicePrefixRulesEndpoints
{
    public static void MapInvoicePrefixRulesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "invoice-prefix-rules")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'invoice-prefix-rules'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/invoice-prefix-rules")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<InvoicePrefixRule, InvoicePrefixRuleListItem>(ListQueryString.Read(http)), ct)))
            .WithName("invoice-prefix-rules_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<InvoicePrefixRuleListItem>>()
            .RequirePermission("invoice-prefix-rules.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<InvoicePrefixRule, InvoicePrefixRuleDetail>(id), ct)))
            .WithName("invoice-prefix-rules_get")
            .Produces<InvoicePrefixRuleDetail>()
            .RequirePermission("invoice-prefix-rules.read");

        group.MapPost("/", async (HttpRequest http, InvoicePrefixRuleInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<InvoicePrefixRule, InvoicePrefixRuleDetail, InvoicePrefixRuleInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/invoice-prefix-rules", created);
            })
            .WithName("invoice-prefix-rules_create")
            .Produces<InvoicePrefixRuleDetail>(StatusCodes.Status201Created)
            .RequirePermission("invoice-prefix-rules.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, InvoicePrefixRuleInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<InvoicePrefixRule, InvoicePrefixRuleDetail, InvoicePrefixRuleInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("invoice-prefix-rules_update")
            .Produces<InvoicePrefixRuleDetail>()
            .RequirePermission("invoice-prefix-rules.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<InvoicePrefixRule>(id), ct);
                return Results.NoContent();
            })
            .WithName("invoice-prefix-rules_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("invoice-prefix-rules.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<InvoicePrefixRule>(id), ct)))
            .WithName("invoice-prefix-rules_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("invoice-prefix-rules.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<InvoicePrefixRule, InvoicePrefixRuleListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("invoice-prefix-rules_export")
            .RequirePermission("invoice-prefix-rules.export");
    }
}
