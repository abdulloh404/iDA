import { useCallback, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { Icon } from '../Icon';
import type { IconName } from '../Icon';
import { ToastContext } from './toastContext';
import type { ToastApi, ToastTone } from './toastContext';
interface Toast {
    id: number;
    tone: ToastTone;
    message: string;
}
const DISMISS_AFTER_MS = 4500;
const ICONS: Record<ToastTone, IconName> = {
    success: 'check',
    error: 'alert',
    info: 'info',
};
export function ToastProvider({ children }: {
    children: ReactNode;
}) {
    const [toasts, setToasts] = useState<Toast[]>([]);
    const push = useCallback((tone: ToastTone, message: string) => {
        const id = Date.now() + Math.random();
        setToasts((current) => [...current, { id, tone, message }]);
        window.setTimeout(() => setToasts((current) => current.filter((t) => t.id !== id)), DISMISS_AFTER_MS);
    }, []);
    const api = useMemo<ToastApi>(() => ({
        success: (m) => push('success', m),
        error: (m) => push('error', m),
        info: (m) => push('info', m),
    }), [push]);
    return (<ToastContext.Provider value={api}>
      {children}
      
      <div className="ida-toast-viewport" role="status" aria-live="polite">
        {toasts.map((toast) => (<div key={toast.id} className={`ida-toast ida-toast--${toast.tone}`}>
            <Icon name={ICONS[toast.tone]} size={18} className={`ida-toast__icon ida-toast__icon--${toast.tone}`}/>
            <span>{toast.message}</span>
          </div>))}
      </div>
    </ToastContext.Provider>);
}
