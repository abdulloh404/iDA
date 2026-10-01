using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.General;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class SpecialtiesEndpoints
{
    public static void MapSpecialtiesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "specialties")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'specialties'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/specialties")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<MstSpecialty, SpecialtyListItem>(ListQueryString.Read(http)), ct)))
            .WithName("specialties_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<SpecialtyListItem>>()
            .RequirePermission("specialties.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<MstSpecialty, SpecialtyDetail>(id), ct)))
            .WithName("specialties_get")
            .Produces<SpecialtyDetail>()
            .RequirePermission("specialties.read");

        group.MapPost("/", async (HttpRequest http, SpecialtyInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<MstSpecialty, SpecialtyDetail, SpecialtyInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/specialties", created);
            })
            .WithName("specialties_create")
            .Produces<SpecialtyDetail>(StatusCodes.Status201Created)
            .RequirePermission("specialties.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, SpecialtyInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<MstSpecialty, SpecialtyDetail, SpecialtyInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("specialties_update")
            .Produces<SpecialtyDetail>()
            .RequirePermission("specialties.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<MstSpecialty>(id), ct);
                return Results.NoContent();
            })
            .WithName("specialties_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("specialties.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<MstSpecialty>(id), ct)))
            .WithName("specialties_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("specialties.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<MstSpecialty>(ListQueryString.Read(http)), ct)))
            .WithName("specialties_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<MstSpecialty, SpecialtyListItem, SpecialtyDetail, SpecialtyInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("specialties.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<MstSpecialty, SpecialtyListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("specialties_export")
            .RequirePermission("specialties.export");
    }
}
