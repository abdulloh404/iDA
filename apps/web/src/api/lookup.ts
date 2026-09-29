import { api } from './client';
export interface LookupItem {
    id: string;
    code: string;
    name: string;
}
export function fetchLookup(resource: string, params?: Record<string, unknown>, signal?: AbortSignal): Promise<LookupItem[]> {
    return api<LookupItem[]>(`/api/master-data/${resource}/lookup`, { params, signal });
}
export function lookupLabel(item: LookupItem): string {
    return item.code ? `${item.code} · ${item.name}` : item.name;
}
