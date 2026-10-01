using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.SystemSettings;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class HisDoctorCodeMapsEndpoints
{
    public static void MapHisDoctorCodeMapsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "his-doctor-code-maps")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'his-doctor-code-maps'.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/his-doctor-code-maps")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<SysHisDoctorCodeMap, HisDoctorCodeMapListItem>(ListQueryString.Read(http)), ct)))
            .WithName("his-doctor-code-maps_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<HisDoctorCodeMapListItem>>()
            .RequirePermission("his-doctor-code-maps.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<SysHisDoctorCodeMap, HisDoctorCodeMapDetail>(id), ct)))
            .WithName("his-doctor-code-maps_get")
            .Produces<HisDoctorCodeMapDetail>()
            .RequirePermission("his-doctor-code-maps.read");

        group.MapPost("/", async (HttpRequest http, HisDoctorCodeMapInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<SysHisDoctorCodeMap, HisDoctorCodeMapDetail, HisDoctorCodeMapInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/his-doctor-code-maps", created);
            })
            .WithName("his-doctor-code-maps_create")
            .Produces<HisDoctorCodeMapDetail>(StatusCodes.Status201Created)
            .RequirePermission("his-doctor-code-maps.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, HisDoctorCodeMapInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<SysHisDoctorCodeMap, HisDoctorCodeMapDetail, HisDoctorCodeMapInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("his-doctor-code-maps_update")
            .Produces<HisDoctorCodeMapDetail>()
            .RequirePermission("his-doctor-code-maps.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<SysHisDoctorCodeMap>(id), ct);
                return Results.NoContent();
            })
            .WithName("his-doctor-code-maps_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("his-doctor-code-maps.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<SysHisDoctorCodeMap>(id), ct)))
            .WithName("his-doctor-code-maps_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("his-doctor-code-maps.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<SysHisDoctorCodeMap, HisDoctorCodeMapListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("his-doctor-code-maps_export")
            .RequirePermission("his-doctor-code-maps.export");
    }
}
