using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Doctors;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DoctorAffiliationsEndpoints
{
    public static void MapDoctorAffiliationsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "doctor-affiliations")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'doctor-affiliations'. Add one under Ida.Application/Features/Doctors/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/doctor-affiliations")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DoctorAffiliation, DoctorAffiliationRow>(ListQueryString.Read(http)), ct)))
            .WithName("doctor-affiliations_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<DoctorAffiliationRow>>()
            .RequirePermission("doctor-affiliations.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DoctorAffiliation, DoctorAffiliationDetail>(id), ct)))
            .WithName("doctor-affiliations_get")
            .Produces<DoctorAffiliationDetail>()
            .RequirePermission("doctor-affiliations.read");

        group.MapPost("/", async (HttpRequest http, DoctorAffiliationInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DoctorAffiliation, DoctorAffiliationDetail, DoctorAffiliationInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/doctor-affiliations", created);
            })
            .WithName("doctor-affiliations_create")
            .Produces<DoctorAffiliationDetail>(StatusCodes.Status201Created)
            .RequirePermission("doctor-affiliations.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, DoctorAffiliationInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DoctorAffiliation, DoctorAffiliationDetail, DoctorAffiliationInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("doctor-affiliations_update")
            .Produces<DoctorAffiliationDetail>()
            .RequirePermission("doctor-affiliations.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DoctorAffiliation>(id), ct);
                return Results.NoContent();
            })
            .WithName("doctor-affiliations_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("doctor-affiliations.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DoctorAffiliation>(id), ct)))
            .WithName("doctor-affiliations_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("doctor-affiliations.read");
    }
}
