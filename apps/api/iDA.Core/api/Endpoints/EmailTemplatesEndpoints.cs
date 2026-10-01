using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.SystemSettings;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class EmailTemplatesEndpoints
{
    public static void MapEmailTemplatesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "email-templates")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'email-templates'.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/email-templates")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<SysEmailTemplate, EmailTemplateListItem>(ListQueryString.Read(http)), ct)))
            .WithName("email-templates_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<EmailTemplateListItem>>()
            .RequirePermission("email-templates.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<SysEmailTemplate, EmailTemplateDetail>(id), ct)))
            .WithName("email-templates_get")
            .Produces<EmailTemplateDetail>()
            .RequirePermission("email-templates.read");

        group.MapPost("/", async (HttpRequest http, EmailTemplateInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<SysEmailTemplate, EmailTemplateDetail, EmailTemplateInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/email-templates", created);
            })
            .WithName("email-templates_create")
            .Produces<EmailTemplateDetail>(StatusCodes.Status201Created)
            .RequirePermission("email-templates.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, EmailTemplateInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<SysEmailTemplate, EmailTemplateDetail, EmailTemplateInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("email-templates_update")
            .Produces<EmailTemplateDetail>()
            .RequirePermission("email-templates.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<SysEmailTemplate>(id), ct);
                return Results.NoContent();
            })
            .WithName("email-templates_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("email-templates.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<SysEmailTemplate>(id), ct)))
            .WithName("email-templates_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("email-templates.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<SysEmailTemplate, EmailTemplateListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("email-templates_export")
            .RequirePermission("email-templates.export");
    }
}
