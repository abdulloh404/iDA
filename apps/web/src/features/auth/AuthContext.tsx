import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import { onUnauthorized, setAuthToken } from '../../api/client';
import { login as loginRequest, readStoredSession, refreshSession, switchHospital as switchHospitalRequest, writeStoredSession, } from './session';
import type { Session } from './session';
import { AuthContext } from './authState';
import type { AuthState } from './authState';
const REFRESH_EVERY_MS = 60000;
function restoreSession(): Session | null {
    const stored = readStoredSession();
    setAuthToken(stored?.token ?? null);
    return stored;
}
export function AuthProvider({ children, onSessionChange, }: {
    children: ReactNode;
    onSessionChange?: () => void;
}) {
    const [session, setSession] = useState<Session | null>(restoreSession);
    const lastRefresh = useRef(0);
    const apply = useCallback((next: Session | null) => {
        lastRefresh.current = Date.now();
        setAuthToken(next?.token ?? null);
        writeStoredSession(next);
        setSession(next);
        onSessionChange?.();
    }, [onSessionChange]);
    useEffect(() => {
        onUnauthorized(() => {
            setAuthToken(null);
            writeStoredSession(null);
            setSession(null);
        });
    }, []);
    const userId = session?.user.id;
    const hospitalId = session?.hospitalId;
    useEffect(() => {
        if (!userId)
            return;
        let cancelled = false;
        const refresh = async () => {
            if (Date.now() - lastRefresh.current < REFRESH_EVERY_MS)
                return;
            lastRefresh.current = Date.now();
            try {
                const next = await refreshSession();
                if (cancelled)
                    return;
                if (next.hospitalId !== hospitalId) {
                    apply(next);
                }
                else {
                    setAuthToken(next.token);
                    writeStoredSession(next);
                    setSession(next);
                }
            }
            catch {
            }
        };
        void refresh();
        const onFocus = () => void refresh();
        window.addEventListener('focus', onFocus);
        return () => {
            cancelled = true;
            window.removeEventListener('focus', onFocus);
        };
    }, [userId, hospitalId, apply]);
    const value = useMemo<AuthState>(() => {
        const granted = new Set(session?.permissions ?? []);
        return {
            session,
            signIn: async (username, password) => apply(await loginRequest(username, password)),
            signOut: () => apply(null),
            changeHospital: async (hospitalId) => apply(await switchHospitalRequest(hospitalId)),
            can: (permission) => granted.has(permission),
        };
    }, [session, apply]);
    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
