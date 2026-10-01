using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Doctors;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DoctorWelfaresEndpoints
{
    public static void MapDoctorWelfaresEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "doctor-welfares")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'doctor-welfares'. Add one under Ida.Application/Features/Doctors/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/doctor-welfares")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DoctorWelfare, DoctorWelfareListItem>(ListQueryString.Read(http)), ct)))
            .WithName("doctor-welfares_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<DoctorWelfareListItem>>()
            .RequirePermission("doctor-welfares.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DoctorWelfare, DoctorWelfareDetail>(id), ct)))
            .WithName("doctor-welfares_get")
            .Produces<DoctorWelfareDetail>()
            .RequirePermission("doctor-welfares.read");

        group.MapPost("/", async (HttpRequest http, DoctorWelfareInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DoctorWelfare, DoctorWelfareDetail, DoctorWelfareInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/doctor-welfares", created);
            })
            .WithName("doctor-welfares_create")
            .Produces<DoctorWelfareDetail>(StatusCodes.Status201Created)
            .RequirePermission("doctor-welfares.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, DoctorWelfareInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DoctorWelfare, DoctorWelfareDetail, DoctorWelfareInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("doctor-welfares_update")
            .Produces<DoctorWelfareDetail>()
            .RequirePermission("doctor-welfares.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DoctorWelfare>(id), ct);
                return Results.NoContent();
            })
            .WithName("doctor-welfares_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("doctor-welfares.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DoctorWelfare>(id), ct)))
            .WithName("doctor-welfares_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("doctor-welfares.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<DoctorWelfare, DoctorWelfareListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("doctor-welfares_export")
            .RequirePermission("doctor-welfares.export");
    }
}
