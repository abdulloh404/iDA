export const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:3100';
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
let onUnauthorizedCallback: (() => void) | null = null;
export function setAuthToken(token: string | null): void {
    authToken = token;
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
async function request(path: string, init: RequestInit_ = {}): Promise<Response> {
    const res = await fetch(`${API_BASE}${path}${qs(init.params)}`, {
        method: init.method ?? 'GET',
        headers: {
            'Content-Type': 'application/json',
            ...(authToken ? { Authorization: `Bearer ${authToken}` } : {}),
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
    if (res.status === 401)
        onUnauthorizedCallback?.();
    const error = envelope?.error;
    throw new ApiError(res.status, error?.code ?? 'http_error', error?.message ?? `HTTP ${res.status}`, error?.traceId, error?.details);
}
export async function api<T>(path: string, init: RequestInit_ = {}): Promise<T> {
    const res = await request(path, init);
    if (res.status === 204)
        return undefined as T;
    const text = await res.text();
    return (text ? JSON.parse(text) : null) as T;
}
export async function apiBlob(path: string, init: RequestInit_ = {}): Promise<{
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
export interface HelloDto {
    message: string;
    serverTime: string;
}
export const getHello = (name: string) => api<HelloDto>(`/hello${name ? `?name=${encodeURIComponent(name)}` : ''}`);
export const getHealth = () => api<{
    status: string;
}>('/healthz');
