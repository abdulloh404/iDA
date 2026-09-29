using Ida.Api.Auth;
using Ida.Application.Features.IngestConfiguration;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace Ida.Api.Endpoints;

public static class IngestConfigurationEndpoints
{
    public static void MapIngestConfigurationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ingest/config").WithTags("ตั้งค่าการนำเข้าข้อมูล")
            .RequireAuthorization(new AuthorizeAttribute
            {
                Roles = "GROUP_ADMIN,HOSPITAL_ADMIN"
            });
        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetIngestConfigurationQuery(), ct)))
            .RequirePermission("ingest-config.read");
        group.MapGet("/schedules/cancelled", async (DateTimeOffset? beforeAt,
                Guid? beforeId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(
                    new GetCancelledIngestSchedulesQuery(beforeAt, beforeId), ct)))
            .RequirePermission("ingest-config.read");
        group.MapPut("/interfaces/{code}", async (string code, InterfaceInput input,
                ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new SaveIngestInterfaceCommand(code, input), ct)))
            .RequirePermission("ingest-config.write");
        group.MapPost("/schedules", async (ScheduleInput input, ISender sender,
                CancellationToken ct) => Results.Ok(await sender.Send(
                    new SaveIngestScheduleCommand(null, input), ct)))
            .RequirePermission("ingest-config.write");
        group.MapPut("/schedules/{id:guid}", async (Guid id, ScheduleInput input,
                ISender sender, CancellationToken ct) => Results.Ok(await sender.Send(
                    new SaveIngestScheduleCommand(id, input), ct)))
            .RequirePermission("ingest-config.write");
        group.MapPost("/schedules/{id:guid}/cancel", async (Guid id,
                ScheduleRevisionInput input, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new CancelIngestScheduleCommand(id, input.Revision), ct);
                return Results.NoContent();
            })
            .RequirePermission("ingest-config.write");
    }
}

