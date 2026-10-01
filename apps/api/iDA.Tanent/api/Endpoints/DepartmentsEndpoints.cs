using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.General;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DepartmentsEndpoints
{
    public static void MapDepartmentsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "departments")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'departments'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/departments")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<MstDepartment, DepartmentListItem>(ListQueryString.Read(http)), ct)))
            .WithName("departments_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<DepartmentListItem>>()
            .RequirePermission("departments.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<MstDepartment, DepartmentDetail>(id), ct)))
            .WithName("departments_get")
            .Produces<DepartmentDetail>()
            .RequirePermission("departments.read");

        group.MapPost("/", async (HttpRequest http, DepartmentInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<MstDepartment, DepartmentDetail, DepartmentInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/departments", created);
            })
            .WithName("departments_create")
            .Produces<DepartmentDetail>(StatusCodes.Status201Created)
            .RequirePermission("departments.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, DepartmentInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<MstDepartment, DepartmentDetail, DepartmentInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("departments_update")
            .Produces<DepartmentDetail>()
            .RequirePermission("departments.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<MstDepartment>(id), ct);
                return Results.NoContent();
            })
            .WithName("departments_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("departments.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<MstDepartment>(id), ct)))
            .WithName("departments_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("departments.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<MstDepartment>(ListQueryString.Read(http)), ct)))
            .WithName("departments_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<MstDepartment, DepartmentListItem, DepartmentDetail, DepartmentInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("departments.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<MstDepartment, DepartmentListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("departments_export")
            .RequirePermission("departments.export");
    }
}
