import { z } from 'zod';
import { tenantApi, tenantApiBlob } from '../../../api/client';
import type { CrudApi } from '../../../api/crud';
import type { AuditEntry, Paged, RecordStatus } from '../../../api/types';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD } from '../descriptor';
import type { ScreenDescriptor } from '../descriptor';
export interface TreatmentCategoryListItem {
    id: string;
    code: string;
    nameTh: string;
    nameEn: string | null;
    isPackage: boolean;
    sourceSystem: string;
    status: RecordStatus;
}
export interface TreatmentCategoryDetail extends TreatmentCategoryListItem {
    syncedAt: string | null;
    remark: string | null;
    rowVersion: string;
}
export interface TreatmentCategoryInput {
    code: string;
    nameTh: string;
    nameEn: string | null;
    isPackage: boolean;
    status: RecordStatus;
    remark: string | null;
}
const treatmentCategorySchema = z.object({
    code: z
        .string()
        .trim()
        .min(1, 'โปรดระบุรหัส Treatment Category')
        .max(20, 'รหัส Treatment Category ต้องไม่เกิน 20 ตัวอักษร'),
    nameTh: z.string().trim().min(1, 'โปรดระบุรายละเอียด (ภาษาไทย)'),
    nameEn: z.string().trim().nullable(),
    isPackage: z.boolean(),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().nullable(),
});
export const treatmentCategoryScreen: ScreenDescriptor<TreatmentCategoryListItem, TreatmentCategoryDetail, TreatmentCategoryInput> = {
    id: 'treatment-category',
    resource: 'treatment-categories',
    path: '/master-data/accounting/treatment-category',
    titleTh: 'ข้อมูล Treatment Category',
    breadcrumb: [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลักทางบัญชี' }],
    api: {
        resource: 'treatment-categories',
        list: (params, signal?: AbortSignal) => tenantApi<Paged<TreatmentCategoryListItem>>('/api/master-data/treatment-categories', { params, signal }),
        get: (id, signal?: AbortSignal) => tenantApi<TreatmentCategoryDetail>(`/api/master-data/treatment-categories/${id}`, { signal }),
        create: (input) => tenantApi<TreatmentCategoryDetail>('/api/master-data/treatment-categories', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => tenantApi<TreatmentCategoryDetail>(`/api/master-data/treatment-categories/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => tenantApi<void>(`/api/master-data/treatment-categories/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/treatment-categories/${id}/history`, { signal }),
        exportXlsx: (params) => tenantApiBlob('/api/master-data/treatment-categories/export', { params }),
    } satisfies CrudApi<TreatmentCategoryListItem, TreatmentCategoryDetail, TreatmentCategoryInput>,
    columns: [
        { key: 'code', header: 'รหัส Category', sortable: true, width: '180px' },
        { key: 'nameTh', header: 'รายละเอียด (ภาษาไทย)', sortable: true },
        { key: 'nameEn', header: 'รายละเอียด (ภาษาอังกฤษ)', sortable: true },
        {
            key: 'isPackage',
            header: 'Package',
            sortable: true,
            width: '120px',
            value: (row) => (row.isPackage ? 'ใช่' : 'ไม่ใช่'),
        },
        { key: 'sourceSystem', header: 'ที่มา', width: '110px' },
        STATUS_COLUMN,
    ],
    filters: [
        {
            kind: 'select',
            name: 'isPackage',
            label: 'Package',
            options: [
                { value: 'true', label: 'เป็น Package' },
                { value: 'false', label: 'ไม่เป็น Package' },
            ],
        },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหารหัสหรือรายละเอียด Category',
    defaultSort: 'code',
    rowKey: (row) => row.id,
    emptyHint: 'Treatment Category ปกติรับมาจาก HIS หากยังไม่ได้ซิงก์ ตารางนี้จะยังว่าง',
    sections: [
        {
            title: 'ข้อมูลทั่วไป',
            fields: [
                {
                    kind: 'text',
                    name: 'code',
                    label: 'รหัส Treatment Category',
                    required: true,
                    maxLength: 20,
                    immutableOnEdit: true,
                },
                { kind: 'text', name: 'nameTh', label: 'รายละเอียด (ภาษาไทย)', required: true },
                { kind: 'text', name: 'nameEn', label: 'รายละเอียด (ภาษาอังกฤษ)' },
                {
                    kind: 'bool',
                    name: 'isPackage',
                    label: 'Package',
                    trueLabel: 'ใช่',
                    falseLabel: 'ไม่ใช่',
                    hint: 'มีผลต่อการคิดส่วนแบ่งระดับ Package',
                },
                STATUS_FIELD,
                REMARK_FIELD,
            ],
        },
    ],
    schema: treatmentCategorySchema,
    defaultValues: {
        code: '',
        nameTh: '',
        nameEn: null,
        isPackage: false,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => ({
        code: detail.code,
        nameTh: detail.nameTh,
        nameEn: detail.nameEn,
        isPackage: detail.isPackage,
        status: detail.status,
        remark: detail.remark,
    }),
};
