using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Doctors;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DoctorHospitalLinksEndpoints
{
    public static void MapDoctorHospitalLinksEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "doctor-hospital-links")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'doctor-hospital-links'. Add one under Ida.Application/Features/Doctors/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/doctor-hospital-links")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DoctorHospitalLink, DoctorHospitalLinkRow>(ListQueryString.Read(http)), ct)))
            .WithName("doctor-hospital-links_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<DoctorHospitalLinkRow>>()
            .RequirePermission("doctor-hospital-links.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DoctorHospitalLink, DoctorHospitalLinkDetail>(id), ct)))
            .WithName("doctor-hospital-links_get")
            .Produces<DoctorHospitalLinkDetail>()
            .RequirePermission("doctor-hospital-links.read");

        group.MapPost("/", async (HttpRequest http, DoctorHospitalLinkInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DoctorHospitalLink, DoctorHospitalLinkDetail, DoctorHospitalLinkInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/doctor-hospital-links", created);
            })
            .WithName("doctor-hospital-links_create")
            .Produces<DoctorHospitalLinkDetail>(StatusCodes.Status201Created)
            .RequirePermission("doctor-hospital-links.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, DoctorHospitalLinkInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DoctorHospitalLink, DoctorHospitalLinkDetail, DoctorHospitalLinkInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("doctor-hospital-links_update")
            .Produces<DoctorHospitalLinkDetail>()
            .RequirePermission("doctor-hospital-links.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DoctorHospitalLink>(id), ct);
                return Results.NoContent();
            })
            .WithName("doctor-hospital-links_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("doctor-hospital-links.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DoctorHospitalLink>(id), ct)))
            .WithName("doctor-hospital-links_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("doctor-hospital-links.read");
    }
}
