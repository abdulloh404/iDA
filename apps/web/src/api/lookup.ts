import { coreApi, tenantApi } from './client';
export interface LookupItem {
    id: string;
    code: string;
    name: string;
}
const lookupApis = {
    'ar-codes': (params?: Record<string, unknown>, signal?: AbortSignal) => tenantApi<LookupItem[]>('/api/master-data/ar-codes/lookup', { params, signal }),
    'bank-branches': (params?: Record<string, unknown>, signal?: AbortSignal) => coreApi<LookupItem[]>('/api/master-data/bank-branches/lookup', { params, signal }),
    'banks': (params?: Record<string, unknown>, signal?: AbortSignal) => coreApi<LookupItem[]>('/api/master-data/banks/lookup', { params, signal }),
    'clinics': (params?: Record<string, unknown>, signal?: AbortSignal) => tenantApi<LookupItem[]>('/api/master-data/clinics/lookup', { params, signal }),
    'departments': (params?: Record<string, unknown>, signal?: AbortSignal) => tenantApi<LookupItem[]>('/api/master-data/departments/lookup', { params, signal }),
    'doctor-codes': (params?: Record<string, unknown>, signal?: AbortSignal) => tenantApi<LookupItem[]>('/api/master-data/doctor-codes/lookup', { params, signal }),
    'doctor-groups': (params?: Record<string, unknown>, signal?: AbortSignal) => tenantApi<LookupItem[]>('/api/master-data/doctor-groups/lookup', { params, signal }),
    'doctor-types': (params?: Record<string, unknown>, signal?: AbortSignal) => tenantApi<LookupItem[]>('/api/master-data/doctor-types/lookup', { params, signal }),
    'doctors': (params?: Record<string, unknown>, signal?: AbortSignal) => coreApi<LookupItem[]>('/api/master-data/doctors/lookup', { params, signal }),
    'document-types': (params?: Record<string, unknown>, signal?: AbortSignal) => coreApi<LookupItem[]>('/api/master-data/document-types/lookup', { params, signal }),
    'expense-types': (params?: Record<string, unknown>, signal?: AbortSignal) => tenantApi<LookupItem[]>('/api/master-data/expense-types/lookup', { params, signal }),
    'hospitals': (params?: Record<string, unknown>, signal?: AbortSignal) => coreApi<LookupItem[]>('/api/master-data/hospitals/lookup', { params, signal }),
    'patient-rights': (params?: Record<string, unknown>, signal?: AbortSignal) => tenantApi<LookupItem[]>('/api/master-data/patient-rights/lookup', { params, signal }),
    'payment-types': (params?: Record<string, unknown>, signal?: AbortSignal) => tenantApi<LookupItem[]>('/api/master-data/payment-types/lookup', { params, signal }),
    'privilege-types': (params?: Record<string, unknown>, signal?: AbortSignal) => tenantApi<LookupItem[]>('/api/master-data/privilege-types/lookup', { params, signal }),
    'receipt-types': (params?: Record<string, unknown>, signal?: AbortSignal) => tenantApi<LookupItem[]>('/api/master-data/receipt-types/lookup', { params, signal }),
    'roles': (params?: Record<string, unknown>, signal?: AbortSignal) => coreApi<LookupItem[]>('/api/master-data/roles/lookup', { params, signal }),
    'share-categories': (params?: Record<string, unknown>, signal?: AbortSignal) => tenantApi<LookupItem[]>('/api/master-data/share-categories/lookup', { params, signal }),
    'specialties': (params?: Record<string, unknown>, signal?: AbortSignal) => coreApi<LookupItem[]>('/api/master-data/specialties/lookup', { params, signal }),
    'status-privileges': (params?: Record<string, unknown>, signal?: AbortSignal) => tenantApi<LookupItem[]>('/api/master-data/status-privileges/lookup', { params, signal }),
    'sub-specialties': (params?: Record<string, unknown>, signal?: AbortSignal) => coreApi<LookupItem[]>('/api/master-data/sub-specialties/lookup', { params, signal }),
    'tax-allowance-items': (params?: Record<string, unknown>, signal?: AbortSignal) => coreApi<LookupItem[]>('/api/master-data/tax-allowance-items/lookup', { params, signal }),
    'tax-types': (params?: Record<string, unknown>, signal?: AbortSignal) => tenantApi<LookupItem[]>('/api/master-data/tax-types/lookup', { params, signal }),
    'titles': (params?: Record<string, unknown>, signal?: AbortSignal) => coreApi<LookupItem[]>('/api/master-data/titles/lookup', { params, signal }),
    'treatment-categories': (params?: Record<string, unknown>, signal?: AbortSignal) => tenantApi<LookupItem[]>('/api/master-data/treatment-categories/lookup', { params, signal }),
    'treatments': (params?: Record<string, unknown>, signal?: AbortSignal) => tenantApi<LookupItem[]>('/api/master-data/treatments/lookup', { params, signal }),
    'welfare-plans': (params?: Record<string, unknown>, signal?: AbortSignal) => tenantApi<LookupItem[]>('/api/master-data/welfare-plans/lookup', { params, signal }),
};
export type LookupResource = keyof typeof lookupApis;
export function fetchLookup(resource: LookupResource, params?: Record<string, unknown>, signal?: AbortSignal): Promise<LookupItem[]> {
    return lookupApis[resource](params, signal);
}
export function lookupLabel(item: LookupItem): string {
    return item.code ? `${item.code} · ${item.name}` : item.name;
}
