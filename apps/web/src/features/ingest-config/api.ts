import { tenantApi } from '../../api/client';
export interface IngestInterface {
    code: string;
    name: string;
    sourceSystem: string;
    endpointUrl: string | null;
    dataCategory: 'Master' | 'Transaction' | 'Unclassified';
}
export interface IngestSchedule {
    id: string;
    name: string;
    intervalValue: number;
    intervalUnit: 'minute' | 'hour' | 'day';
    nextRunAt: string;
    enabled: boolean;
    revision: number;
    datasetCodes: string[];
    lastStatus: string | null;
    lastStartedAt: string | null;
    cancelledAt: string | null;
}
export interface IngestConfiguration {
    hospitalId: string;
    interfaces: IngestInterface[];
    schedules: IngestSchedule[];
    cancelledScheduleCount: number;
    workerOnline: boolean;
    workerLastSeenAt: string | null;
}
export interface CancelledScheduleCursor {
    beforeAt: string;
    beforeId: string;
}
export interface CancelledSchedulePage {
    items: IngestSchedule[];
    hasMore: boolean;
}
export type InterfaceInput = Pick<IngestInterface, 'endpointUrl'>;
export type ScheduleInput = Pick<IngestSchedule, 'name' | 'intervalValue' | 'intervalUnit' | 'enabled' | 'datasetCodes'> & {
    revision?: number;
    firstRunAt?: string | null;
};
export const ingestConfigApi = {
    get: (signal?: AbortSignal) => tenantApi<IngestConfiguration>('/api/ingest/config/', { signal }),
    getCancelledSchedules: (cursor: CancelledScheduleCursor | null, signal?: AbortSignal) => {
        const query = cursor ? `?beforeAt=${encodeURIComponent(cursor.beforeAt)}&beforeId=${encodeURIComponent(cursor.beforeId)}` : '';
        return tenantApi<CancelledSchedulePage>(`/api/ingest/config/schedules/cancelled${query}`, { signal });
    },
    saveInterface: (code: string, input: InterfaceInput) => tenantApi<IngestConfiguration>(`/api/ingest/config/interfaces/${encodeURIComponent(code)}`, { method: 'PUT', body: input }),
    saveSchedule: (id: string | null, input: ScheduleInput) => tenantApi<IngestSchedule>(id ? `/api/ingest/config/schedules/${id}` : '/api/ingest/config/schedules', { method: id ? 'PUT' : 'POST', body: input }),
    cancelSchedule: (id: string, revision: number) => tenantApi<void>(`/api/ingest/config/schedules/${id}/cancel`, { method: 'POST', body: { revision } }),
};
