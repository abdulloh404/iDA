import { coreApi } from '../../api/client';
export interface HospitalAccess {
    hospitalId: string;
    tenantApiPath: string;
    hospitalName: string;
    roleCode: string;
    roleNameTh: string;
}
export interface SessionUser {
    id: string;
    username: string;
    displayName: string;
    email: string | null;
    photoUrl: string | null;
}
export interface Session {
    token: string;
    expiresAt: string;
    user: SessionUser;
    hospitalId: string;
    tenantApiPath: string;
    hospitals: HospitalAccess[];
    roles: string[];
    permissions: string[];
}
export type Me = Omit<Session, 'token' | 'expiresAt'>;
export const login = (username: string, password: string) => coreApi<Session>('/api/auth/login', { method: 'POST', body: { username, password } });
export const fetchMe = (signal?: AbortSignal) => coreApi<Me>('/api/auth/me', { signal });
export const refreshSession = (signal?: AbortSignal) => coreApi<Session>('/api/auth/refresh', { method: 'POST', signal });
export const switchHospital = (hospitalId: string) => coreApi<Session>('/api/auth/switch-hospital', { method: 'POST', body: { hospitalId } });
const STORAGE_KEY = 'ida.session';
export function readStoredSession(): Session | null {
    try {
        const raw = sessionStorage.getItem(STORAGE_KEY);
        if (!raw)
            return null;
        const session = JSON.parse(raw) as Session;
        if (new Date(session.expiresAt).getTime() <= Date.now())
            return null;
        return session;
    }
    catch {
        return null;
    }
}
export function writeStoredSession(session: Session | null): void {
    try {
        if (session)
            sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
        else
            sessionStorage.removeItem(STORAGE_KEY);
    }
    catch {
    }
}
