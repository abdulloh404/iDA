using Ida.Application.Common;

namespace Ida.Application.Features.IngestMonitoring;

public record IngestBatchSummary(
    int BatchCount,
    int DatasetRunCount,
    int PublishedRunCount,
    int FailedRunCount,
    int WithdrawnRunCount,
    int ReceivedCount,
    int ChangedCount,
    int DuplicateCount,
    int PendingCount,
    int RejectedCount);

public record IngestLegacySummary(
    int DatasetRunCount,
    int RawPageCount,
    DateTime? FirstStartedAt,
    DateTime? LastStartedAt);

public record IngestBatchListItem(
    Guid Id,
    DateOnly BusinessDate,
    string SourceMode,
    string SourceFilter,
    string Status,
    DateTime StartedAt,
    DateTime? FinishedAt,
    string? TriggerKind,
    string? TriggeredBy,
    int DatasetRunCount,
    int FailedRunCount,
    int WithdrawnRunCount,
    int ReceivedCount,
    int StagedCount,
    int ChangedCount,
    int DuplicateCount,
    int PendingCount,
    int RejectedCount,
    int RawPageCount,
    int RawBodyCount,
    string? ErrorMessage);

public record IngestBatchListDto(
    PagedResult<IngestBatchListItem> Batches,
    IngestBatchSummary Summary,
    IngestLegacySummary Legacy);

public record IngestRunListItem(
    Guid Id,
    Guid? BatchId,
    string DatasetCode,
    string? DatasetName,
    string FixtureVersion,
    DateOnly BusinessDate,
    string SourceMode,
    string Status,
    DateTime StartedAt,
    DateTime? FinishedAt,
    int ReceivedCount,
    int StagedCount,
    int ChangedCount,
    int DuplicateCount,
    int PendingCount,
    int RejectedCount,
    decimal SourceTotal,
    decimal LoadedTotal,
    int RawPageCount,
    int RawBodyCount,
    int IssueCount,
    string? ReconciliationStatus,
    decimal? ReconciliationDifference,
    string? ErrorMessage);

public record IngestBatchDetail(
    IngestBatchListItem Batch,
    IReadOnlyList<IngestRunListItem> Runs);

public record IngestStagingItem(
    Guid Id,
    int PageNumber,
    int ItemIndex,
    string DatasetCode,
    string? SourceKeyCandidate,
    string? ValidationStatus,
    string? Disposition,
    Guid? TargetRecordId,
    DateTime ReceivedAt,
    DateTime? ProcessedAt,
    string? PayloadJson);

public record IngestIssueItem(
    Guid Id,
    Guid? StagingId,
    string IssueKind,
    string IssueCode,
    string? FieldName,
    string Message,
    DateTime OccurredAt);

public record IngestRunEventItem(
    Guid Id,
    string Status,
    DateTime OccurredAt,
    string? Detail);

public record IngestRawPageItem(
    int PageNumber,
    DateTime ReceivedAt,
    string? RawSha256,
    bool RawAvailable,
    bool PayloadAvailable);

public record IngestReconciliationDto(
    int ReceivedCount,
    int ChangedCount,
    int DuplicateCount,
    decimal SourceTotal,
    decimal LoadedTotal,
    decimal Difference,
    string Status);

public record IngestControlActionItem(
    Guid Id,
    string Action,
    string Actor,
    string Reason,
    DateTime ActedAt);

public record IngestRunDetail(
    IngestRunListItem Run,
    IReadOnlyList<IngestStagingItem> StagingItems,
    int StagingItemCount,
    IReadOnlyList<IngestIssueItem> Issues,
    IReadOnlyList<IngestRunEventItem> Events,
    IReadOnlyList<IngestRawPageItem> RawPages,
    IReadOnlyList<IngestControlActionItem> ControlActions,
    IngestReconciliationDto? Reconciliation);

public record IngestRawPageDetail(
    Guid RunId,
    string DatasetCode,
    int PageNumber,
    DateTime ReceivedAt,
    string? RawSha256,
    string? RawBody,
    string? PayloadJson);

public record TriggerMockIngestInput(
    DateOnly BusinessDate,
    string Source,
    string IdempotencyKey,
    bool SimulateFailureAfterCapture = false,
    string[]? DatasetCodes = null);

public record TriggerMockIngestResult(
    Guid BatchId,
    string Status,
    DateOnly BusinessDate,
    string Source,
    bool ReusedIdempotencyKey,
    string? ErrorMessage);

public interface IIngestMonitoringStore
{
    Task<IngestBatchListDto> ListBatches(string hospital, ListRequest request,
        CancellationToken ct);
    Task<IngestBatchDetail> GetBatch(string hospital, Guid batchId, CancellationToken ct);
    Task<IngestRunDetail> GetRun(string hospital, Guid runId, CancellationToken ct);
    Task<IngestRawPageDetail> GetRawPage(string hospital, Guid runId, int pageNumber,
        CancellationToken ct);
}

public interface IMockIngestRunner
{
    Task<TriggerMockIngestResult> Trigger(string hospital, string actor,
        TriggerMockIngestInput input, CancellationToken ct);
}

public record ListIngestBatchesQuery(ListRequest Request) : IQuery<IngestBatchListDto>;
public record GetIngestBatchQuery(Guid BatchId) : IQuery<IngestBatchDetail>;
public record GetIngestRunQuery(Guid RunId) : IQuery<IngestRunDetail>;
public record GetIngestRawPageQuery(Guid RunId, int PageNumber) : IQuery<IngestRawPageDetail>;
public record TriggerMockIngestCommand(TriggerMockIngestInput Input) : ICommand<TriggerMockIngestResult>;

public class IngestMonitoringHandlers(IIngestMonitoringStore store, IMockIngestRunner runner,
    ITenantContext tenant, ICurrentUser user) :
    IQueryHandler<ListIngestBatchesQuery, IngestBatchListDto>,
    IQueryHandler<GetIngestBatchQuery, IngestBatchDetail>,
    IQueryHandler<GetIngestRunQuery, IngestRunDetail>,
    IQueryHandler<GetIngestRawPageQuery, IngestRawPageDetail>,
    ICommandHandler<TriggerMockIngestCommand, TriggerMockIngestResult>
{
    public Task<IngestBatchListDto> Handle(ListIngestBatchesQuery request,
        CancellationToken ct) => store.ListBatches(tenant.HospitalId, request.Request, ct);

    public Task<IngestBatchDetail> Handle(GetIngestBatchQuery request, CancellationToken ct) =>
        store.GetBatch(tenant.HospitalId, request.BatchId, ct);

    public Task<IngestRunDetail> Handle(GetIngestRunQuery request, CancellationToken ct) =>
        store.GetRun(tenant.HospitalId, request.RunId, ct);

    public Task<IngestRawPageDetail> Handle(GetIngestRawPageQuery request,
        CancellationToken ct)
    {
        if (request.PageNumber < 1)
            throw ApiException.BadRequest("invalid_page", "เลขหน้า raw ต้องมากกว่า 0");
        return store.GetRawPage(tenant.HospitalId, request.RunId, request.PageNumber, ct);
    }

    public Task<TriggerMockIngestResult> Handle(TriggerMockIngestCommand request,
        CancellationToken ct)
    {
        var input = request.Input;
        var source = input.Source.Trim().ToLowerInvariant();
        if (source == "custom" && !user.IsInRole("GROUP_ADMIN") && !user.IsInRole("HOSPITAL_ADMIN"))
            throw ApiException.Forbidden(message: "การเลือกโดเมนเพื่อนำเข้าทันทีสำหรับผู้ดูแลระบบเท่านั้น");
        if (source is not ("all" or "his" or "oracle" or "custom"))
            throw ApiException.BadRequest("invalid_source", "เลือกแหล่งข้อมูล mock เป็น all, his, oracle หรือ custom เท่านั้น");
        var codes = input.DatasetCodes?.Select(code => code?.Trim() ?? "").ToArray() ?? [];
        if (source == "custom" && (codes.Length == 0 || codes.Any(string.IsNullOrWhiteSpace) ||
            codes.Length != codes.Distinct(StringComparer.Ordinal).Count()))
            throw ApiException.BadRequest("invalid_dataset_codes", "เลือกโดเมนอย่างน้อยหนึ่งรายการและห้ามซ้ำ");
        if (source != "custom" && codes.Length > 0)
            throw ApiException.BadRequest("invalid_dataset_codes", "ระบุ datasetCodes ได้เฉพาะ source=custom");
        if (string.IsNullOrWhiteSpace(input.IdempotencyKey) ||
            input.IdempotencyKey.Trim().Length is < 8 or > 120)
            throw ApiException.BadRequest("invalid_idempotency_key", "ต้องส่ง idempotency key ยาว 8-120 ตัวอักษร");
        return runner.Trigger(tenant.HospitalId, user.UserName, input with
        {
            Source = source,
            IdempotencyKey = input.IdempotencyKey.Trim(),
            DatasetCodes = codes.Order(StringComparer.Ordinal).ToArray()
        }, ct);
    }
}

