using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.Accounting;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class NoWaitPaymentRulesEndpoints
{
    public static void MapNoWaitPaymentRulesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "no-wait-payment-rules")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'no-wait-payment-rules'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/no-wait-payment-rules")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<NoWaitPaymentRule, NoWaitPaymentRuleListItem>(ListQueryString.Read(http)), ct)))
            .WithName("no-wait-payment-rules_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<NoWaitPaymentRuleListItem>>()
            .RequirePermission("no-wait-payment-rules.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<NoWaitPaymentRule, NoWaitPaymentRuleDetail>(id), ct)))
            .WithName("no-wait-payment-rules_get")
            .Produces<NoWaitPaymentRuleDetail>()
            .RequirePermission("no-wait-payment-rules.read");

        group.MapPost("/", async (HttpRequest http, NoWaitPaymentRuleInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<NoWaitPaymentRule, NoWaitPaymentRuleDetail, NoWaitPaymentRuleInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/no-wait-payment-rules", created);
            })
            .WithName("no-wait-payment-rules_create")
            .Produces<NoWaitPaymentRuleDetail>(StatusCodes.Status201Created)
            .RequirePermission("no-wait-payment-rules.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, NoWaitPaymentRuleInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<NoWaitPaymentRule, NoWaitPaymentRuleDetail, NoWaitPaymentRuleInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("no-wait-payment-rules_update")
            .Produces<NoWaitPaymentRuleDetail>()
            .RequirePermission("no-wait-payment-rules.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<NoWaitPaymentRule>(id), ct);
                return Results.NoContent();
            })
            .WithName("no-wait-payment-rules_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("no-wait-payment-rules.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<NoWaitPaymentRule>(id), ct)))
            .WithName("no-wait-payment-rules_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("no-wait-payment-rules.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<NoWaitPaymentRule, NoWaitPaymentRuleListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("no-wait-payment-rules_export")
            .RequirePermission("no-wait-payment-rules.export");
    }
}
