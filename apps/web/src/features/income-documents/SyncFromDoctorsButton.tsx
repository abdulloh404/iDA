import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { tenantApi } from '../../api/client';
import { Icon } from '../../components/Icon';
import { ApiErrorAlert } from '../../components/feedback/ApiErrorAlert';
import { ConfirmDialog } from '../../components/feedback/ConfirmDialog';
import { useToast } from '../../components/feedback/toastContext';
import { invalidateAfterWrite } from '../../app/queryClient';
import { usePermission } from '../auth/authState';
interface SyncResult {
    created: number;
    skippedNoEmail: number;
}
export function SyncFromDoctorsButton() {
    const canWrite = usePermission('slip-settings.write');
    const queryClient = useQueryClient();
    const toast = useToast();
    const [confirming, setConfirming] = useState(false);
    const sync = useMutation({
        mutationFn: () => tenantApi<SyncResult>('/api/master-data/slip-settings/sync', { method: 'POST' }),
        onSuccess: (result) => {
            invalidateAfterWrite(queryClient);
            setConfirming(false);
            const skipped = result.skippedNoEmail > 0
                ? ` · ข้าม ${result.skippedNoEmail.toLocaleString('th-TH')} รหัสที่ประวัติแพทย์ยังไม่มีอีเมล`
                : '';
            toast.success(result.created > 0
                ? `เพิ่มการตั้งค่าให้แพทย์ ${result.created.toLocaleString('th-TH')} รหัส${skipped}`
                : `แพทย์ทุกรหัสมีการตั้งค่าแล้ว${skipped}`);
        },
    });
    if (!canWrite)
        return null;
    return (<>
      <button type="button" className="ida-btn ida-btn--secondary" onClick={() => setConfirming(true)}>
        <Icon name="refresh" size={18}/>
        ดึงจากประวัติแพทย์
      </button>
      <ConfirmDialog open={confirming} title="ดึงการตั้งค่าจากประวัติแพทย์?" confirmLabel="ดึงข้อมูล" cancelLabel="ยกเลิก" busy={sync.isPending} onCancel={() => {
            sync.reset();
            setConfirming(false);
        }} onConfirm={() => sync.mutate()}>
        ระบบจะเพิ่มการตั้งค่าให้รหัสแพทย์ที่ใช้งานอยู่และยังไม่มีการตั้งค่า โดยใช้อีเมลในประวัติแพทย์
        แถวที่มีอยู่แล้วจะไม่ถูกแก้ไข
        {sync.error != null && (<div style={{ marginTop: 'var(--ida-space-4)' }}>
            <ApiErrorAlert error={sync.error}/>
          </div>)}
      </ConfirmDialog>
    </>);
}
