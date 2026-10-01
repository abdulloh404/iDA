using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Doctors;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DoctorSpecialtiesEndpoints
{
    public static void MapDoctorSpecialtiesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "doctor-specialties")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'doctor-specialties'. Add one under Ida.Application/Features/Doctors/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/doctor-specialties")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DoctorSpecialty, DoctorSpecialtyRow>(ListQueryString.Read(http)), ct)))
            .WithName("doctor-specialties_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<DoctorSpecialtyRow>>()
            .RequirePermission("doctor-specialties.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DoctorSpecialty, DoctorSpecialtyDetail>(id), ct)))
            .WithName("doctor-specialties_get")
            .Produces<DoctorSpecialtyDetail>()
            .RequirePermission("doctor-specialties.read");

        group.MapPost("/", async (HttpRequest http, DoctorSpecialtyInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DoctorSpecialty, DoctorSpecialtyDetail, DoctorSpecialtyInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/doctor-specialties", created);
            })
            .WithName("doctor-specialties_create")
            .Produces<DoctorSpecialtyDetail>(StatusCodes.Status201Created)
            .RequirePermission("doctor-specialties.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, DoctorSpecialtyInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DoctorSpecialty, DoctorSpecialtyDetail, DoctorSpecialtyInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("doctor-specialties_update")
            .Produces<DoctorSpecialtyDetail>()
            .RequirePermission("doctor-specialties.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DoctorSpecialty>(id), ct);
                return Results.NoContent();
            })
            .WithName("doctor-specialties_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("doctor-specialties.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DoctorSpecialty>(id), ct)))
            .WithName("doctor-specialties_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("doctor-specialties.read");
    }
}
