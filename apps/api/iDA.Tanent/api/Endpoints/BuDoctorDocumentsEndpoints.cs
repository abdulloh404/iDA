using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Doctors;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class BuDoctorDocumentsEndpoints
{
    public static void MapBuDoctorDocumentsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "bu-doctor-documents")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'bu-doctor-documents'. Add one under Ida.Application/Features/Doctors/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/bu-doctor-documents")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<BuDoctorDocument, BuDoctorDocumentRow>(ListQueryString.Read(http)), ct)))
            .WithName("bu-doctor-documents_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<BuDoctorDocumentRow>>()
            .RequirePermission("bu-doctor-documents.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<BuDoctorDocument, BuDoctorDocumentDetail>(id), ct)))
            .WithName("bu-doctor-documents_get")
            .Produces<BuDoctorDocumentDetail>()
            .RequirePermission("bu-doctor-documents.read");

        group.MapPost("/", async (HttpRequest http, BuDoctorDocumentInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<BuDoctorDocument, BuDoctorDocumentDetail, BuDoctorDocumentInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/bu-doctor-documents", created);
            })
            .WithName("bu-doctor-documents_create")
            .Produces<BuDoctorDocumentDetail>(StatusCodes.Status201Created)
            .RequirePermission("bu-doctor-documents.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, BuDoctorDocumentInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<BuDoctorDocument, BuDoctorDocumentDetail, BuDoctorDocumentInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("bu-doctor-documents_update")
            .Produces<BuDoctorDocumentDetail>()
            .RequirePermission("bu-doctor-documents.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<BuDoctorDocument>(id), ct);
                return Results.NoContent();
            })
            .WithName("bu-doctor-documents_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("bu-doctor-documents.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<BuDoctorDocument>(id), ct)))
            .WithName("bu-doctor-documents_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("bu-doctor-documents.read");
    }
}
