using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.SystemSettings;
using Ida.Domain.Core;
using MediatR;

namespace Ida.Api.Endpoints;

public static class TermsEndpoints
{
    public static void MapTermsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "terms")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'terms'.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/terms")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<SysTerms, TermsDetail>(ListQueryString.Read(http)), ct)))
            .WithName("terms_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<TermsDetail>>()
            .RequirePermission("terms.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<SysTerms, TermsDetail>(id), ct)))
            .WithName("terms_get")
            .Produces<TermsDetail>()
            .RequirePermission("terms.read");

        group.MapPost("/", async (HttpRequest http, TermsInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<SysTerms, TermsDetail, TermsInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/terms", created);
            })
            .WithName("terms_create")
            .Produces<TermsDetail>(StatusCodes.Status201Created)
            .RequirePermission("terms.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, TermsInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<SysTerms, TermsDetail, TermsInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("terms_update")
            .Produces<TermsDetail>()
            .RequirePermission("terms.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<SysTerms>(id), ct);
                return Results.NoContent();
            })
            .WithName("terms_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("terms.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<SysTerms>(id), ct)))
            .WithName("terms_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("terms.read");
    }
}
