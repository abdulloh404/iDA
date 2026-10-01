import { tenantApi } from '../../api/client';
import type { SelectOption } from '../../components/form/Select';
import type { ApprovalAction } from '../master-data/descriptor';
export type ApprovalStatus = 'DRAFT' | 'PENDING' | 'APPROVED' | 'RETURNED' | 'REJECTED' | 'CANCELLED';
export const APPROVAL_STATUS_OPTIONS: readonly SelectOption[] = [
    { value: 'PENDING', label: 'รออนุมัติ' },
    { value: 'APPROVED', label: 'อนุมัติ' },
    { value: 'RETURNED', label: 'ส่งกลับ' },
    { value: 'CANCELLED', label: 'ยกเลิกคำขอ' },
    { value: 'REJECTED', label: 'ไม่อนุมัติ' },
];
export const CYCLE_OPTIONS: readonly SelectOption[] = [
    { value: 'false', label: 'ยังไม่ปิดรอบ' },
    { value: 'true', label: 'ปิดรอบแล้ว' },
];
export const cycleLabel = (closed: boolean) => (closed ? 'ปิดรอบแล้ว' : 'ยังไม่ปิดรอบ');
export const decideDoctorFees = (resource: 'external-fees' | 'fee-items') => (ids: readonly string[], action: ApprovalAction, comment: string | null) => tenantApi<{
    decided: number;
    status: ApprovalStatus;
}>('/api/doctor-fees/decide', {
    method: 'POST',
    body: { resource, ids, action, comment },
});
export function whyLocked(row: {
    approvalStatus: ApprovalStatus;
    cycleClosed: boolean;
}) {
    if (row.cycleClosed)
        return 'รายการนี้ปิดรอบชำระแล้ว แก้ไขหรือลบไม่ได้';
    switch (row.approvalStatus) {
        case 'APPROVED':
            return 'รายการนี้อนุมัติแล้ว แก้ไขหรือลบไม่ได้';
        case 'REJECTED':
            return 'รายการนี้ไม่ได้รับอนุมัติแล้ว ให้สร้างรายการใหม่แทน';
        case 'CANCELLED':
            return 'รายการนี้ถูกยกเลิกคำขอแล้ว';
        default:
            return null;
    }
}
export const APPROVE_PERMISSION = 'doctor-fee-402.approve';
export const BREADCRUMB = [{ label: 'จัดการค่าแพทย์ 40(2)' }] as const;
