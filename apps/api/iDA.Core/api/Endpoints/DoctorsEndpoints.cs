using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Doctors;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DoctorsEndpoints
{
    public static void MapDoctorsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "doctors")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'doctors'. Add one under Ida.Application/Features/Doctors/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/doctors")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<Doctor, DoctorListItem>(ListQueryString.Read(http)), ct)))
            .WithName("doctors_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<DoctorListItem>>()
            .RequirePermission("doctors.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<Doctor, DoctorDetail>(id), ct)))
            .WithName("doctors_get")
            .Produces<DoctorDetail>()
            .RequirePermission("doctors.read");

        group.MapPost("/", async (HttpRequest http, DoctorInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<Doctor, DoctorDetail, DoctorInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/doctors", created);
            })
            .WithName("doctors_create")
            .Produces<DoctorDetail>(StatusCodes.Status201Created)
            .RequirePermission("doctors.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, DoctorInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<Doctor, DoctorDetail, DoctorInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("doctors_update")
            .Produces<DoctorDetail>()
            .RequirePermission("doctors.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<Doctor>(id), ct);
                return Results.NoContent();
            })
            .WithName("doctors_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("doctors.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<Doctor>(id), ct)))
            .WithName("doctors_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("doctors.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<Doctor>(ListQueryString.Read(http)), ct)))
            .WithName("doctors_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<Doctor, DoctorListItem, DoctorDetail, DoctorInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("doctors.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<Doctor, DoctorListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("doctors_export")
            .RequirePermission("doctors.export");
    }
}
