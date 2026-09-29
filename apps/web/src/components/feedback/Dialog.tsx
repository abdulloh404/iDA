import { useEffect, useRef } from 'react';
import type { ReactNode, RefObject } from 'react';
export type DialogSize = 'sm' | 'md' | 'lg' | 'xl';
interface DialogProps {
    open: boolean;
    titleId: string;
    children: ReactNode;
    onCancel: () => void;
    size?: DialogSize;
    scrollable?: boolean;
    className?: string;
    initialFocusRef?: RefObject<HTMLElement | null>;
}
export function Dialog({ open, titleId, children, onCancel, size = 'sm', scrollable = false, className, initialFocusRef, }: DialogProps) {
    const dialogRef = useRef<HTMLDivElement>(null);
    useEffect(() => {
        if (!open)
            return;
        const field = dialogRef.current?.querySelector<HTMLElement>('input:not([disabled]), textarea:not([disabled]), select:not([disabled])');
        (field ?? initialFocusRef?.current ?? dialogRef.current)?.focus();
    }, [initialFocusRef, open]);
    useEffect(() => {
        if (!open)
            return;
        const onKeyDown = (event: KeyboardEvent) => {
            if (event.key === 'Escape')
                onCancel();
        };
        document.addEventListener('keydown', onKeyDown);
        return () => document.removeEventListener('keydown', onKeyDown);
    }, [onCancel, open]);
    if (!open)
        return null;
    return (<div className="ida-modal__overlay" onClick={(event) => {
            if (event.target === event.currentTarget)
                onCancel();
        }}>
      <div ref={dialogRef} className={[
            'ida-modal',
            `ida-modal--${size}`,
            scrollable ? 'ida-modal--scrollable' : '',
            className ?? '',
        ].filter(Boolean).join(' ')} role="dialog" aria-modal="true" aria-labelledby={titleId} tabIndex={-1}>
        {children}
      </div>
    </div>);
}
