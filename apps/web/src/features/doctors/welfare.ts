import { z } from 'zod';
import { tenantApi, tenantApiBlob } from '../../api/client';
import type { CrudApi } from '../../api/crud';
import type { AuditEntry, Paged, RecordStatus } from '../../api/types';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD, omitFields } from '../master-data/descriptor';
import type { ChildTableDef, ScreenDescriptor } from '../master-data/descriptor';
import { WELFARE_SCOPE_OPTIONS } from './options';
const SCOPE_LABELS = new Map(WELFARE_SCOPE_OPTIONS.map((o) => [o.value, o.label]));
export interface WelfarePlanListItem {
    id: string;
    code: string;
    nameTh: string;
    nameEn: string | null;
    welfareScope: string;
    annualLimit: number;
    status: RecordStatus;
}
export interface WelfarePlanDetail extends WelfarePlanListItem {
    remark: string | null;
    rowVersion: string;
}
export type WelfarePlanInput = Omit<WelfarePlanDetail, 'id' | 'rowVersion'>;
export const welfarePlanScreen: ScreenDescriptor<WelfarePlanListItem, WelfarePlanDetail, WelfarePlanInput> = {
    id: 'welfare-plan',
    resource: 'welfare-plans',
    path: '/doctors/welfare-plan',
    titleTh: 'แผนสวัสดิการแพทย์',
    breadcrumb: [{ label: 'จัดการข้อมูลแพทย์' }],
    api: {
        resource: 'welfare-plans',
        list: (params, signal?: AbortSignal) => tenantApi<Paged<WelfarePlanListItem>>('/api/master-data/welfare-plans', { params, signal }),
        get: (id, signal?: AbortSignal) => tenantApi<WelfarePlanDetail>(`/api/master-data/welfare-plans/${id}`, { signal }),
        create: (input) => tenantApi<WelfarePlanDetail>('/api/master-data/welfare-plans', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => tenantApi<WelfarePlanDetail>(`/api/master-data/welfare-plans/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => tenantApi<void>(`/api/master-data/welfare-plans/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/welfare-plans/${id}/history`, { signal }),
        exportXlsx: (params) => tenantApiBlob('/api/master-data/welfare-plans/export', { params }),
    } satisfies CrudApi<WelfarePlanListItem, WelfarePlanDetail, WelfarePlanInput>,
    columns: [
        { key: 'code', header: 'รหัสแผน', sortable: true, width: '160px' },
        { key: 'nameTh', header: 'ชื่อแผนสวัสดิการ', sortable: true },
        {
            key: 'welfareScope',
            header: 'ขอบเขต',
            sortable: true,
            width: '230px',
            value: (row) => SCOPE_LABELS.get(row.welfareScope) ?? row.welfareScope,
        },
        {
            key: 'annualLimit',
            header: 'วงเงินต่อปี (บาท)',
            sortable: true,
            align: 'right',
            format: 'amount',
            width: '190px',
        },
        STATUS_COLUMN,
    ],
    filters: [
        { kind: 'select', name: 'welfareScope', label: 'ขอบเขต', options: WELFARE_SCOPE_OPTIONS },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหารหัสหรือชื่อแผนสวัสดิการ',
    defaultSort: 'code',
    rowKey: (row) => row.id,
    emptyHint: 'แผนสวัสดิการกำหนดวงเงินรักษาพยาบาลต่อปีของแพทย์ในโรงพยาบาลนี้',
    sections: [
        {
            title: 'ข้อมูลแผน',
            fields: [
                {
                    kind: 'text',
                    name: 'code',
                    label: 'รหัสแผนสวัสดิการ',
                    required: true,
                    maxLength: 20,
                    immutableOnEdit: true,
                },
                { kind: 'text', name: 'nameTh', label: 'ชื่อแผนสวัสดิการ', required: true },
                { kind: 'text', name: 'nameEn', label: 'ชื่อแผนสวัสดิการ (ภาษาอังกฤษ)' },
                {
                    kind: 'select',
                    name: 'welfareScope',
                    label: 'ขอบเขตสวัสดิการ',
                    required: true,
                    options: WELFARE_SCOPE_OPTIONS,
                    width: 'lg',
                },
                {
                    kind: 'amount',
                    name: 'annualLimit',
                    label: 'วงเงินการรักษา (บาท/ปี)',
                    required: true,
                    hint: 'แผนที่ไม่มีสวัสดิการจะถูกบันทึกเป็นศูนย์เสมอ',
                },
                STATUS_FIELD,
                REMARK_FIELD,
            ],
        },
    ],
    schema: z.object({
        code: z
            .string()
            .trim()
            .min(1, 'โปรดระบุรหัสแผนสวัสดิการ')
            .max(20, 'รหัสแผนสวัสดิการต้องไม่เกิน 20 ตัวอักษร'),
        nameTh: z.string().trim().min(1, 'โปรดระบุชื่อแผนสวัสดิการ'),
        nameEn: z.string().trim().nullable(),
        welfareScope: z.string().min(1, 'โปรดเลือกขอบเขตสวัสดิการ'),
        annualLimit: z.number().min(0, 'วงเงินต่อปีต้องไม่ติดลบ'),
        status: z.enum(['ACTIVE', 'INACTIVE']),
        remark: z.string().trim().nullable(),
    }),
    defaultValues: {
        code: '',
        nameTh: '',
        nameEn: null,
        welfareScope: 'DOCTOR_ONLY',
        annualLimit: 0,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => omitFields(detail, 'id', 'rowVersion'),
};
export interface WelfareUsageRow {
    id: number;
    welfareId: string;
    patientHn: string;
    patientName: string | null;
    relationName: string | null;
    visitDate: string;
    invoiceNo: string | null;
    usedAmount: number;
    sourceSystem: string | null;
}
const welfareUsagesChild: ChildTableDef<WelfareUsageRow, Record<string, never>> = {
    title: 'รายการใช้สิทธิ์',
    description: 'ข้อมูลจากระบบ HIS — ยอดที่ใช้ไปของวงเงินคำนวณจากรายการเหล่านี้',
    api: {
        resource: 'doctor-welfare-usages',
        list: (params, signal?: AbortSignal) => tenantApi<Paged<WelfareUsageRow>>('/api/master-data/doctor-welfare-usages', { params, signal }),
        get: (id, signal?: AbortSignal) => tenantApi<WelfareUsageRow & { rowVersion: string; }>(`/api/master-data/doctor-welfare-usages/${id}`, { signal }),
        create: (input) => tenantApi<WelfareUsageRow & { rowVersion: string; }>('/api/master-data/doctor-welfare-usages', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => tenantApi<WelfareUsageRow & { rowVersion: string; }>(`/api/master-data/doctor-welfare-usages/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => tenantApi<void>(`/api/master-data/doctor-welfare-usages/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/doctor-welfare-usages/${id}/history`, { signal }),
        exportXlsx: (params) => tenantApiBlob('/api/master-data/doctor-welfare-usages/export', { params }),
    } satisfies CrudApi<WelfareUsageRow, WelfareUsageRow & { rowVersion: string; }, Record<string, never>>,
    parentKey: 'welfareId',
    rowKey: (row) => String(row.id),
    readOnly: true,
    columns: [
        { key: 'visitDate', header: 'วันที่รักษา', format: 'date', width: '140px' },
        { key: 'patientHn', header: 'H.N.', width: '130px' },
        { key: 'patientName', header: 'ผู้ใช้สิทธิ์' },
        { key: 'relationName', header: 'ความสัมพันธ์', width: '150px' },
        { key: 'invoiceNo', header: 'เลขที่ Invoice', width: '170px' },
        {
            key: 'usedAmount',
            header: 'จำนวนเงิน (บาท)',
            align: 'right',
            format: 'amount',
            width: '170px',
        },
    ],
    fields: [],
    schema: z.object({}),
    defaultValues: {},
    toInput: () => ({}),
    emptyHint: 'ยังไม่มีการใช้สิทธิ์สวัสดิการในปีนี้',
};
export interface DoctorWelfareListItem {
    id: string;
    doctorId: string;
    doctorName: string | null;
    doctorGlobalCode: string | null;
    planName: string | null;
    welfareScope: string;
    welfareYear: number;
    annualLimit: number;
    usedAmount: number;
    remainingAmount: number | null;
    status: RecordStatus;
}
export interface DoctorWelfareDetail {
    id: string;
    doctorId: string;
    welfarePlanId: string;
    welfareYear: number;
    annualLimit: number;
    usedAmount: number;
    remainingAmount: number | null;
    documentUrl: string | null;
    status: RecordStatus;
    remark: string | null;
    rowVersion: string;
}
export type DoctorWelfareInput = Omit<DoctorWelfareDetail, 'id' | 'rowVersion' | 'usedAmount' | 'remainingAmount'>;
export const doctorWelfareScreen: ScreenDescriptor<DoctorWelfareListItem, DoctorWelfareDetail, DoctorWelfareInput> = {
    id: 'doctor-welfare',
    resource: 'doctor-welfares',
    path: '/doctors/welfare',
    titleTh: 'สวัสดิการแพทย์',
    breadcrumb: [{ label: 'จัดการข้อมูลแพทย์' }],
    api: {
        resource: 'doctor-welfares',
        list: (params, signal?: AbortSignal) => tenantApi<Paged<DoctorWelfareListItem>>('/api/master-data/doctor-welfares', { params, signal }),
        get: (id, signal?: AbortSignal) => tenantApi<DoctorWelfareDetail>(`/api/master-data/doctor-welfares/${id}`, { signal }),
        create: (input) => tenantApi<DoctorWelfareDetail>('/api/master-data/doctor-welfares', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => tenantApi<DoctorWelfareDetail>(`/api/master-data/doctor-welfares/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => tenantApi<void>(`/api/master-data/doctor-welfares/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/doctor-welfares/${id}/history`, { signal }),
        exportXlsx: (params) => tenantApiBlob('/api/master-data/doctor-welfares/export', { params }),
    } satisfies CrudApi<DoctorWelfareListItem, DoctorWelfareDetail, DoctorWelfareInput>,
    columns: [
        { key: 'doctorGlobalCode', header: 'รหัสแพทย์กลาง', sortable: true, width: '170px' },
        { key: 'doctorName', header: 'แพทย์' },
        { key: 'planName', header: 'แผนสวัสดิการ', width: '200px' },
        {
            key: 'welfareScope',
            header: 'ขอบเขต',
            width: '210px',
            value: (row) => SCOPE_LABELS.get(row.welfareScope) ?? row.welfareScope,
        },
        { key: 'welfareYear', header: 'ปี (ค.ศ.)', sortable: true, align: 'right', width: '120px' },
        {
            key: 'annualLimit',
            header: 'วงเงิน (บาท)',
            sortable: true,
            align: 'right',
            format: 'amount',
            width: '170px',
        },
        {
            key: 'usedAmount',
            header: 'ใช้ไปแล้ว (บาท)',
            align: 'right',
            format: 'amount',
            width: '170px',
        },
        {
            key: 'remainingAmount',
            header: 'คงเหลือ (บาท)',
            sortable: true,
            align: 'right',
            format: 'amount',
            width: '170px',
        },
        STATUS_COLUMN,
    ],
    filters: [
        { kind: 'lookup', name: 'doctorId', label: 'แพทย์', resource: 'doctors' },
        { kind: 'select', name: 'welfareScope', label: 'สวัสดิการ', options: WELFARE_SCOPE_OPTIONS },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหารหัสแพทย์กลางหรือชื่อแพทย์',
    defaultSort: '-welfareYear',
    rowKey: (row) => row.id,
    emptyHint: 'วงเงินสวัสดิการกำหนดเป็นรายปีต่อแพทย์หนึ่งคน',
    sections: [
        {
            title: 'วงเงินรายปี',
            description: 'ยอดที่ใช้ไปและยอดคงเหลือคำนวณจากรายการใช้สิทธิ์ จึงไม่มีช่องให้กรอก',
            fields: [
                {
                    kind: 'lookup',
                    name: 'doctorId',
                    label: 'แพทย์',
                    resource: 'doctors',
                    required: true,
                    width: 'lg',
                },
                {
                    kind: 'lookup',
                    name: 'welfarePlanId',
                    label: 'แผนสวัสดิการ',
                    resource: 'welfare-plans',
                    required: true,
                    width: 'lg',
                },
                { kind: 'number', name: 'welfareYear', label: 'ปี (ค.ศ.)', required: true, min: 2000 },
                { kind: 'amount', name: 'annualLimit', label: 'วงเงินการรักษา (บาท)', required: true },
                { kind: 'text', name: 'documentUrl', label: 'ไฟล์เอกสาร', width: 'lg' },
                STATUS_FIELD,
                REMARK_FIELD,
            ],
        },
    ],
    schema: z.object({
        doctorId: z.string().min(1, 'โปรดเลือกแพทย์'),
        welfarePlanId: z.string().min(1, 'โปรดเลือกแผนสวัสดิการ'),
        welfareYear: z
            .number()
            .int()
            .min(2000, 'ปีต้องเป็น ค.ศ. ระหว่าง 2000 ถึง 2100')
            .max(2100, 'ปีต้องเป็น ค.ศ. ระหว่าง 2000 ถึง 2100'),
        annualLimit: z.number().min(0, 'วงเงินการรักษาต้องไม่ติดลบ'),
        documentUrl: z.string().trim().nullable(),
        status: z.enum(['ACTIVE', 'INACTIVE']),
        remark: z.string().trim().nullable(),
    }),
    defaultValues: {
        doctorId: '',
        welfarePlanId: '',
        welfareYear: new Date().getFullYear(),
        annualLimit: 0,
        documentUrl: null,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => omitFields(detail, 'id', 'rowVersion', 'usedAmount', 'remainingAmount'),
    childTables: [welfareUsagesChild],
};
