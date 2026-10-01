using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Doctors;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class WelfarePlansEndpoints
{
    public static void MapWelfarePlansEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "welfare-plans")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'welfare-plans'. Add one under Ida.Application/Features/Doctors/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/welfare-plans")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<MstWelfarePlan, WelfarePlanListItem>(ListQueryString.Read(http)), ct)))
            .WithName("welfare-plans_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<WelfarePlanListItem>>()
            .RequirePermission("welfare-plans.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<MstWelfarePlan, WelfarePlanDetail>(id), ct)))
            .WithName("welfare-plans_get")
            .Produces<WelfarePlanDetail>()
            .RequirePermission("welfare-plans.read");

        group.MapPost("/", async (HttpRequest http, WelfarePlanInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<MstWelfarePlan, WelfarePlanDetail, WelfarePlanInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/welfare-plans", created);
            })
            .WithName("welfare-plans_create")
            .Produces<WelfarePlanDetail>(StatusCodes.Status201Created)
            .RequirePermission("welfare-plans.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, WelfarePlanInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<MstWelfarePlan, WelfarePlanDetail, WelfarePlanInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("welfare-plans_update")
            .Produces<WelfarePlanDetail>()
            .RequirePermission("welfare-plans.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<MstWelfarePlan>(id), ct);
                return Results.NoContent();
            })
            .WithName("welfare-plans_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("welfare-plans.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<MstWelfarePlan>(id), ct)))
            .WithName("welfare-plans_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("welfare-plans.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<MstWelfarePlan>(ListQueryString.Read(http)), ct)))
            .WithName("welfare-plans_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<MstWelfarePlan, WelfarePlanListItem, WelfarePlanDetail, WelfarePlanInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("welfare-plans.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<MstWelfarePlan, WelfarePlanListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("welfare-plans_export")
            .RequirePermission("welfare-plans.export");
    }
}
