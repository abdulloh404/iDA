import { createContext, useContext } from 'react';
import type { Session } from './session';
export interface AuthState {
    session: Session | null;
    isSwitchingHospital: boolean;
    signIn: (username: string, password: string) => Promise<void>;
    signOut: () => void;
    changeHospital: (hospitalId: string) => Promise<void>;
    can: (permission: string) => boolean;
}
export const AuthContext = createContext<AuthState | null>(null);
export function useAuth(): AuthState {
    const context = useContext(AuthContext);
    if (!context)
        throw new Error('useAuth must be used inside <AuthProvider>');
    return context;
}
export function usePermission(permission: string | undefined): boolean {
    const { can } = useAuth();
    return permission ? can(permission) : true;
}
