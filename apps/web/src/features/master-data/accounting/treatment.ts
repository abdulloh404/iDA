import { z } from 'zod';
import { tenantApi, tenantApiBlob } from '../../../api/client';
import type { CrudApi } from '../../../api/crud';
import type { AuditEntry, Paged, RecordStatus } from '../../../api/types';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD, omitFields, } from '../descriptor';
import type { ScreenDescriptor } from '../descriptor';
export interface TreatmentListItem {
    id: string;
    code: string;
    nameTh: string;
    nameEn: string | null;
    categoryCode: string | null;
    categoryNameTh: string | null;
    premiumRate: number | null;
    socialRate: number | null;
    unitPrice: number | null;
    requiresReading: boolean;
    status: RecordStatus;
}
export interface TreatmentDetail {
    id: string;
    code: string;
    treatmentCategoryId: string | null;
    nameTh: string;
    nameEn: string | null;
    premiumRate: number | null;
    socialRate: number | null;
    unitPrice: number | null;
    requiresReading: boolean;
    sourceSystem: string;
    syncedAt: string | null;
    status: RecordStatus;
    remark: string | null;
    rowVersion: string;
}
export type TreatmentInput = Omit<TreatmentDetail, 'id' | 'sourceSystem' | 'syncedAt' | 'rowVersion'>;
const percent = (label: string) => z
    .number()
    .min(0, `${label}ต้องอยู่ระหว่าง 0 ถึง 100`)
    .max(100, `${label}ต้องอยู่ระหว่าง 0 ถึง 100`)
    .nullable();
const treatmentSchema = z.object({
    code: z
        .string()
        .trim()
        .min(1, 'โปรดระบุรหัส Treatment')
        .max(40, 'รหัส Treatment ต้องไม่เกิน 40 ตัวอักษร'),
    treatmentCategoryId: z.string().nullable(),
    nameTh: z.string().trim().min(1, 'โปรดระบุชื่อ Treatment (ภาษาไทย)'),
    nameEn: z.string().trim().nullable(),
    premiumRate: percent('อัตราส่วนแบ่ง Premium'),
    socialRate: percent('อัตราส่วนแบ่งประกันสังคม'),
    unitPrice: z.number().min(0, 'ราคา Treatment ต้องไม่ติดลบ').nullable(),
    requiresReading: z.boolean(),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().nullable(),
});
export const treatmentScreen: ScreenDescriptor<TreatmentListItem, TreatmentDetail, TreatmentInput> = {
    id: 'treatment',
    resource: 'treatments',
    path: '/master-data/accounting/treatment',
    titleTh: 'ข้อมูล Treatment',
    breadcrumb: [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลักทางบัญชี' }],
    api: {
        resource: 'treatments',
        list: (params, signal?: AbortSignal) => tenantApi<Paged<TreatmentListItem>>('/api/master-data/treatments', { params, signal }),
        get: (id, signal?: AbortSignal) => tenantApi<TreatmentDetail>(`/api/master-data/treatments/${id}`, { signal }),
        create: (input) => tenantApi<TreatmentDetail>('/api/master-data/treatments', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => tenantApi<TreatmentDetail>(`/api/master-data/treatments/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => tenantApi<void>(`/api/master-data/treatments/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/treatments/${id}/history`, { signal }),
        exportXlsx: (params) => tenantApiBlob('/api/master-data/treatments/export', { params }),
    } satisfies CrudApi<TreatmentListItem, TreatmentDetail, TreatmentInput>,
    columns: [
        { key: 'code', header: 'รหัส Treatment', sortable: true, width: '180px' },
        { key: 'nameTh', header: 'ชื่อ Treatment', sortable: true },
        { key: 'categoryCode', header: 'Category', sortable: true, width: '140px' },
        {
            key: 'premiumRate',
            header: 'Premium (%)',
            sortable: true,
            align: 'right',
            format: 'amount',
            width: '130px',
        },
        {
            key: 'socialRate',
            header: 'ประกันสังคม (%)',
            sortable: true,
            align: 'right',
            format: 'amount',
            width: '150px',
        },
        {
            key: 'unitPrice',
            header: 'ราคา',
            sortable: true,
            align: 'right',
            format: 'amount',
            width: '130px',
        },
        STATUS_COLUMN,
    ],
    filters: [
        {
            kind: 'lookup',
            name: 'treatmentCategoryId',
            label: 'Treatment Category',
            resource: 'treatment-categories',
        },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหารหัสหรือชื่อ Treatment',
    defaultSort: 'code',
    rowKey: (row) => row.id,
    emptyHint: 'Treatment ปกติรับมาจาก HIS หากยังไม่ได้ซิงก์ ตารางนี้จะยังว่าง',
    sections: [
        {
            title: 'ข้อมูลทั่วไป',
            fields: [
                {
                    kind: 'text',
                    name: 'code',
                    label: 'รหัส Treatment',
                    required: true,
                    maxLength: 40,
                    immutableOnEdit: true,
                },
                {
                    kind: 'lookup',
                    name: 'treatmentCategoryId',
                    label: 'Treatment Category',
                    resource: 'treatment-categories',
                    emptyLabel: 'ไม่ระบุ',
                    width: 'lg',
                },
                { kind: 'text', name: 'nameTh', label: 'ชื่อ Treatment (ภาษาไทย)', required: true },
                { kind: 'text', name: 'nameEn', label: 'ชื่อ Treatment (ภาษาอังกฤษ)' },
                {
                    kind: 'bool',
                    name: 'requiresReading',
                    label: 'ต้องรออ่านผลก่อนจ่ายแพทย์',
                    trueLabel: 'ต้องรอ',
                    falseLabel: 'ไม่ต้องรอ',
                    hint: 'ใช้กับรายการอย่าง X-ray และ EKG',
                },
                STATUS_FIELD,
                REMARK_FIELD,
            ],
        },
        {
            title: 'อัตราและราคา',
            description: 'อัตราส่วนแบ่งเป็นเปอร์เซ็นต์ ปรับทีละหลายรายการได้จากไฟล์ Excel',
            fields: [
                { kind: 'amount', name: 'premiumRate', label: 'อัตราส่วนแบ่ง Premium (%)' },
                { kind: 'amount', name: 'socialRate', label: 'อัตราส่วนแบ่งประกันสังคม (%)' },
                { kind: 'amount', name: 'unitPrice', label: 'ราคา Treatment (บาท)' },
            ],
        },
    ],
    schema: treatmentSchema,
    defaultValues: {
        code: '',
        treatmentCategoryId: null,
        nameTh: '',
        nameEn: null,
        premiumRate: null,
        socialRate: null,
        unitPrice: null,
        requiresReading: false,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => omitFields(detail, 'id', 'sourceSystem', 'syncedAt', 'rowVersion'),
};
