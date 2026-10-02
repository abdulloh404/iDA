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
    setAuthToken(stored?.token ?? null, stored?.hospitalId ?? null);
    return stored;
}
export function AuthProvider({ children, onSessionChange, }: {
    children: ReactNode;
    onSessionChange?: () => void;
}) {
    const [session, setSession] = useState<Session | null>(restoreSession);
    const [isSwitchingHospital, setIsSwitchingHospital] = useState(false);
    const lastRefresh = useRef(0);
    const apply = useCallback((next: Session | null) => {
        lastRefresh.current = Date.now();
        setAuthToken(next?.token ?? null, next?.hospitalId ?? null);
        writeStoredSession(next);
        setSession(next);
        onSessionChange?.();
    }, [onSessionChange]);
    useEffect(() => {
        onUnauthorized(() => apply(null));
    }, [apply]);
    const userId = session?.user.id;
    const hospitalId = session?.hospitalId;
    useEffect(() => {
        if (!userId)
            return;
        let cancelled = false;
        const refresh = async () => {
            if (hospitalId && Date.now() - lastRefresh.current < REFRESH_EVERY_MS)
                return;
            lastRefresh.current = Date.now();
            const requestToken = getAuthToken();
            try {
                const next = await refreshSession();
                if (cancelled || requestToken !== getAuthToken())
                    return;
                if (next.hospitalId !== hospitalId) {
                    apply(next);
                }
                else {
                    setAuthToken(next.token, next.hospitalId);
                    writeStoredSession(next);
                    setSession(next);
                }
            }
            catch {
                if (!cancelled && requestToken === getAuthToken() && !hospitalId)
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
    }, [userId, hospitalId, apply]);
    const value = useMemo<AuthState>(() => {
        const granted = new Set(session?.permissions ?? []);
        return {
            session,
            isSwitchingHospital,
            signIn: async (username, password) => apply(await loginRequest(username, password)),
            signOut: () => apply(null),
            changeHospital: async (hospitalId) => {
                if (isSwitchingHospital || hospitalId === session?.hospitalId)
                    return;
                setIsSwitchingHospital(true);
                try {
                    apply(await switchHospitalRequest(hospitalId));
                }
                finally {
                    setIsSwitchingHospital(false);
                }
            },
            can: (permission) => granted.has(permission),
        };
    }, [session, isSwitchingHospital, apply]);
    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
