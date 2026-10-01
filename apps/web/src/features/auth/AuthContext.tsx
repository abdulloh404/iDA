import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import { getAuthToken, onUnauthorized, setAuthToken } from '../../api/client';
import { login as loginRequest, readStoredSession, refreshSession, switchHospital as switchHospitalRequest, writeStoredSession, } from './session';
import type { Session } from './session';
import { AuthContext } from './authState';
import type { AuthState } from './authState';
const REFRESH_EVERY_MS = 60000;
function restoreSession(): Session | null {
    const stored = readStoredSession();
    setAuthToken(stored?.token ?? null, stored?.tenantApiPath ?? null);
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
        setAuthToken(next?.token ?? null, next?.tenantApiPath ?? null);
        writeStoredSession(next);
        setSession(next);
        onSessionChange?.();
    }, [onSessionChange]);
    useEffect(() => {
        onUnauthorized(() => apply(null));
    }, [apply]);
    const userId = session?.user.id;
    const hospitalId = session?.hospitalId;
    const tenantApiPath = session?.tenantApiPath;
    useEffect(() => {
        if (!userId)
            return;
        let cancelled = false;
        const refresh = async () => {
            if (tenantApiPath && Date.now() - lastRefresh.current < REFRESH_EVERY_MS)
                return;
            lastRefresh.current = Date.now();
            const requestToken = getAuthToken();
            try {
                const next = await refreshSession();
                if (cancelled || requestToken !== getAuthToken())
                    return;
                if (next.hospitalId !== hospitalId || next.tenantApiPath !== tenantApiPath) {
                    apply(next);
                }
                else {
                    setAuthToken(next.token, next.tenantApiPath);
                    writeStoredSession(next);
                    setSession(next);
                }
            }
            catch {
                if (!cancelled && requestToken === getAuthToken() && !tenantApiPath)
                    apply(null);
            }
        };
        void refresh();
        const onFocus = () => void refresh();
        window.addEventListener('focus', onFocus);
        return () => {
            cancelled = true;
            window.removeEventListener('focus', onFocus);
        };
    }, [userId, hospitalId, tenantApiPath, apply]);
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
    return <AuthContext.Provider value={value}>{session && !session.tenantApiPath ? null : children}</AuthContext.Provider>;
}
