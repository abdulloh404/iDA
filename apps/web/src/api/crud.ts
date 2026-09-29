import { api, apiBlob } from './client';
import type { AuditEntry, ListParams, Paged } from './types';
export function createCrudApi<TList, TDetail, TInput>(resource: string) {
    const base = `/api/master-data/${resource}`;
    return {
        resource,
        list: (params: ListParams, signal?: AbortSignal) => api<Paged<TList>>(base, { params, signal }),
        get: (id: string, signal?: AbortSignal) => api<TDetail>(`${base}/${id}`, { signal }),
        create: (input: TInput) => api<TDetail>(base, { method: 'POST', body: input }),
        update: (id: string, input: TInput, rowVersion: string) => api<TDetail>(`${base}/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id: string) => api<void>(`${base}/${id}`, { method: 'DELETE' }),
        history: (id: string, signal?: AbortSignal) => api<AuditEntry[]>(`${base}/${id}/history`, { signal }),
        exportXlsx: (params: ListParams) => apiBlob(`${base}/export`, { params }),
    };
}
export type CrudApi<TList, TDetail, TInput> = ReturnType<typeof createCrudApi<TList, TDetail, TInput>>;
