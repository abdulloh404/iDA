using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.SystemSettings;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class IncomeDocSettingsEndpoints
{
    public static void MapIncomeDocSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "income-doc-settings")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'income-doc-settings'.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/income-doc-settings")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<SysIncomeDocSetting, IncomeDocSettingDetail>(ListQueryString.Read(http)), ct)))
            .WithName("income-doc-settings_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<IncomeDocSettingDetail>>()
            .RequirePermission("income-doc-settings.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<SysIncomeDocSetting, IncomeDocSettingDetail>(id), ct)))
            .WithName("income-doc-settings_get")
            .Produces<IncomeDocSettingDetail>()
            .RequirePermission("income-doc-settings.read");

        group.MapPost("/", async (HttpRequest http, IncomeDocSettingInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<SysIncomeDocSetting, IncomeDocSettingDetail, IncomeDocSettingInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/income-doc-settings", created);
            })
            .WithName("income-doc-settings_create")
            .Produces<IncomeDocSettingDetail>(StatusCodes.Status201Created)
            .RequirePermission("income-doc-settings.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, IncomeDocSettingInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<SysIncomeDocSetting, IncomeDocSettingDetail, IncomeDocSettingInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("income-doc-settings_update")
            .Produces<IncomeDocSettingDetail>()
            .RequirePermission("income-doc-settings.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<SysIncomeDocSetting>(id), ct);
                return Results.NoContent();
            })
            .WithName("income-doc-settings_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("income-doc-settings.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<SysIncomeDocSetting>(id), ct)))
            .WithName("income-doc-settings_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("income-doc-settings.read");
    }
}
