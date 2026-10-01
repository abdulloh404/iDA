using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.General;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class ClinicsEndpoints
{
    public static void MapClinicsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "clinics")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'clinics'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/clinics")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<MstClinic, ClinicListItem>(ListQueryString.Read(http)), ct)))
            .WithName("clinics_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<ClinicListItem>>()
            .RequirePermission("clinics.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<MstClinic, ClinicDetail>(id), ct)))
            .WithName("clinics_get")
            .Produces<ClinicDetail>()
            .RequirePermission("clinics.read");

        group.MapPost("/", async (HttpRequest http, ClinicInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<MstClinic, ClinicDetail, ClinicInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/clinics", created);
            })
            .WithName("clinics_create")
            .Produces<ClinicDetail>(StatusCodes.Status201Created)
            .RequirePermission("clinics.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, ClinicInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<MstClinic, ClinicDetail, ClinicInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("clinics_update")
            .Produces<ClinicDetail>()
            .RequirePermission("clinics.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<MstClinic>(id), ct);
                return Results.NoContent();
            })
            .WithName("clinics_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("clinics.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<MstClinic>(id), ct)))
            .WithName("clinics_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("clinics.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<MstClinic>(ListQueryString.Read(http)), ct)))
            .WithName("clinics_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<MstClinic, ClinicListItem, ClinicDetail, ClinicInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("clinics.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<MstClinic, ClinicListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("clinics_export")
            .RequirePermission("clinics.export");
    }
}
