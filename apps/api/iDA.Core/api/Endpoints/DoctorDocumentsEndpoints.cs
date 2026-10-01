using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Doctors;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DoctorDocumentsEndpoints
{
    public static void MapDoctorDocumentsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "doctor-documents")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'doctor-documents'. Add one under Ida.Application/Features/Doctors/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/doctor-documents")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DoctorDocument, DoctorDocumentRow>(ListQueryString.Read(http)), ct)))
            .WithName("doctor-documents_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<DoctorDocumentRow>>()
            .RequirePermission("doctor-documents.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DoctorDocument, DoctorDocumentDetail>(id), ct)))
            .WithName("doctor-documents_get")
            .Produces<DoctorDocumentDetail>()
            .RequirePermission("doctor-documents.read");

        group.MapPost("/", async (HttpRequest http, DoctorDocumentInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DoctorDocument, DoctorDocumentDetail, DoctorDocumentInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/doctor-documents", created);
            })
            .WithName("doctor-documents_create")
            .Produces<DoctorDocumentDetail>(StatusCodes.Status201Created)
            .RequirePermission("doctor-documents.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, DoctorDocumentInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DoctorDocument, DoctorDocumentDetail, DoctorDocumentInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("doctor-documents_update")
            .Produces<DoctorDocumentDetail>()
            .RequirePermission("doctor-documents.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DoctorDocument>(id), ct);
                return Results.NoContent();
            })
            .WithName("doctor-documents_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("doctor-documents.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DoctorDocument>(id), ct)))
            .WithName("doctor-documents_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("doctor-documents.read");
    }
}
