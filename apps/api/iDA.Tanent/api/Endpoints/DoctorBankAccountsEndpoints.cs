using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.Doctors;
using Ida.Domain.Bu;
using MediatR;

namespace Ida.Api.Endpoints;

public static class DoctorBankAccountsEndpoints
{
    public static void MapDoctorBankAccountsEndpoints(this IEndpointRouteBuilder app)
    {
        var resource = CrudRegistry.Resources.FirstOrDefault(r => r.Name == "doctor-bank-accounts")
            ?? throw new InvalidOperationException("No CrudSpec declares the resource 'doctor-bank-accounts'. Add one under Ida.Application/Features/Doctors/, or remove the route.");
        var commonFilters = "page, pageSize (สูงสุด 200), sort (เช่น -code), q, status (all|active|inactive)";
        var filterDescription = resource.FilterKeys.Count == 0
            ? $"ตัวกรอง: {commonFilters}"
            : $"ตัวกรอง: {commonFilters}, {string.Join(", ", resource.FilterKeys)}";

        var group = app.MapGroup("/api/master-data/doctor-bank-accounts")
            .WithTags(resource.DisplayNameTh);

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new ListQuery<DoctorBankAccount, DoctorBankAccountRow>(ListQueryString.Read(http)), ct)))
            .WithName("doctor-bank-accounts_list")
            .WithDescription(filterDescription)
            .Produces<PagedResult<DoctorBankAccountRow>>()
            .RequirePermission("doctor-bank-accounts.read");

        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetByIdQuery<DoctorBankAccount, DoctorBankAccountDetail>(id), ct)))
            .WithName("doctor-bank-accounts_get")
            .Produces<DoctorBankAccountDetail>()
            .RequirePermission("doctor-bank-accounts.read");

        group.MapPost("/", async (HttpRequest http, DoctorBankAccountInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(
                    new CreateCommand<DoctorBankAccount, DoctorBankAccountDetail, DoctorBankAccountInput>(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/doctor-bank-accounts", created);
            })
            .WithName("doctor-bank-accounts_create")
            .Produces<DoctorBankAccountDetail>(StatusCodes.Status201Created)
            .RequirePermission("doctor-bank-accounts.write");

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest http, DoctorBankAccountInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateCommand<DoctorBankAccount, DoctorBankAccountDetail, DoctorBankAccountInput>(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("doctor-bank-accounts_update")
            .Produces<DoctorBankAccountDetail>()
            .RequirePermission("doctor-bank-accounts.write");

        group.MapDelete("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
            {
                await mediator.Send(new DeleteCommand<DoctorBankAccount>(id), ct);
                return Results.NoContent();
            })
            .WithName("doctor-bank-accounts_delete")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission("doctor-bank-accounts.delete");

        group.MapGet("/{id:guid}/history", async (Guid id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HistoryQuery<DoctorBankAccount>(id), ct)))
            .WithName("doctor-bank-accounts_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("doctor-bank-accounts.read");
    }
}
