import { api } from '../../api/client';
import type { ListParams, Paged } from '../../api/types';
export const APPROVAL_STATUSES = [
    'DRAFT',
    'PENDING',
    'APPROVED',
    'RETURNED',
    'REJECTED',
    'CANCELLED',
] as const;
export type ApprovalStatus = (typeof APPROVAL_STATUSES)[number];
export const APPROVAL_STATUS_LABELS: Record<ApprovalStatus, string> = {
    DRAFT: 'ร่าง',
    PENDING: 'รออนุมัติ',
    APPROVED: 'อนุมัติแล้ว',
    RETURNED: 'ส่งกลับให้แก้ไข',
    REJECTED: 'ไม่อนุมัติ',
    CANCELLED: 'ยกเลิกคำขอ',
};
export interface ApprovalRequestListItem {
    id: string;
    requestNo: string;
    requestType: string;
    requestTypeNameTh: string;
    summary: string;
    requestedBy: string;
    requestedAt: string;
    updatedAt: string;
    status: ApprovalStatus;
    currentStepRole: string | null;
    currentStepRoleNameTh: string | null;
}
export interface ApprovalStep {
    stepSeq: number;
    approverRole: string;
    approverRoleNameTh: string;
    approverUser: string | null;
    action: 'APPROVE' | 'REJECT' | 'RETURN' | null;
    actionAt: string | null;
    comment: string | null;
}
export interface ApprovalRequestDetail {
    id: string;
    requestNo: string;
    requestType: string;
    requestTypeNameTh: string;
    summary: string;
    targetTable: string;
    targetId: string | null;
    doctorId: string | null;
    doctorName: string | null;
    payload: string;
    requestedBy: string;
    requestedAt: string;
    closedAt: string | null;
    status: ApprovalStatus;
    steps: ApprovalStep[];
    canDecide: boolean;
}
export interface RequestTypeOption {
    code: string;
    nameTh: string;
}
export const APPROVAL_SCOPES = ['mine', 'pending', 'history'] as const;
export type ApprovalScope = (typeof APPROVAL_SCOPES)[number];
export const approvalsApi = {
    list: (scope: ApprovalScope, params: ListParams, signal?: AbortSignal) => api<Paged<ApprovalRequestListItem>>(`/api/approvals/${scope}`, { params, signal }),
    get: (id: string, signal?: AbortSignal) => api<ApprovalRequestDetail>(`/api/approvals/${id}`, { signal }),
    decide: (id: string, action: 'APPROVE' | 'REJECT' | 'RETURN', comment: string | null) => api<ApprovalRequestDetail>(`/api/approvals/${id}/decide`, {
        method: 'POST',
        body: { action, comment },
    }),
    requestTypes: (signal?: AbortSignal) => api<RequestTypeOption[]>('/api/approvals/request-types', { signal }),
};
