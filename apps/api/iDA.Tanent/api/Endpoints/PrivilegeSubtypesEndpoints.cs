using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.General;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class PrivilegeSubtypesEndpoints
{
    public static void MapPrivilegeSubtypesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "privilege-subtypes")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'privilege-subtypes'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/privilege-subtypes")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<MstPrivilegeSubtype, PrivilegeSubtypeListItem>(ListQueryString.Read(http)), ct)))
            .WithName("privilege-subtypes_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<PrivilegeSubtypeListItem>>()
            .RequirePermission("privilege-subtypes.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<MstPrivilegeSubtype, PrivilegeSubtypeDetail>(id), ct)))
            .WithName("privilege-subtypes_get")
            .Produces<PrivilegeSubtypeDetail>()
            .RequirePermission("privilege-subtypes.read");

        group.MapPost("/", async (HttpRequest http, PrivilegeSubtypeInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<MstPrivilegeSubtype, PrivilegeSubtypeDetail, PrivilegeSubtypeInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/privilege-subtypes", created);
            })
            .WithName("privilege-subtypes_create")
            .Produces<PrivilegeSubtypeDetail>(StatusCodes.Status201Created)
            .RequirePermission("privilege-subtypes.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, PrivilegeSubtypeInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<MstPrivilegeSubtype, PrivilegeSubtypeDetail, PrivilegeSubtypeInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("privilege-subtypes_update")
            .Produces<PrivilegeSubtypeDetail>()
            .RequirePermission("privilege-subtypes.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<MstPrivilegeSubtype>(id), ct);
                return Results.NoContent();
            })
            .WithName("privilege-subtypes_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("privilege-subtypes.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<MstPrivilegeSubtype>(id), ct)))
            .WithName("privilege-subtypes_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("privilege-subtypes.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<MstPrivilegeSubtype>(ListQueryString.Read(http)), ct)))
            .WithName("privilege-subtypes_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<MstPrivilegeSubtype, PrivilegeSubtypeListItem, PrivilegeSubtypeDetail, PrivilegeSubtypeInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("privilege-subtypes.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<MstPrivilegeSubtype, PrivilegeSubtypeListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("privilege-subtypes_export")
            .RequirePermission("privilege-subtypes.export");
    }
}
