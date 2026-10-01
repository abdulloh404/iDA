using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.DoctorFee402;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class TaxDeductionsEndpoints
{
    public static void MapTaxDeductionsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "tax-deductions")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'tax-deductions'.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/tax-deductions")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DfTaxDeduction, TaxDeductionListItem>(ListQueryString.Read(http)), ct)))
            .WithName("tax-deductions_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<TaxDeductionListItem>>()
            .RequirePermission("tax-deductions.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DfTaxDeduction, TaxDeductionDetail>(id), ct)))
            .WithName("tax-deductions_get")
            .Produces<TaxDeductionDetail>()
            .RequirePermission("tax-deductions.read");

        group.MapPost("/", async (HttpRequest http, TaxDeductionInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DfTaxDeduction, TaxDeductionDetail, TaxDeductionInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/tax-deductions", created);
            })
            .WithName("tax-deductions_create")
            .Produces<TaxDeductionDetail>(StatusCodes.Status201Created)
            .RequirePermission("tax-deductions.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, TaxDeductionInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DfTaxDeduction, TaxDeductionDetail, TaxDeductionInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("tax-deductions_update")
            .Produces<TaxDeductionDetail>()
            .RequirePermission("tax-deductions.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DfTaxDeduction>(id), ct);
                return Results.NoContent();
            })
            .WithName("tax-deductions_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("tax-deductions.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DfTaxDeduction>(id), ct)))
            .WithName("tax-deductions_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("tax-deductions.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(
                    new ExportQuery<DfTaxDeduction, TaxDeductionListItem>(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("tax-deductions_export")
            .RequirePermission("tax-deductions.export");
    }
}
