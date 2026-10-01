import { z } from 'zod';
import { tenantApi, tenantApiBlob } from '../../api/client';
import type { CrudApi } from '../../api/crud';
import type { AuditEntry, Paged } from '../../api/types';
import type { ScreenDescriptor } from '../master-data/descriptor';
import { DutyScheduleFormScreen } from './DutyScheduleFormScreen';
export const DUTY_SCHEDULE_STATUSES = [
    'DRAFT',
    'SUBMITTED',
    'CALCULATED',
    'REJECTED',
] as const;
export type DutyScheduleStatus = (typeof DUTY_SCHEDULE_STATUSES)[number];
export const DUTY_SCHEDULE_STATUS_LABELS: Record<DutyScheduleStatus, string> = {
    DRAFT: 'รอดำเนินการ Submit',
    SUBMITTED: 'ส่งให้บัญชีแล้ว',
    CALCULATED: 'คำนวณแล้ว',
    REJECTED: 'ถูกตีกลับ',
};
export type DutyScheduleKind = 'DUTY' | 'GUARANTEE_HOURLY' | 'GUARANTEE_SESSION' | 'GUARANTEE_MONTHLY';
export interface DutyScheduleListItem {
    id: string;
    kind: DutyScheduleKind;
    year: number;
    month: number;
    status: DutyScheduleStatus;
    shiftCount: number;
    completedShiftCount: number;
    totalWorkHours: number;
    submittedAt: string | null;
    calculatedAt: string | null;
}
export interface DutyScheduleDetail {
    id: string;
    kind: DutyScheduleKind;
    year: number;
    month: number;
    status: DutyScheduleStatus;
    submittedAt: string | null;
    submittedBy: string | null;
    calculatedAt: string | null;
    rejectedAt: string | null;
    rejectedBy: string | null;
    rejectReason: string | null;
    remark: string | null;
    rowVersion: string;
}
export interface DutyScheduleInput {
    remark: string | null;
}
export const dutyScheduleApi = {
    resource: 'duty-schedules',
    list: (params, signal?: AbortSignal) => tenantApi<Paged<DutyScheduleListItem>>('/api/master-data/duty-schedules', { params, signal }),
    get: (id, signal?: AbortSignal) => tenantApi<DutyScheduleDetail>(`/api/master-data/duty-schedules/${id}`, { signal }),
    create: (input) => tenantApi<DutyScheduleDetail>('/api/master-data/duty-schedules', { method: 'POST', body: input }),
    update: (id, input, rowVersion) => tenantApi<DutyScheduleDetail>(`/api/master-data/duty-schedules/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
    remove: (id) => tenantApi<void>(`/api/duty-schedules/${id}`, { method: 'DELETE' }),
    history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/duty-schedules/${id}/history`, { signal }),
    exportXlsx: (params) => tenantApiBlob('/api/master-data/duty-schedules/export', { params }),
} satisfies CrudApi<DutyScheduleListItem, DutyScheduleDetail, DutyScheduleInput>;
export const generateDutySchedule = (kind: DutyScheduleKind, year: number, month: number) => tenantApi<DutyScheduleDetail>('/api/duty-schedules/generate', {
    method: 'POST',
    body: { kind, year, month },
});
export const submitDutySchedule = (id: string) => tenantApi<DutyScheduleDetail>(`/api/duty-schedules/${id}/submit`, { method: 'POST' });
export const rejectDutySchedule = (id: string, reason: string) => tenantApi<DutyScheduleDetail>(`/api/duty-schedules/${id}/reject`, {
    method: 'POST',
    body: { reason },
});
export { MONTHS_TH, monthLabel, selectableYears } from '../shared/period';
import { MONTHS_TH, monthLabel, selectableYears } from '../shared/period';
function scheduleScreen(options: {
    id: string;
    path: string;
    titleTh: string;
    kind: DutyScheduleKind;
    emptyHint: string;
}): ScreenDescriptor<DutyScheduleListItem, DutyScheduleDetail, DutyScheduleInput> {
    return {
        id: options.id,
        resource: 'duty-schedules',
        path: options.path,
        titleTh: options.titleTh,
        breadcrumb: [{ label: 'ตารางเวรและการลงชื่อเข้าเวร' }],
        api: dutyScheduleApi,
        columns: [
            { key: 'year', header: 'ปี', sortable: true, width: '100px' },
            {
                key: 'month',
                header: 'เดือน',
                sortable: true,
                width: '160px',
                value: (row) => monthLabel(row.month),
            },
            { key: 'shiftCount', header: 'จำนวนเวร', align: 'right', width: '120px' },
            {
                key: 'completedShiftCount',
                header: 'ลงเวลาครบ',
                align: 'right',
                width: '140px',
                value: (row) => `${row.completedShiftCount} / ${row.shiftCount}`,
            },
            {
                key: 'totalWorkHours',
                header: 'ชั่วโมงรวม',
                align: 'right',
                width: '130px',
                sortable: false,
            },
            {
                key: 'status',
                header: 'สถานะ',
                sortable: true,
                width: '180px',
                value: (row) => DUTY_SCHEDULE_STATUS_LABELS[row.status] ?? row.status,
            },
        ],
        filters: [
            {
                kind: 'select',
                name: 'year',
                label: 'ปี',
                options: selectableYears().map((y) => ({ value: String(y), label: String(y) })),
            },
            {
                kind: 'select',
                name: 'month',
                label: 'เดือน',
                options: MONTHS_TH.map((label, index) => ({ value: String(index + 1), label })),
            },
        ],
        fixedFilters: { kind: options.kind },
        searchHint: 'ค้นหาตารางเวร',
        defaultSort: '-period',
        rowKey: (row) => row.id,
        emptyHint: options.emptyHint,
        sections: [
            { fields: [{ kind: 'textarea', name: 'remark', label: 'หมายเหตุ', rows: 3 }] },
        ],
        schema: z.object({ remark: z.string().trim().nullable() }),
        defaultValues: { remark: null },
        toInput: (detail) => ({ remark: detail.remark }),
        FormScreen: DutyScheduleFormScreen,
    };
}
export const dutyCheckinScreen = scheduleScreen({
    id: 'duty-checkin',
    path: '/duty-schedules/duty',
    titleTh: 'ลงชื่อเข้าเวร',
    kind: 'DUTY',
    emptyHint: 'ตารางเวรสร้างจากอัตราค่าแพทย์เวรของเดือนที่เลือก แล้วจึงลงเวลาทำงานรายเวร',
});
export const hourlyCheckinScreen = scheduleScreen({
    id: 'duty-checkin-hourly',
    path: '/duty-schedules/guarantee-hourly',
    titleTh: 'ลงชื่อทำงานประกันรายได้รายชั่วโมง',
    kind: 'GUARANTEE_HOURLY',
    emptyHint: 'ตารางเวรสร้างจากอัตราประกันรายได้รายชั่วโมงที่ใช้งานอยู่ในเดือนนั้น',
});
export const sessionCheckinScreen = scheduleScreen({
    id: 'duty-checkin-session',
    path: '/duty-schedules/guarantee-session',
    titleTh: 'ลงชื่อทำงานประกันรายได้รายคาบ',
    kind: 'GUARANTEE_SESSION',
    emptyHint: 'ตารางเวรสร้างจากอัตราประกันรายได้รายคาบที่ใช้งานอยู่ในเดือนนั้น',
});
export const monthlyCheckinScreen = scheduleScreen({
    id: 'duty-checkin-monthly',
    path: '/duty-schedules/guarantee-monthly',
    titleTh: 'ลงชื่อทำงานประกันรายได้รายเดือน',
    kind: 'GUARANTEE_MONTHLY',
    emptyHint: 'ตารางเวรสร้างจากอัตราประกันรายได้รายเดือนที่ใช้งานอยู่ในเดือนนั้น',
});
