using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Doctors;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DoctorProfessionalRecordsEndpoints
{
    public static void MapDoctorProfessionalRecordsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "doctor-professional-records")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'doctor-professional-records'. Add one under Ida.Application/Features/Doctors/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/doctor-professional-records")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DoctorProfessionalRecord, DoctorProfessionalRecordRow>(ListQueryString.Read(http)), ct)))
            .WithName("doctor-professional-records_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<DoctorProfessionalRecordRow>>()
            .RequirePermission("doctor-professional-records.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DoctorProfessionalRecord, DoctorProfessionalRecordDetail>(id), ct)))
            .WithName("doctor-professional-records_get")
            .Produces<DoctorProfessionalRecordDetail>()
            .RequirePermission("doctor-professional-records.read");

        group.MapPost("/", async (HttpRequest http, DoctorProfessionalRecordInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DoctorProfessionalRecord, DoctorProfessionalRecordDetail, DoctorProfessionalRecordInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/doctor-professional-records", created);
            })
            .WithName("doctor-professional-records_create")
            .Produces<DoctorProfessionalRecordDetail>(StatusCodes.Status201Created)
            .RequirePermission("doctor-professional-records.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, DoctorProfessionalRecordInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DoctorProfessionalRecord, DoctorProfessionalRecordDetail, DoctorProfessionalRecordInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("doctor-professional-records_update")
            .Produces<DoctorProfessionalRecordDetail>()
            .RequirePermission("doctor-professional-records.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DoctorProfessionalRecord>(id), ct);
                return Results.NoContent();
            })
            .WithName("doctor-professional-records_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("doctor-professional-records.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DoctorProfessionalRecord>(id), ct)))
            .WithName("doctor-professional-records_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("doctor-professional-records.read");
    }
}
