using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.SystemSettings;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class HisNotifyEmailsEndpoints
{
    public static void MapHisNotifyEmailsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "his-notify-emails")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'his-notify-emails'.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/his-notify-emails")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<SysHisNotifyEmail, HisNotifyEmailListItem>(ListQueryString.Read(http)), ct)))
            .WithName("his-notify-emails_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<HisNotifyEmailListItem>>()
            .RequirePermission("his-notify-emails.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<SysHisNotifyEmail, HisNotifyEmailDetail>(id), ct)))
            .WithName("his-notify-emails_get")
            .Produces<HisNotifyEmailDetail>()
            .RequirePermission("his-notify-emails.read");

        group.MapPost("/", async (HttpRequest http, HisNotifyEmailInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<SysHisNotifyEmail, HisNotifyEmailDetail, HisNotifyEmailInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/his-notify-emails", created);
            })
            .WithName("his-notify-emails_create")
            .Produces<HisNotifyEmailDetail>(StatusCodes.Status201Created)
            .RequirePermission("his-notify-emails.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, HisNotifyEmailInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<SysHisNotifyEmail, HisNotifyEmailDetail, HisNotifyEmailInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("his-notify-emails_update")
            .Produces<HisNotifyEmailDetail>()
            .RequirePermission("his-notify-emails.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<SysHisNotifyEmail>(id), ct);
                return Results.NoContent();
            })
            .WithName("his-notify-emails_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("his-notify-emails.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<SysHisNotifyEmail>(id), ct)))
            .WithName("his-notify-emails_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("his-notify-emails.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<SysHisNotifyEmail, HisNotifyEmailListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("his-notify-emails_export")
            .RequirePermission("his-notify-emails.export");
    }
}
