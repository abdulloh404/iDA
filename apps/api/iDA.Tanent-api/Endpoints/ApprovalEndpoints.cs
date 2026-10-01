using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Features.Approvals;
using MediatR;

namespace Ida.Api.Endpoints;

public static class ApprovalEndpoints
{
    public static void MapApprovalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/approvals").WithTags("คำขอและการอนุมัติ");

        group.MapGet("/mine", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListApprovalRequestsQuery(ApprovalScope.Mine, ListQueryString.Read(http)), ct)))
            .WithName("approvals_mine")
            .WithDescription(Filters)
            .Produces<PagedResult<ApprovalRequestListItem>>()
            .RequirePermission("approvals.read");

        group.MapGet("/pending", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListApprovalRequestsQuery(ApprovalScope.Pending, ListQueryString.Read(http)), ct)))
            .WithName("approvals_pending")
            .WithDescription(Filters)
            .Produces<PagedResult<ApprovalRequestListItem>>()
            .RequirePermission("approvals.approve");

        group.MapGet("/history", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListApprovalRequestsQuery(ApprovalScope.History, ListQueryString.Read(http)), ct)))
            .WithName("approvals_history")
            .WithDescription(Filters)
            .Produces<PagedResult<ApprovalRequestListItem>>()
            .RequirePermission("approvals.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetApprovalRequestQuery(id), ct)))
            .WithName("approvals_get")
            .Produces<ApprovalRequestDetail>()
            .RequirePermission("approvals.read");

        group.MapPost("/{id:guid}/decide", async (Guid id, DecideApprovalInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new DecideApprovalCommand(id, input.Action, input.Comment), ct)))
            .WithName("approvals_decide")
            .WithDescription("APPROVE | REJECT | RETURN — สองอย่างหลังต้องมีเหตุผล")
            .Produces<ApprovalRequestDetail>()
            .RequirePermission("approvals.approve");

        group.MapGet("/request-types", () =>
                Results.Ok(ApprovalLabels.RequestTypes
                    .Select(kv => new { code = kv.Key, nameTh = kv.Value })))
            .WithName("approvals_request_types")
            .RequirePermission("approvals.read");
    }

    private const string Filters =
        "ตัวกรอง: page, pageSize, sort (requestNo|requestType|requestedBy|requestedAt|updatedAt|status), " +
        "q, requestType, status, requestedBy, requestedFrom, requestedTo";
}

public record DecideApprovalInput(string Action, string? Comment);

