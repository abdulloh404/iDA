using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Doctors;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DoctorTrainingsEndpoints
{
    public static void MapDoctorTrainingsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "doctor-trainings")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'doctor-trainings'. Add one under Ida.Application/Features/Doctors/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/doctor-trainings")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DoctorTraining, DoctorTrainingRow>(ListQueryString.Read(http)), ct)))
            .WithName("doctor-trainings_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<DoctorTrainingRow>>()
            .RequirePermission("doctor-trainings.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DoctorTraining, DoctorTrainingDetail>(id), ct)))
            .WithName("doctor-trainings_get")
            .Produces<DoctorTrainingDetail>()
            .RequirePermission("doctor-trainings.read");

        group.MapPost("/", async (HttpRequest http, DoctorTrainingInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DoctorTraining, DoctorTrainingDetail, DoctorTrainingInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/doctor-trainings", created);
            })
            .WithName("doctor-trainings_create")
            .Produces<DoctorTrainingDetail>(StatusCodes.Status201Created)
            .RequirePermission("doctor-trainings.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, DoctorTrainingInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DoctorTraining, DoctorTrainingDetail, DoctorTrainingInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("doctor-trainings_update")
            .Produces<DoctorTrainingDetail>()
            .RequirePermission("doctor-trainings.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DoctorTraining>(id), ct);
                return Results.NoContent();
            })
            .WithName("doctor-trainings_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("doctor-trainings.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DoctorTraining>(id), ct)))
            .WithName("doctor-trainings_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("doctor-trainings.read");
    }
}
