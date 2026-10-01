import { useState } from 'react';
import type { ReactNode } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { coreApi } from '../../api/client';
import { Icon } from '../../components/Icon';
import type { IconName } from '../../components/Icon';
import { ApiErrorAlert } from '../../components/feedback/ApiErrorAlert';
import { ConfirmDialog } from '../../components/feedback/ConfirmDialog';
import { useToast } from '../../components/feedback/toastContext';
import { invalidateAfterWrite } from '../../app/queryClient';
import { usePermission } from '../auth/authState';
import { formatDateTime } from './format';
import type { UserDetail } from './user';
interface UserAccount {
    userId: string;
    username: string;
    authType: 'AD' | 'LOCAL';
    lastLoginAt: string | null;
    passwordChangedAt: string | null;
    passwordExpiresAt: string | null;
    passwordState: 'AD' | 'NOT_SET' | 'NO_EXPIRY' | 'ACTIVE' | 'EXPIRING' | 'EXPIRED';
}
interface ResetResult {
    temporaryPassword: string;
    passwordExpiresAt: string | null;
}
const PASSWORD_STATES: Record<UserAccount['passwordState'], {
    label: string;
    badge: string;
    icon: IconName;
}> = {
    AD: { label: 'ใช้รหัสผ่านของ Active Directory', badge: 'ida-badge--info', icon: 'info' },
    NOT_SET: { label: 'ยังไม่ได้ตั้งรหัสผ่าน — เข้าสู่ระบบไม่ได้', badge: 'ida-badge--pending', icon: 'clock' },
    NO_EXPIRY: { label: 'ไม่มีวันหมดอายุ (ตั้งก่อนมีนโยบาย)', badge: 'ida-badge--info', icon: 'info' },
    ACTIVE: { label: 'ใช้งานได้', badge: 'ida-badge--success', icon: 'check' },
    EXPIRING: { label: 'ใกล้หมดอายุ', badge: 'ida-badge--pending', icon: 'clock' },
    EXPIRED: { label: 'หมดอายุแล้ว — เข้าสู่ระบบไม่ได้', badge: 'ida-badge--error', icon: 'alert' },
};
export function UserAccountPanel({ id }: {
    id: string;
    detail: UserDetail;
    readOnly: boolean;
}) {
    const canWrite = usePermission('users.write');
    const queryClient = useQueryClient();
    const toast = useToast();
    const [confirming, setConfirming] = useState(false);
    const [issued, setIssued] = useState<ResetResult | null>(null);
    const account = useQuery({
        queryKey: ['master', 'users', id, 'account'],
        queryFn: ({ signal }) => coreApi<UserAccount>(`/api/master-data/users/${id}/account`, { signal }),
    });
    const reset = useMutation({
        mutationFn: () => coreApi<ResetResult>(`/api/master-data/users/${id}/reset-password`, { method: 'POST' }),
        onSuccess: (result) => {
            setConfirming(false);
            setIssued(result);
            invalidateAfterWrite(queryClient);
        },
    });
    const copy = async () => {
        if (!issued)
            return;
        try {
            await navigator.clipboard.writeText(issued.temporaryPassword);
            toast.success('คัดลอกรหัสผ่านชั่วคราวแล้ว');
        }
        catch {
            toast.error('คัดลอกไม่สำเร็จ — เลือกข้อความแล้วคัดลอกเอง');
        }
    };
    const data = account.data;
    const state = data ? PASSWORD_STATES[data.passwordState] : null;
    return (<section className="ida-card">
      <div className="ida-card__head">
        <h2 className="ida-card__title">รายละเอียดบัญชีผู้ใช้</h2>
        {canWrite && data?.authType === 'LOCAL' && (<button type="button" className="ida-btn ida-btn--secondary ida-btn--sm" onClick={() => setConfirming(true)}>
            <Icon name="key" size={16}/>
            ตั้งรหัสผ่านใหม่
          </button>)}
      </div>

      {account.error && <ApiErrorAlert error={account.error}/>}
      {account.isPending && <span className="ida-skeleton" style={{ height: '64px', display: 'block' }}/>}

      {data && state && (<dl className="ida-detail-grid">
          <Detail label="ชื่อบัญชีผู้ใช้">{data.username}</Detail>
          <Detail label="ประเภทผู้ใช้งาน">{data.authType === 'AD' ? 'AD' : 'Local'}</Detail>
          <Detail label="เข้าใช้งานล่าสุด">
            {data.lastLoginAt ? formatDateTime(data.lastLoginAt) : 'ยังไม่เคยเข้าใช้งาน'}
          </Detail>
          <Detail label="วันที่รหัสผ่านหมดอายุครั้งถัดไป">
            <span className="ida-account-password">
              {data.passwordExpiresAt && <span>{formatDateTime(data.passwordExpiresAt)}</span>}
              <span className={`ida-badge ${state.badge}`}>
                <Icon name={state.icon} size={14}/>
                {state.label}
              </span>
            </span>
          </Detail>
        </dl>)}

      <ConfirmDialog open={confirming} title="ตั้งรหัสผ่านใหม่?" confirmLabel="ตั้งรหัสผ่านใหม่" busy={reset.isPending} onCancel={() => {
            reset.reset();
            setConfirming(false);
        }} onConfirm={() => reset.mutate()}>
        ระบบจะออกรหัสผ่านชั่วคราวให้ {data?.username} และรหัสเดิมจะใช้ไม่ได้ทันที
        รหัสชั่วคราวแสดงครั้งเดียว — ส่งต่อให้ผู้ใช้ทางช่องทางที่ปลอดภัย
        {reset.error != null && (<div style={{ marginTop: 'var(--ida-space-4)' }}>
            <ApiErrorAlert error={reset.error}/>
          </div>)}
      </ConfirmDialog>

      <ConfirmDialog open={issued !== null} title="รหัสผ่านชั่วคราว" confirmLabel="คัดลอกรหัสผ่าน" cancelLabel="ปิด" onConfirm={() => void copy()} onCancel={() => setIssued(null)}>
        <p style={{ marginTop: 0 }}>
          รหัสนี้จะไม่แสดงอีก เมื่อปิดหน้าต่างนี้แล้วต้องตั้งรหัสใหม่อีกครั้งถ้าทำหาย
        </p>
        <p className="ida-temp-password" aria-label="รหัสผ่านชั่วคราว">
          {issued?.temporaryPassword}
        </p>
        {issued?.passwordExpiresAt && (<p className="ida-caption ida-text-secondary" style={{ marginBottom: 0 }}>
            หมดอายุ {formatDateTime(issued.passwordExpiresAt)}
          </p>)}
      </ConfirmDialog>
    </section>);
}
function Detail({ label, children }: {
    label: string;
    children: ReactNode;
}) {
    return (<div className="ida-detail-grid__item">
      <dt className="ida-label">{label}</dt>
      <dd className="ida-detail-grid__value">{children}</dd>
    </div>);
}
