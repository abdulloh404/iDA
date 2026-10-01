using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Doctors;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DoctorCodesEndpoints
{
    public static void MapDoctorCodesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "doctor-codes")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'doctor-codes'. Add one under Ida.Application/Features/Doctors/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/doctor-codes")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DoctorCode, DoctorCodeListItem>(ListQueryString.Read(http)), ct)))
            .WithName("doctor-codes_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<DoctorCodeListItem>>()
            .RequirePermission("doctor-codes.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DoctorCode, DoctorCodeDetail>(id), ct)))
            .WithName("doctor-codes_get")
            .Produces<DoctorCodeDetail>()
            .RequirePermission("doctor-codes.read");

        group.MapPost("/", async (HttpRequest http, DoctorCodeInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DoctorCode, DoctorCodeDetail, DoctorCodeInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/doctor-codes", created);
            })
            .WithName("doctor-codes_create")
            .Produces<DoctorCodeDetail>(StatusCodes.Status201Created)
            .RequirePermission("doctor-codes.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, DoctorCodeInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DoctorCode, DoctorCodeDetail, DoctorCodeInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("doctor-codes_update")
            .Produces<DoctorCodeDetail>()
            .RequirePermission("doctor-codes.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DoctorCode>(id), ct);
                return Results.NoContent();
            })
            .WithName("doctor-codes_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("doctor-codes.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DoctorCode>(id), ct)))
            .WithName("doctor-codes_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("doctor-codes.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<DoctorCode>(ListQueryString.Read(http)), ct)))
            .WithName("doctor-codes_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<DoctorCode, DoctorCodeListItem, DoctorCodeDetail, DoctorCodeInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("doctor-codes.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<DoctorCode, DoctorCodeListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("doctor-codes_export")
            .RequirePermission("doctor-codes.export");
    }
}
