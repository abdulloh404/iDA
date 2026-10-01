using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Doctors;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DoctorInsurancesEndpoints
{
    public static void MapDoctorInsurancesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "doctor-insurances")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'doctor-insurances'. Add one under Ida.Application/Features/Doctors/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/doctor-insurances")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DoctorInsurance, DoctorInsuranceRow>(ListQueryString.Read(http)), ct)))
            .WithName("doctor-insurances_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<DoctorInsuranceRow>>()
            .RequirePermission("doctor-insurances.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DoctorInsurance, DoctorInsuranceDetail>(id), ct)))
            .WithName("doctor-insurances_get")
            .Produces<DoctorInsuranceDetail>()
            .RequirePermission("doctor-insurances.read");

        group.MapPost("/", async (HttpRequest http, DoctorInsuranceInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DoctorInsurance, DoctorInsuranceDetail, DoctorInsuranceInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/doctor-insurances", created);
            })
            .WithName("doctor-insurances_create")
            .Produces<DoctorInsuranceDetail>(StatusCodes.Status201Created)
            .RequirePermission("doctor-insurances.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, DoctorInsuranceInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DoctorInsurance, DoctorInsuranceDetail, DoctorInsuranceInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("doctor-insurances_update")
            .Produces<DoctorInsuranceDetail>()
            .RequirePermission("doctor-insurances.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DoctorInsurance>(id), ct);
                return Results.NoContent();
            })
            .WithName("doctor-insurances_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("doctor-insurances.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DoctorInsurance>(id), ct)))
            .WithName("doctor-insurances_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("doctor-insurances.read");
    }
}
