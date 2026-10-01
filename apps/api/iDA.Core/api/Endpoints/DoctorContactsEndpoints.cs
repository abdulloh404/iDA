using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Doctors;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DoctorContactsEndpoints
{
    public static void MapDoctorContactsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "doctor-contacts")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'doctor-contacts'. Add one under Ida.Application/Features/Doctors/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/doctor-contacts")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DoctorContact, DoctorContactRow>(ListQueryString.Read(http)), ct)))
            .WithName("doctor-contacts_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<DoctorContactRow>>()
            .RequirePermission("doctor-contacts.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DoctorContact, DoctorContactDetail>(id), ct)))
            .WithName("doctor-contacts_get")
            .Produces<DoctorContactDetail>()
            .RequirePermission("doctor-contacts.read");

        group.MapPost("/", async (HttpRequest http, DoctorContactInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DoctorContact, DoctorContactDetail, DoctorContactInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/doctor-contacts", created);
            })
            .WithName("doctor-contacts_create")
            .Produces<DoctorContactDetail>(StatusCodes.Status201Created)
            .RequirePermission("doctor-contacts.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, DoctorContactInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DoctorContact, DoctorContactDetail, DoctorContactInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("doctor-contacts_update")
            .Produces<DoctorContactDetail>()
            .RequirePermission("doctor-contacts.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DoctorContact>(id), ct);
                return Results.NoContent();
            })
            .WithName("doctor-contacts_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("doctor-contacts.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DoctorContact>(id), ct)))
            .WithName("doctor-contacts_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("doctor-contacts.read");
    }
}
