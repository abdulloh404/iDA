import type { AuditEntry, ListParams, Paged } from './types';
export interface CrudApi<TList, TDetail, TInput> {
    resource: string;
    list: (params: ListParams, signal?: AbortSignal) => Promise<Paged<TList>>;
    get: (id: string, signal?: AbortSignal) => Promise<TDetail>;
    create: (input: TInput) => Promise<TDetail>;
    update: (id: string, input: TInput, rowVersion: string) => Promise<TDetail>;
    remove: (id: string) => Promise<void>;
    history: (id: string, signal?: AbortSignal) => Promise<AuditEntry[]>;
    exportXlsx: (params: ListParams) => Promise<{
        blob: Blob;
        fileName: string;
    }>;
}
