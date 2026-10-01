using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Doctors;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DoctorEducationsEndpoints
{
    public static void MapDoctorEducationsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "doctor-educations")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'doctor-educations'. Add one under Ida.Application/Features/Doctors/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/doctor-educations")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DoctorEducation, DoctorEducationRow>(ListQueryString.Read(http)), ct)))
            .WithName("doctor-educations_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<DoctorEducationRow>>()
            .RequirePermission("doctor-educations.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DoctorEducation, DoctorEducationDetail>(id), ct)))
            .WithName("doctor-educations_get")
            .Produces<DoctorEducationDetail>()
            .RequirePermission("doctor-educations.read");

        group.MapPost("/", async (HttpRequest http, DoctorEducationInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DoctorEducation, DoctorEducationDetail, DoctorEducationInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/doctor-educations", created);
            })
            .WithName("doctor-educations_create")
            .Produces<DoctorEducationDetail>(StatusCodes.Status201Created)
            .RequirePermission("doctor-educations.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, DoctorEducationInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DoctorEducation, DoctorEducationDetail, DoctorEducationInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("doctor-educations_update")
            .Produces<DoctorEducationDetail>()
            .RequirePermission("doctor-educations.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DoctorEducation>(id), ct);
                return Results.NoContent();
            })
            .WithName("doctor-educations_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("doctor-educations.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DoctorEducation>(id), ct)))
            .WithName("doctor-educations_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("doctor-educations.read");
    }
}
