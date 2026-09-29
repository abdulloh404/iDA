using Ida.Api.Auth;
using Ida.Application.Common;
using Ida.Application.Features.IngestMonitoring;
using MediatR;

namespace Ida.Api.Endpoints;

public static class IngestMonitoringEndpoints
{
    public static void MapIngestMonitoringEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ingest").WithTags("ติดตามการนำเข้าข้อมูล");

        group.MapGet("/batches", async (HttpRequest http, ISender sender,
                CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListIngestBatchesQuery(
                    ListQueryString.Read(http)), ct)))
            .WithDescription("ตัวกรอง: page, pageSize, sort, calledFrom, calledTo, businessFrom, businessTo, source, status")
            .Produces<IngestBatchListDto>()
            .RequirePermission("ingest.read");

        group.MapGet("/batches/{batchId:guid}", async (Guid batchId, ISender sender,
                CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetIngestBatchQuery(batchId), ct)))
            .Produces<IngestBatchDetail>()
            .RequirePermission("ingest.read");

        group.MapGet("/runs/{runId:guid}", async (Guid runId, ISender sender,
                CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetIngestRunQuery(runId), ct)))
            .Produces<IngestRunDetail>()
            .RequirePermission("ingest.read");

        group.MapGet("/runs/{runId:guid}/raw-pages/{pageNumber:int}",
                async (Guid runId, int pageNumber, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetIngestRawPageQuery(runId, pageNumber), ct)))
            .Produces<IngestRawPageDetail>()
            .RequirePermission("ingest.raw.read");

        group.MapPost("/mock-runs", async (TriggerMockIngestInput input, ISender sender,
                CancellationToken ct) =>
                Results.Ok(await sender.Send(new TriggerMockIngestCommand(input), ct)))
            .WithDescription("เปิด batch mock ใหม่เท่านั้น ไม่เรียก HIS/Oracle จริง")
            .Produces<TriggerMockIngestResult>()
            .RequirePermission("ingest.trigger");
    }
}

