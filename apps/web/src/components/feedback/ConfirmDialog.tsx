import { useRef } from 'react';
import type { ReactNode } from 'react';
import { Dialog } from './Dialog';
interface ConfirmDialogProps {
    open: boolean;
    title: string;
    children: ReactNode;
    confirmLabel?: string;
    cancelLabel?: string;
    tone?: 'primary' | 'danger';
    busy?: boolean;
    onConfirm: () => void;
    onCancel: () => void;
}
export function ConfirmDialog({ open, title, children, confirmLabel = 'ยืนยัน', cancelLabel = 'ยกเลิก', tone = 'primary', busy = false, onConfirm, onCancel, }: ConfirmDialogProps) {
    const confirmRef = useRef<HTMLButtonElement>(null);
    return (<Dialog open={open} titleId="confirm-title" onCancel={onCancel} initialFocusRef={confirmRef}>
      <h2 className="ida-modal__title" id="confirm-title">
        {title}
      </h2>
      <div className="ida-text-secondary">{children}</div>
      <div className="ida-modal__footer">
        <button type="button" className="ida-btn ida-btn--secondary" onClick={onCancel}>
          {cancelLabel}
        </button>
        <button ref={confirmRef} type="button" className={`ida-btn ${tone === 'danger' ? 'ida-btn--danger' : 'ida-btn--primary'}`} onClick={onConfirm} disabled={busy}>
          {busy && <span className="ida-spinner" aria-hidden="true"/>} {confirmLabel}
        </button>
      </div>
    </Dialog>);
}
