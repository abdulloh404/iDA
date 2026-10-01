import { api } from '../../api/client';
import type { ListParams, Paged } from '../../api/types';
export type IngestStatus = 'Running' | 'Published' | 'Failed' | 'Withdrawn';
export type SourceFilter = 'all' | 'his' | 'oracle' | 'custom';
export interface IngestBatchSummary {
    batchCount: number;
    datasetRunCount: number;
    publishedRunCount: number;
    failedRunCount: number;
    withdrawnRunCount: number;
    receivedCount: number;
    changedCount: number;
    duplicateCount: number;
    pendingCount: number;
    rejectedCount: number;
}
export interface IngestLegacySummary {
    datasetRunCount: number;
    rawPageCount: number;
    firstStartedAt: string | null;
    lastStartedAt: string | null;
}
export interface IngestBatchListItem {
    id: string;
    businessDate: string;
    sourceMode: string;
    sourceFilter: SourceFilter | 'custom';
    status: IngestStatus;
    startedAt: string;
    finishedAt: string | null;
    triggerKind: string | null;
    triggeredBy: string | null;
    datasetRunCount: number;
    failedRunCount: number;
    withdrawnRunCount: number;
    receivedCount: number;
    stagedCount: number;
    changedCount: number;
    duplicateCount: number;
    pendingCount: number;
    rejectedCount: number;
    rawPageCount: number;
    rawBodyCount: number;
    errorMessage: string | null;
}
export interface IngestBatchListDto {
    batches: Paged<IngestBatchListItem>;
    summary: IngestBatchSummary;
    legacy: IngestLegacySummary;
}
export interface IngestRunListItem {
    id: string;
    batchId: string | null;
    datasetCode: string;
    datasetName: string | null;
    fixtureVersion: string;
    businessDate: string;
    sourceMode: string;
    status: IngestStatus;
    startedAt: string;
    finishedAt: string | null;
    receivedCount: number;
    stagedCount: number;
    changedCount: number;
    duplicateCount: number;
    pendingCount: number;
    rejectedCount: number;
    sourceTotal: number;
    loadedTotal: number;
    rawPageCount: number;
    rawBodyCount: number;
    issueCount: number;
    reconciliationStatus: string | null;
    reconciliationDifference: number | null;
    errorMessage: string | null;
}
export interface IngestBatchDetail {
    batch: IngestBatchListItem;
    runs: IngestRunListItem[];
}
export interface IngestStagingItem {
    id: string;
    pageNumber: number;
    itemIndex: number;
    datasetCode: string;
    sourceKeyCandidate: string | null;
    validationStatus: string | null;
    disposition: string | null;
    targetRecordId: string | null;
    receivedAt: string;
    processedAt: string | null;
    payloadJson: string | null;
}
export interface IngestIssueItem {
    id: string;
    stagingId: string | null;
    issueKind: string;
    issueCode: string;
    fieldName: string | null;
    message: string;
    occurredAt: string;
}
export interface IngestRunEventItem {
    id: string;
    status: IngestStatus;
    occurredAt: string;
    detail: string | null;
}
export interface IngestRawPageItem {
    pageNumber: number;
    receivedAt: string;
    rawSha256: string | null;
    rawAvailable: boolean;
    payloadAvailable: boolean;
}
export interface IngestReconciliation {
    receivedCount: number;
    changedCount: number;
    duplicateCount: number;
    sourceTotal: number;
    loadedTotal: number;
    difference: number;
    status: string;
}
export interface IngestControlActionItem {
    id: string;
    action: string;
    actor: string;
    reason: string;
    actedAt: string;
}
export interface IngestRunDetail {
    run: IngestRunListItem;
    stagingItems: IngestStagingItem[];
    stagingItemCount: number;
    issues: IngestIssueItem[];
    events: IngestRunEventItem[];
    rawPages: IngestRawPageItem[];
    controlActions: IngestControlActionItem[];
    reconciliation: IngestReconciliation | null;
}
export interface IngestRawPageDetail {
    runId: string;
    datasetCode: string;
    pageNumber: number;
    receivedAt: string;
    rawSha256: string | null;
    rawBody: string | null;
    payloadJson: string | null;
}
export interface TriggerMockIngestInput {
    businessDate: string;
    source: SourceFilter;
    idempotencyKey: string;
    simulateFailureAfterCapture?: boolean;
    datasetCodes?: string[];
}
export interface TriggerMockIngestResult {
    batchId: string;
    status: IngestStatus;
    businessDate: string;
    source: SourceFilter;
    reusedIdempotencyKey: boolean;
    errorMessage: string | null;
}
export const ingestApi = {
    listBatches: (params: ListParams, signal?: AbortSignal) => api<IngestBatchListDto>('/api/ingest/batches', { params, signal }),
    getBatch: (id: string, signal?: AbortSignal) => api<IngestBatchDetail>(`/api/ingest/batches/${id}`, { signal }),
    getRun: (id: string, signal?: AbortSignal) => api<IngestRunDetail>(`/api/ingest/runs/${id}`, { signal }),
    getRawPage: (runId: string, page: number, signal?: AbortSignal) => api<IngestRawPageDetail>(`/api/ingest/runs/${runId}/raw-pages/${page}`, { signal }),
    triggerMock: (input: TriggerMockIngestInput) => api<TriggerMockIngestResult>('/api/ingest/mock-runs', { method: 'POST', body: input }),
};
