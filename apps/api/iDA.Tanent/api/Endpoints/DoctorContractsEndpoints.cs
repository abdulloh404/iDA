using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Doctors;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DoctorContractsEndpoints
{
    public static void MapDoctorContractsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "doctor-contracts")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'doctor-contracts'. Add one under Ida.Application/Features/Doctors/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/doctor-contracts")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DoctorContract, DoctorContractRow>(ListQueryString.Read(http)), ct)))
            .WithName("doctor-contracts_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<DoctorContractRow>>()
            .RequirePermission("doctor-contracts.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DoctorContract, DoctorContractDetail>(id), ct)))
            .WithName("doctor-contracts_get")
            .Produces<DoctorContractDetail>()
            .RequirePermission("doctor-contracts.read");

        group.MapPost("/", async (HttpRequest http, DoctorContractInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DoctorContract, DoctorContractDetail, DoctorContractInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/doctor-contracts", created);
            })
            .WithName("doctor-contracts_create")
            .Produces<DoctorContractDetail>(StatusCodes.Status201Created)
            .RequirePermission("doctor-contracts.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, DoctorContractInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DoctorContract, DoctorContractDetail, DoctorContractInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("doctor-contracts_update")
            .Produces<DoctorContractDetail>()
            .RequirePermission("doctor-contracts.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DoctorContract>(id), ct);
                return Results.NoContent();
            })
            .WithName("doctor-contracts_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("doctor-contracts.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DoctorContract>(id), ct)))
            .WithName("doctor-contracts_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("doctor-contracts.read");
    }
}
