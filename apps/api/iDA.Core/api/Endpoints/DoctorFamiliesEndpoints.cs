using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Doctors;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DoctorFamiliesEndpoints
{
    public static void MapDoctorFamiliesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "doctor-families")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'doctor-families'. Add one under Ida.Application/Features/Doctors/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/doctor-families")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DoctorFamily, DoctorFamilyRow>(ListQueryString.Read(http)), ct)))
            .WithName("doctor-families_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<DoctorFamilyRow>>()
            .RequirePermission("doctor-families.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DoctorFamily, DoctorFamilyDetail>(id), ct)))
            .WithName("doctor-families_get")
            .Produces<DoctorFamilyDetail>()
            .RequirePermission("doctor-families.read");

        group.MapPost("/", async (HttpRequest http, DoctorFamilyInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DoctorFamily, DoctorFamilyDetail, DoctorFamilyInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/doctor-families", created);
            })
            .WithName("doctor-families_create")
            .Produces<DoctorFamilyDetail>(StatusCodes.Status201Created)
            .RequirePermission("doctor-families.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, DoctorFamilyInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DoctorFamily, DoctorFamilyDetail, DoctorFamilyInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("doctor-families_update")
            .Produces<DoctorFamilyDetail>()
            .RequirePermission("doctor-families.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DoctorFamily>(id), ct);
                return Results.NoContent();
            })
            .WithName("doctor-families_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("doctor-families.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DoctorFamily>(id), ct)))
            .WithName("doctor-families_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("doctor-families.read");
    }
}
