using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.General;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DoctorGroupsEndpoints
{
    public static void MapDoctorGroupsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "doctor-groups")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'doctor-groups'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/doctor-groups")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<MstDoctorGroup, DoctorGroupListItem>(ListQueryString.Read(http)), ct)))
            .WithName("doctor-groups_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<DoctorGroupListItem>>()
            .RequirePermission("doctor-groups.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<MstDoctorGroup, DoctorGroupDetail>(id), ct)))
            .WithName("doctor-groups_get")
            .Produces<DoctorGroupDetail>()
            .RequirePermission("doctor-groups.read");

        group.MapPost("/", async (HttpRequest http, DoctorGroupInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<MstDoctorGroup, DoctorGroupDetail, DoctorGroupInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/doctor-groups", created);
            })
            .WithName("doctor-groups_create")
            .Produces<DoctorGroupDetail>(StatusCodes.Status201Created)
            .RequirePermission("doctor-groups.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, DoctorGroupInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<MstDoctorGroup, DoctorGroupDetail, DoctorGroupInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("doctor-groups_update")
            .Produces<DoctorGroupDetail>()
            .RequirePermission("doctor-groups.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<MstDoctorGroup>(id), ct);
                return Results.NoContent();
            })
            .WithName("doctor-groups_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("doctor-groups.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<MstDoctorGroup>(id), ct)))
            .WithName("doctor-groups_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("doctor-groups.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<MstDoctorGroup>(ListQueryString.Read(http)), ct)))
            .WithName("doctor-groups_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<MstDoctorGroup, DoctorGroupListItem, DoctorGroupDetail, DoctorGroupInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("doctor-groups.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<MstDoctorGroup, DoctorGroupListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("doctor-groups_export")
            .RequirePermission("doctor-groups.export");
    }
}
