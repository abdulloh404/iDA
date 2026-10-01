using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.DutySchedules;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DutyShiftDoctorsEndpoints
{
    public static void MapDutyShiftDoctorsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "duty-shift-doctors")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'duty-shift-doctors'.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/duty-shift-doctors")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DutyShiftDoctor, DutyShiftDoctorRow>(ListQueryString.Read(http)), ct)))
            .WithName("duty-shift-doctors_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<DutyShiftDoctorRow>>()
            .RequirePermission("duty-shift-doctors.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DutyShiftDoctor, DutyShiftDoctorDetail>(id), ct)))
            .WithName("duty-shift-doctors_get")
            .Produces<DutyShiftDoctorDetail>()
            .RequirePermission("duty-shift-doctors.read");

        group.MapPost("/", async (HttpRequest http, DutyShiftDoctorInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DutyShiftDoctor, DutyShiftDoctorDetail, DutyShiftDoctorInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/duty-shift-doctors", created);
            })
            .WithName("duty-shift-doctors_create")
            .Produces<DutyShiftDoctorDetail>(StatusCodes.Status201Created)
            .RequirePermission("duty-shift-doctors.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, DutyShiftDoctorInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DutyShiftDoctor, DutyShiftDoctorDetail, DutyShiftDoctorInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("duty-shift-doctors_update")
            .Produces<DutyShiftDoctorDetail>()
            .RequirePermission("duty-shift-doctors.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DutyShiftDoctor>(id), ct);
                return Results.NoContent();
            })
            .WithName("duty-shift-doctors_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("duty-shift-doctors.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DutyShiftDoctor>(id), ct)))
            .WithName("duty-shift-doctors_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("duty-shift-doctors.read");
    }
}
