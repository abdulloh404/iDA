export function formatDate(iso: string | null | undefined): string {
    if (!iso)
        return '—';
    const date = new Date(iso);
    if (Number.isNaN(date.getTime()))
        return iso;
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}/${date.getFullYear()}`;
}
export function formatDateTime(iso: string | null | undefined): string {
    if (!iso)
        return '—';
    const date = new Date(iso);
    if (Number.isNaN(date.getTime()))
        return iso;
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}/${date.getFullYear()} ${pad(date.getHours())}:${pad(date.getMinutes())}`;
}
export function todayInput(): string {
    const date = new Date();
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}
export function shortId(id: string): string {
    return id.slice(0, 8);
}
export function formatInt(value: number): string {
    return value.toLocaleString('th-TH');
}
export function prettyJson(raw: string | null): string {
    if (!raw)
        return '—';
    try {
        return JSON.stringify(JSON.parse(raw), null, 2);
    }
    catch {
        return raw;
    }
}
