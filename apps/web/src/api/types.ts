export interface Paged<T> {
    items: T[];
    page: number;
    pageSize: number;
    total: number;
    totalPages: number;
}
export const STATUS_FILTERS = ['all', 'active', 'inactive'] as const;
export type StatusFilter = (typeof STATUS_FILTERS)[number];
export const RECORD_STATUSES = ['ACTIVE', 'INACTIVE'] as const;
export type RecordStatus = (typeof RECORD_STATUSES)[number];
export interface ListParams {
    page?: number;
    pageSize?: number;
    sort?: string;
    q?: string;
    status?: StatusFilter;
    [key: string]: unknown;
}
export interface AuditEntry {
    id: number;
    action: 'INSERT' | 'UPDATE' | 'DELETE';
    changedBy: string;
    changedAt: string;
    changes: AuditFieldChange[];
}
export interface AuditFieldChange {
    field: string;
    oldValue: string | null;
    newValue: string | null;
}
export interface HasRowVersion {
    rowVersion: string;
}
export const DEFAULT_PAGE_SIZE = 10;
