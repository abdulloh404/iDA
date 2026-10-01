using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Doctors;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DoctorAddressesEndpoints
{
    public static void MapDoctorAddressesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "doctor-addresses")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'doctor-addresses'. Add one under Ida.Application/Features/Doctors/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/doctor-addresses")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DoctorAddress, DoctorAddressRow>(ListQueryString.Read(http)), ct)))
            .WithName("doctor-addresses_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<DoctorAddressRow>>()
            .RequirePermission("doctor-addresses.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DoctorAddress, DoctorAddressDetail>(id), ct)))
            .WithName("doctor-addresses_get")
            .Produces<DoctorAddressDetail>()
            .RequirePermission("doctor-addresses.read");

        group.MapPost("/", async (HttpRequest http, DoctorAddressInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DoctorAddress, DoctorAddressDetail, DoctorAddressInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/doctor-addresses", created);
            })
            .WithName("doctor-addresses_create")
            .Produces<DoctorAddressDetail>(StatusCodes.Status201Created)
            .RequirePermission("doctor-addresses.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, DoctorAddressInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DoctorAddress, DoctorAddressDetail, DoctorAddressInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("doctor-addresses_update")
            .Produces<DoctorAddressDetail>()
            .RequirePermission("doctor-addresses.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DoctorAddress>(id), ct);
                return Results.NoContent();
            })
            .WithName("doctor-addresses_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("doctor-addresses.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DoctorAddress>(id), ct)))
            .WithName("doctor-addresses_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("doctor-addresses.read");
    }
}
