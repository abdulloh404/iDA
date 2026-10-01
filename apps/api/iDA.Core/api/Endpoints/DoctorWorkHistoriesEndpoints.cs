using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Doctors;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DoctorWorkHistoriesEndpoints
{
    public static void MapDoctorWorkHistoriesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "doctor-work-histories")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'doctor-work-histories'. Add one under Ida.Application/Features/Doctors/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/doctor-work-histories")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DoctorWorkHistory, DoctorWorkHistoryRow>(ListQueryString.Read(http)), ct)))
            .WithName("doctor-work-histories_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<DoctorWorkHistoryRow>>()
            .RequirePermission("doctor-work-histories.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DoctorWorkHistory, DoctorWorkHistoryDetail>(id), ct)))
            .WithName("doctor-work-histories_get")
            .Produces<DoctorWorkHistoryDetail>()
            .RequirePermission("doctor-work-histories.read");

        group.MapPost("/", async (HttpRequest http, DoctorWorkHistoryInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DoctorWorkHistory, DoctorWorkHistoryDetail, DoctorWorkHistoryInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/doctor-work-histories", created);
            })
            .WithName("doctor-work-histories_create")
            .Produces<DoctorWorkHistoryDetail>(StatusCodes.Status201Created)
            .RequirePermission("doctor-work-histories.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, DoctorWorkHistoryInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DoctorWorkHistory, DoctorWorkHistoryDetail, DoctorWorkHistoryInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("doctor-work-histories_update")
            .Produces<DoctorWorkHistoryDetail>()
            .RequirePermission("doctor-work-histories.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DoctorWorkHistory>(id), ct);
                return Results.NoContent();
            })
            .WithName("doctor-work-histories_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("doctor-work-histories.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DoctorWorkHistory>(id), ct)))
            .WithName("doctor-work-histories_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("doctor-work-histories.read");
    }
}
