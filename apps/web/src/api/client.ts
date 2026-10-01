export const API_BASE = (import.meta.env.API_URL ?? '').replace(/\/+$/, '');
export interface FieldError {
    field: string;
    code: string;
    message: string;
}
export class ApiError extends Error {
    code: string;
    status: number;
    traceId?: string;
    fields: FieldError[];
    details?: unknown;
    constructor(status: number, code: string, message: string, traceId?: string, details?: unknown) {
        super(message);
        this.status = status;
        this.code = code;
        this.traceId = traceId;
        this.details = details;
        this.fields = readFields(details);
    }
}
function readFields(details: unknown): FieldError[] {
    if (!details || typeof details !== 'object')
        return [];
    const fields = (details as {
        fields?: unknown;
    }).fields;
    return Array.isArray(fields) ? (fields as FieldError[]) : [];
}
let authToken: string | null = null;
let activeTenantApiPath: string | null = null;
let onUnauthorizedCallback: (() => void) | null = null;
export function setAuthToken(token: string | null, tenantApiPath: string | null = null): void {
    authToken = token;
    activeTenantApiPath = token ? tenantApiPath : null;
}
export function getAuthToken(): string | null {
    return authToken;
}
export function onUnauthorized(callback: () => void): void {
    onUnauthorizedCallback = callback;
}
export function qs(params?: Record<string, unknown>): string {
    if (!params)
        return '';
    const search = new URLSearchParams();
    for (const [key, value] of Object.entries(params)) {
        if (value === null || value === undefined || value === '')
            continue;
        search.append(key, String(value));
    }
    const text = search.toString();
    return text ? `?${text}` : '';
}
export interface RequestInit_ {
    method?: string;
    body?: unknown;
    params?: Record<string, unknown>;
    signal?: AbortSignal;
}
function tenantPath(path: string): string {
    if (!activeTenantApiPath)
        throw new ApiError(401, 'tenant_not_selected', 'กรุณาเลือกโรงพยาบาลก่อนเรียกใช้งานข้อมูล');
    return `${activeTenantApiPath}${path}`;
}
async function request(path: string, init: RequestInit_ = {}): Promise<Response> {
    const requestToken = authToken;
    const res = await fetch(`${API_BASE}${path}${qs(init.params)}`, {
        method: init.method ?? 'GET',
        headers: {
            'Content-Type': 'application/json',
            ...(requestToken ? { Authorization: `Bearer ${requestToken}` } : {}),
        },
        ...(init.body !== undefined ? { body: JSON.stringify(init.body) } : {}),
        ...(init.signal ? { signal: init.signal } : {}),
    });
    if (res.ok)
        return res;
    const text = await res.text();
    let envelope: {
        error?: {
            code?: string;
            message?: string;
            traceId?: string;
            details?: unknown;
        };
    } | null = null;
    try {
        envelope = text ? JSON.parse(text) : null;
    }
    catch {
    }
    if (res.status === 401 && requestToken === authToken)
        onUnauthorizedCallback?.();
    const error = envelope?.error;
    throw new ApiError(res.status, error?.code ?? 'http_error', error?.message ?? `HTTP ${res.status}`, error?.traceId, error?.details);
}
async function requestJson<T>(path: string, init: RequestInit_ = {}): Promise<T> {
    const res = await request(path, init);
    if (res.status === 204)
        return undefined as T;
    const text = await res.text();
    return (text ? JSON.parse(text) : null) as T;
}
async function requestBlob(path: string, init: RequestInit_ = {}): Promise<{
    blob: Blob;
    fileName: string;
}> {
    const res = await request(path, init);
    const disposition = res.headers.get('content-disposition') ?? '';
    const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition);
    return {
        blob: await res.blob(),
        fileName: match ? decodeURIComponent(match[1]) : 'export.xlsx',
    };
}
export const coreApi = async <T>(path: string, init: RequestInit_ = {}) => requestJson<T>(`/core${path}`, init);
export const tenantApi = async <T>(path: string, init: RequestInit_ = {}) => requestJson<T>(tenantPath(path), init);
export const coreApiBlob = async (path: string, init: RequestInit_ = {}) => requestBlob(`/core${path}`, init);
export const tenantApiBlob = async (path: string, init: RequestInit_ = {}) => requestBlob(tenantPath(path), init);