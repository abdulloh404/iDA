using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.IncomeDocuments;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class SlipSettingsEndpoints
{
    public static void MapSlipSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "slip-settings")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'slip-settings'.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/slip-settings")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DocSlipSetting, SlipSettingListItem>(ListQueryString.Read(http)), ct)))
            .WithName("slip-settings_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<SlipSettingListItem>>()
            .RequirePermission("slip-settings.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DocSlipSetting, SlipSettingDetail>(id), ct)))
            .WithName("slip-settings_get")
            .Produces<SlipSettingDetail>()
            .RequirePermission("slip-settings.read");

        group.MapPost("/", async (HttpRequest http, SlipSettingInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DocSlipSetting, SlipSettingDetail, SlipSettingInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/slip-settings", created);
            })
            .WithName("slip-settings_create")
            .Produces<SlipSettingDetail>(StatusCodes.Status201Created)
            .RequirePermission("slip-settings.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, SlipSettingInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DocSlipSetting, SlipSettingDetail, SlipSettingInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("slip-settings_update")
            .Produces<SlipSettingDetail>()
            .RequirePermission("slip-settings.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DocSlipSetting>(id), ct);
                return Results.NoContent();
            })
            .WithName("slip-settings_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("slip-settings.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DocSlipSetting>(id), ct)))
            .WithName("slip-settings_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("slip-settings.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<DocSlipSetting, SlipSettingListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("slip-settings_export")
            .RequirePermission("slip-settings.export");
    }
}
