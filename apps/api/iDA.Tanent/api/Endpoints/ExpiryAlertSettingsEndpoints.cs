using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.SystemSettings;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class ExpiryAlertSettingsEndpoints
{
    public static void MapExpiryAlertSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "expiry-alert-settings")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'expiry-alert-settings'.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/expiry-alert-settings")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<SysExpiryAlertSetting, ExpiryAlertSettingDetail>(ListQueryString.Read(http)), ct)))
            .WithName("expiry-alert-settings_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<ExpiryAlertSettingDetail>>()
            .RequirePermission("expiry-alert-settings.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<SysExpiryAlertSetting, ExpiryAlertSettingDetail>(id), ct)))
            .WithName("expiry-alert-settings_get")
            .Produces<ExpiryAlertSettingDetail>()
            .RequirePermission("expiry-alert-settings.read");

        group.MapPost("/", async (HttpRequest http, ExpiryAlertSettingInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<SysExpiryAlertSetting, ExpiryAlertSettingDetail, ExpiryAlertSettingInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/expiry-alert-settings", created);
            })
            .WithName("expiry-alert-settings_create")
            .Produces<ExpiryAlertSettingDetail>(StatusCodes.Status201Created)
            .RequirePermission("expiry-alert-settings.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, ExpiryAlertSettingInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<SysExpiryAlertSetting, ExpiryAlertSettingDetail, ExpiryAlertSettingInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("expiry-alert-settings_update")
            .Produces<ExpiryAlertSettingDetail>()
            .RequirePermission("expiry-alert-settings.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<SysExpiryAlertSetting>(id), ct);
                return Results.NoContent();
            })
            .WithName("expiry-alert-settings_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("expiry-alert-settings.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<SysExpiryAlertSetting>(id), ct)))
            .WithName("expiry-alert-settings_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("expiry-alert-settings.read");
    }
}
