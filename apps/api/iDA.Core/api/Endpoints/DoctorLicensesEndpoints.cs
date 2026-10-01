using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Doctors;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DoctorLicensesEndpoints
{
    public static void MapDoctorLicensesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "doctor-licenses")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'doctor-licenses'. Add one under Ida.Application/Features/Doctors/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/doctor-licenses")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DoctorLicense, DoctorLicenseRow>(ListQueryString.Read(http)), ct)))
            .WithName("doctor-licenses_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<DoctorLicenseRow>>()
            .RequirePermission("doctor-licenses.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DoctorLicense, DoctorLicenseDetail>(id), ct)))
            .WithName("doctor-licenses_get")
            .Produces<DoctorLicenseDetail>()
            .RequirePermission("doctor-licenses.read");

        group.MapPost("/", async (HttpRequest http, DoctorLicenseInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DoctorLicense, DoctorLicenseDetail, DoctorLicenseInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/doctor-licenses", created);
            })
            .WithName("doctor-licenses_create")
            .Produces<DoctorLicenseDetail>(StatusCodes.Status201Created)
            .RequirePermission("doctor-licenses.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, DoctorLicenseInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DoctorLicense, DoctorLicenseDetail, DoctorLicenseInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("doctor-licenses_update")
            .Produces<DoctorLicenseDetail>()
            .RequirePermission("doctor-licenses.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DoctorLicense>(id), ct);
                return Results.NoContent();
            })
            .WithName("doctor-licenses_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("doctor-licenses.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DoctorLicense>(id), ct)))
            .WithName("doctor-licenses_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("doctor-licenses.read");
    }
}
