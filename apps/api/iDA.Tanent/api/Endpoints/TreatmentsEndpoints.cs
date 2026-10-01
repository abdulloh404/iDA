using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.Accounting;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class TreatmentsEndpoints
{
    public static void MapTreatmentsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "treatments")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'treatments'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/treatments")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<MstTreatment, TreatmentListItem>(ListQueryString.Read(http)), ct)))
            .WithName("treatments_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<TreatmentListItem>>()
            .RequirePermission("treatments.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<MstTreatment, TreatmentDetail>(id), ct)))
            .WithName("treatments_get")
            .Produces<TreatmentDetail>()
            .RequirePermission("treatments.read");

        group.MapPost("/", async (HttpRequest http, TreatmentInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<MstTreatment, TreatmentDetail, TreatmentInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/treatments", created);
            })
            .WithName("treatments_create")
            .Produces<TreatmentDetail>(StatusCodes.Status201Created)
            .RequirePermission("treatments.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, TreatmentInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<MstTreatment, TreatmentDetail, TreatmentInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("treatments_update")
            .Produces<TreatmentDetail>()
            .RequirePermission("treatments.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<MstTreatment>(id), ct);
                return Results.NoContent();
            })
            .WithName("treatments_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("treatments.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<MstTreatment>(id), ct)))
            .WithName("treatments_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("treatments.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<MstTreatment>(ListQueryString.Read(http)), ct)))
            .WithName("treatments_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<MstTreatment, TreatmentListItem, TreatmentDetail, TreatmentInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("treatments.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<MstTreatment, TreatmentListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("treatments_export")
            .RequirePermission("treatments.export");
    }
}
