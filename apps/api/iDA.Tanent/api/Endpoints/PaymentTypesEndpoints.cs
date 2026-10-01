using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.Accounting;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class PaymentTypesEndpoints
{
    public static void MapPaymentTypesEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "payment-types")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'payment-types'. Add one under Ida.Application/Features/MasterData/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/payment-types")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<MstPaymentType, PaymentTypeListItem>(ListQueryString.Read(http)), ct)))
            .WithName("payment-types_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<PaymentTypeListItem>>()
            .RequirePermission("payment-types.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<MstPaymentType, PaymentTypeDetail>(id), ct)))
            .WithName("payment-types_get")
            .Produces<PaymentTypeDetail>()
            .RequirePermission("payment-types.read");

        group.MapPost("/", async (HttpRequest http, PaymentTypeInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<MstPaymentType, PaymentTypeDetail, PaymentTypeInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/payment-types", created);
            })
            .WithName("payment-types_create")
            .Produces<PaymentTypeDetail>(StatusCodes.Status201Created)
            .RequirePermission("payment-types.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, PaymentTypeInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<MstPaymentType, PaymentTypeDetail, PaymentTypeInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("payment-types_update")
            .Produces<PaymentTypeDetail>()
            .RequirePermission("payment-types.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<MstPaymentType>(id), ct);
                return Results.NoContent();
            })
            .WithName("payment-types_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("payment-types.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<MstPaymentType>(id), ct)))
            .WithName("payment-types_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("payment-types.read");

        group.MapGet("/lookup", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new LookupQuery<MstPaymentType>(ListQueryString.Read(http)), ct)))
            .WithName("payment-types_lookup")
            .WithDescription(
                "ตัวเลือกสำหรับ dropdown ของหน้าจออื่น — เฉพาะรายการที่ใช้งาน " +
                $"สูงสุด {CrudLookupHandler<MstPaymentType, PaymentTypeListItem, PaymentTypeDetail, PaymentTypeInput>.MaxOptions} รายการ")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("payment-types.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<MstPaymentType, PaymentTypeListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("payment-types_export")
            .RequirePermission("payment-types.export");
    }
}
