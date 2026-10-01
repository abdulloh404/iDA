using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class PatientRightsEndpoints
{
    public static void MapPatientRightsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "patient-rights")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'patient-rights'. Add one under Ida.Application/Features/ShareRates/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/patient-rights")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<MstPatientRight, MasterListItem>(ListQueryString.Read(http)), ct)))
            .WithName("patient-rights_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<MasterListItem>>()
            .RequirePermission("patient-rights.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<MstPatientRight, MasterDetail>(id), ct)))
            .WithName("patient-rights_get")
            .Produces<MasterDetail>()
            .RequirePermission("patient-rights.read");

        group.MapPost("/", async (HttpRequest http, MasterInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<MstPatientRight, MasterDetail, MasterInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/patient-rights", created);
            })
            .WithName("patient-rights_create")
            .Produces<MasterDetail>(StatusCodes.Status201Created)
            .RequirePermission("patient-rights.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, MasterInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<MstPatientRight, MasterDetail, MasterInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("patient-rights_update")
            .Produces<MasterDetail>()
            .RequirePermission("patient-rights.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<MstPatientRight>(id), ct);
                return Results.NoContent();
            })
            .WithName("patient-rights_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("patient-rights.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<MstPatientRight>(id), ct)))
            .WithName("patient-rights_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("patient-rights.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<MstPatientRight>(ListQueryString.Read(http)), ct)))
            .WithName("patient-rights_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<MstPatientRight, MasterListItem, MasterDetail, MasterInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("patient-rights.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<MstPatientRight, MasterListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("patient-rights_export")
            .RequirePermission("patient-rights.export");
    }
}
