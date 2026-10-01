using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.General;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class SubSpecialtiesEndpoints
{
    public static void MapSubSpecialtiesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "sub-specialties")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'sub-specialties'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/sub-specialties")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<MstSubSpecialty, SubSpecialtyListItem>(ListQueryString.Read(http)), ct)))
            .WithName("sub-specialties_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<SubSpecialtyListItem>>()
            .RequirePermission("sub-specialties.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<MstSubSpecialty, SubSpecialtyDetail>(id), ct)))
            .WithName("sub-specialties_get")
            .Produces<SubSpecialtyDetail>()
            .RequirePermission("sub-specialties.read");

        group.MapPost("/", async (HttpRequest http, SubSpecialtyInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<MstSubSpecialty, SubSpecialtyDetail, SubSpecialtyInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/sub-specialties", created);
            })
            .WithName("sub-specialties_create")
            .Produces<SubSpecialtyDetail>(StatusCodes.Status201Created)
            .RequirePermission("sub-specialties.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, SubSpecialtyInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<MstSubSpecialty, SubSpecialtyDetail, SubSpecialtyInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("sub-specialties_update")
            .Produces<SubSpecialtyDetail>()
            .RequirePermission("sub-specialties.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<MstSubSpecialty>(id), ct);
                return Results.NoContent();
            })
            .WithName("sub-specialties_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("sub-specialties.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<MstSubSpecialty>(id), ct)))
            .WithName("sub-specialties_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("sub-specialties.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<MstSubSpecialty>(ListQueryString.Read(http)), ct)))
            .WithName("sub-specialties_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<MstSubSpecialty, SubSpecialtyListItem, SubSpecialtyDetail, SubSpecialtyInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("sub-specialties.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<MstSubSpecialty, SubSpecialtyListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("sub-specialties_export")
            .RequirePermission("sub-specialties.export");
    }
}
