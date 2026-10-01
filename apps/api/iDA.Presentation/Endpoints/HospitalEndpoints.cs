using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Common.Crud;
using Ida.Application.Features.MasterData.General;
using MediatR;

namespace Ida.Api.Endpoints;

public static class HospitalEndpoints
{
    public static void MapHospitalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/master-data/hospitals").WithTags("สาขาโรงพยาบาล");

        group.MapGet("/", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new ListHospitalsQuery(ListQueryString.Read(http)), ct)))
            .WithName("hospitals_list")
            .WithDescription("ตัวกรอง: page, pageSize, sort, q, status, id, nameTh")
            .Produces<PagedResult<HospitalListItem>>()
            .RequirePermission("hospitals.read");

        group.MapGet("/lookup", async (ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HospitalLookupQuery(), ct)))
            .WithName("hospitals_lookup")
            .Produces<IReadOnlyList<LookupItem>>()
            .RequirePermission("hospitals.read");

        group.MapGet("/{id}", async (string id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new GetHospitalQuery(id), ct)))
            .WithName("hospitals_get")
            .Produces<HospitalDetail>()
            .RequirePermission("hospitals.read");

        group.MapPost("/", async (HttpRequest http, HospitalInput input, ISender mediator, CancellationToken ct) =>
            {
                var created = await mediator.Send(new CreateHospitalCommand(input), ct);
                return Results.Created($"{http.PathBase}/api/master-data/hospitals/{created.Id}", created);
            })
            .WithName("hospitals_create")
            .Produces<HospitalDetail>(StatusCodes.Status201Created)
            .RequirePermission("hospitals.write");

        group.MapPut("/{id}", async (string id, HttpRequest http, HospitalInput input,
                ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(
                    new UpdateHospitalCommand(id, input, ListQueryString.RowVersion(http)), ct)))
            .WithName("hospitals_update")
            .Produces<HospitalDetail>()
            .RequirePermission("hospitals.write");

        group.MapGet("/{id}/history", async (string id, ISender mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new HospitalHistoryQuery(id), ct)))
            .WithName("hospitals_history")
            .Produces<IReadOnlyList<AuditEntryDto>>()
            .RequirePermission("hospitals.read");

        group.MapGet("/export", async (HttpRequest http, ISender mediator, CancellationToken ct) =>
            {
                var file = await mediator.Send(new ExportHospitalsQuery(ListQueryString.Read(http)), ct);
                return Results.File(file.Content, ExportFile.ContentType, file.FileName);
            })
            .WithName("hospitals_export")
            .RequirePermission("hospitals.export");
    }
}
