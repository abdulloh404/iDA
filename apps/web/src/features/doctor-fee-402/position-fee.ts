import { z } from 'zod';
import { tenantApi, tenantApiBlob } from '../../api/client';
import type { CrudApi } from '../../api/crud';
import type { AuditEntry, Paged, RecordStatus } from '../../api/types';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD, omitFields } from '../master-data/descriptor';
import type { ScreenDescriptor } from '../master-data/descriptor';
import { BREADCRUMB } from './approval';
export interface PositionFeeListItem {
    id: string;
    doctorCode: string | null;
    doctorName: string | null;
    clinicName: string | null;
    positionName: string;
    startDate: string;
    endDate: string;
    monthlyAmount: number;
    status: RecordStatus;
}
export interface PositionFeeDetail {
    id: string;
    doctorCodeId: string;
    clinicId: string | null;
    positionName: string;
    startDate: string;
    endDate: string;
    monthlyAmount: number;
    status: RecordStatus;
    remark: string | null;
    rowVersion: string;
}
export type PositionFeeInput = Omit<PositionFeeDetail, 'id' | 'rowVersion'>;
const schema = z
    .object({
    doctorCodeId: z.string().min(1, 'โปรดระบุแพทย์'),
    clinicId: z.string().nullable(),
    positionName: z.string().trim().min(1, 'โปรดระบุตำแหน่ง').max(200),
    startDate: z.string().min(1, 'โปรดระบุวันที่เริ่มต้น'),
    endDate: z.string().min(1, 'โปรดระบุวันที่สิ้นสุด'),
    monthlyAmount: z
        .number({ invalid_type_error: 'โปรดระบุรายได้ (บาท/เดือน)' })
        .positive('โปรดระบุรายได้ (บาท/เดือน)'),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().max(1000).nullable(),
})
    .refine((v) => !v.startDate || !v.endDate || v.startDate <= v.endDate, {
    path: ['endDate'],
    message: 'โปรดระบุวันที่เริ่มต้น น้อยกว่าวันที่สิ้นสุด',
});
export const positionFeeScreen: ScreenDescriptor<PositionFeeListItem, PositionFeeDetail, PositionFeeInput> = {
    id: 'position-fee',
    resource: 'position-fees',
    path: '/doctor-fee-402/position-fee',
    titleTh: 'ค่าบริหาร / ตำแหน่ง',
    breadcrumb: BREADCRUMB,
    api: {
        resource: 'position-fees',
        list: (params, signal?: AbortSignal) => tenantApi<Paged<PositionFeeListItem>>('/api/master-data/position-fees', { params, signal }),
        get: (id, signal?: AbortSignal) => tenantApi<PositionFeeDetail>(`/api/master-data/position-fees/${id}`, { signal }),
        create: (input) => tenantApi<PositionFeeDetail>('/api/master-data/position-fees', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => tenantApi<PositionFeeDetail>(`/api/master-data/position-fees/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => tenantApi<void>(`/api/master-data/position-fees/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/position-fees/${id}/history`, { signal }),
        exportXlsx: (params) => tenantApiBlob('/api/master-data/position-fees/export', { params }),
    } satisfies CrudApi<PositionFeeListItem, PositionFeeDetail, PositionFeeInput>,
    columns: [
        { key: 'doctorCode', header: 'รหัสแพทย์', sortable: true, width: '130px' },
        { key: 'doctorName', header: 'แพทย์' },
        { key: 'positionName', header: 'ตำแหน่ง', sortable: true },
        { key: 'startDate', header: 'วันที่เริ่มต้น', sortable: true, format: 'date', width: '140px' },
        { key: 'endDate', header: 'วันที่สิ้นสุด', sortable: true, format: 'date', width: '140px' },
        {
            key: 'monthlyAmount',
            header: 'รายได้ (บาท/เดือน)',
            sortable: true,
            align: 'right',
            format: 'amount',
            width: '170px',
        },
        STATUS_COLUMN,
    ],
    filters: [
        { kind: 'lookup', name: 'doctorCodeId', label: 'แพทย์', resource: 'doctor-codes' },
        { kind: 'date', name: 'startDate', label: 'มีผลตั้งแต่' },
        { kind: 'date', name: 'endDate', label: 'มีผลถึง' },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหารหัสแพทย์ ชื่อแพทย์ หรือตำแหน่ง',
    defaultSort: '-startDate',
    rowKey: (row) => row.id,
    emptyHint: 'ค่าบริหารเป็นรายได้คงที่รายเดือนตามตำแหน่ง เดือนที่ทำงานไม่เต็มเดือนระบบคิดตามจำนวนวัน (หาร 30)',
    sections: [
        {
            title: 'รายละเอียด',
            fields: [
                {
                    kind: 'lookup',
                    name: 'doctorCodeId',
                    label: 'แพทย์',
                    resource: 'doctor-codes',
                    required: true,
                    width: 'lg',
                },
                { kind: 'lookup', name: 'clinicId', label: 'คลินิก', resource: 'clinics', emptyLabel: 'ไม่ระบุ' },
                { kind: 'text', name: 'positionName', label: 'ตำแหน่ง', required: true, maxLength: 200 },
                { kind: 'date', name: 'startDate', label: 'วันที่เริ่มต้น', required: true },
                { kind: 'date', name: 'endDate', label: 'วันที่สิ้นสุด', required: true },
                { kind: 'amount', name: 'monthlyAmount', label: 'รายได้ (บาท/เดือน)', required: true },
            ],
        },
        { title: 'อื่น ๆ', fields: [STATUS_FIELD, REMARK_FIELD] },
    ],
    schema: schema as never,
    defaultValues: {
        doctorCodeId: '',
        clinicId: null,
        positionName: '',
        startDate: '',
        endDate: '',
        monthlyAmount: 0,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => omitFields(detail, 'id', 'rowVersion'),
};
