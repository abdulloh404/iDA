using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.General;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DocumentTypesEndpoints
{
    public static void MapDocumentTypesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "document-types")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'document-types'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/document-types")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<MstDocumentType, DocumentTypeListItem>(ListQueryString.Read(http)), ct)))
            .WithName("document-types_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<DocumentTypeListItem>>()
            .RequirePermission("document-types.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<MstDocumentType, DocumentTypeDetail>(id), ct)))
            .WithName("document-types_get")
            .Produces<DocumentTypeDetail>()
            .RequirePermission("document-types.read");

        group.MapPost("/", async (HttpRequest http, DocumentTypeInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<MstDocumentType, DocumentTypeDetail, DocumentTypeInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/document-types", created);
            })
            .WithName("document-types_create")
            .Produces<DocumentTypeDetail>(StatusCodes.Status201Created)
            .RequirePermission("document-types.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, DocumentTypeInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<MstDocumentType, DocumentTypeDetail, DocumentTypeInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("document-types_update")
            .Produces<DocumentTypeDetail>()
            .RequirePermission("document-types.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<MstDocumentType>(id), ct);
                return Results.NoContent();
            })
            .WithName("document-types_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("document-types.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<MstDocumentType>(id), ct)))
            .WithName("document-types_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("document-types.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<MstDocumentType>(ListQueryString.Read(http)), ct)))
            .WithName("document-types_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<MstDocumentType, DocumentTypeListItem, DocumentTypeDetail, DocumentTypeInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("document-types.read");
    }
}
