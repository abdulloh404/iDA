import { z } from 'zod';
import { coreApi, coreApiBlob } from '../../../api/client';
import type { CrudApi } from '../../../api/crud';
import type { AuditEntry, Paged, RecordStatus } from '../../../api/types';
import type { ScreenDescriptor } from '../descriptor';
export interface SpecialtyListItem {
    id: string;
    code: string;
    nameTh: string;
    nameEn: string | null;
    status: RecordStatus;
    createdAt: string;
    createdBy: string;
    updatedAt: string;
    updatedBy: string;
}
export interface SpecialtyDetail extends SpecialtyListItem {
    displaySeq: number | null;
    remark: string | null;
    rowVersion: string;
}
export interface SpecialtyInput {
    code: string;
    nameTh: string;
    nameEn: string | null;
    displaySeq: number | null;
    status: RecordStatus;
    remark: string | null;
}
export const specialtyApi = {
    resource: 'specialties',
    list: (params, signal?: AbortSignal) => coreApi<Paged<SpecialtyListItem>>('/api/master-data/specialties', { params, signal }),
    get: (id, signal?: AbortSignal) => coreApi<SpecialtyDetail>(`/api/master-data/specialties/${id}`, { signal }),
    create: (input) => coreApi<SpecialtyDetail>('/api/master-data/specialties', { method: 'POST', body: input }),
    update: (id, input, rowVersion) => coreApi<SpecialtyDetail>(`/api/master-data/specialties/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
    remove: (id) => coreApi<void>(`/api/master-data/specialties/${id}`, { method: 'DELETE' }),
    history: (id, signal?: AbortSignal) => coreApi<AuditEntry[]>(`/api/master-data/specialties/${id}/history`, { signal }),
    exportXlsx: (params) => coreApiBlob('/api/master-data/specialties/export', { params }),
} satisfies CrudApi<SpecialtyListItem, SpecialtyDetail, SpecialtyInput>;
const specialtySchema = z.object({
    code: z
        .string()
        .trim()
        .min(1, 'โปรดระบุรหัสความเชี่ยวชาญ')
        .max(20, 'รหัสความเชี่ยวชาญต้องไม่เกิน 20 ตัวอักษร'),
    nameTh: z.string().trim().min(1, 'โปรดระบุชื่อความเชี่ยวชาญ (ภาษาไทย)'),
    nameEn: z.string().trim().nullable(),
    displaySeq: z.number().int().nullable(),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().nullable(),
});
export const specialtyScreen: ScreenDescriptor<SpecialtyListItem, SpecialtyDetail, SpecialtyInput> = {
    id: 'specialty',
    resource: 'specialties',
    path: '/master-data/general/specialty',
    titleTh: 'ความเชี่ยวชาญ',
    breadcrumb: [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลักทั่วไป' }],
    api: specialtyApi,
    columns: [
        { key: 'code', header: 'รหัสความเชี่ยวชาญ', sortable: true, width: '200px' },
        { key: 'nameTh', header: 'ความเชี่ยวชาญ', sortable: true },
        { key: 'nameEn', header: 'Specialty (EN)', sortable: true },
        { key: 'status', header: 'สถานะ', sortable: true, format: 'status', width: '140px' },
    ],
    filters: [{ kind: 'status', name: 'status', label: 'สถานะ' }],
    searchHint: 'ค้นหารหัสหรือชื่อความเชี่ยวชาญ',
    defaultSort: '-updatedAt',
    rowKey: (row) => row.id,
    emptyHint: 'ความเชี่ยวชาญเป็นรายการกลางที่ใช้ร่วมกันทุกโรงพยาบาลในเครือ',
    sections: [
        {
            title: 'ข้อมูลทั่วไป',
            fields: [
                {
                    kind: 'text',
                    name: 'code',
                    label: 'รหัสความเชี่ยวชาญ',
                    required: true,
                    maxLength: 20,
                    immutableOnEdit: true,
                },
                { kind: 'text', name: 'nameTh', label: 'ความเชี่ยวชาญ (ภาษาไทย)', required: true },
                { kind: 'text', name: 'nameEn', label: 'ความเชี่ยวชาญ (ภาษาอังกฤษ)' },
                { kind: 'number', name: 'displaySeq', label: 'ลำดับการแสดงผล', min: 0 },
                {
                    kind: 'switch',
                    name: 'status',
                    label: 'สถานะ',
                    onValue: 'ACTIVE',
                    offValue: 'INACTIVE',
                    onLabel: 'ใช้งาน',
                    offLabel: 'ไม่ใช้งาน',
                },
                { kind: 'textarea', name: 'remark', label: 'หมายเหตุ', rows: 3 },
            ],
        },
    ],
    schema: specialtySchema,
    defaultValues: {
        code: '',
        nameTh: '',
        nameEn: null,
        displaySeq: null,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => ({
        code: detail.code,
        nameTh: detail.nameTh,
        nameEn: detail.nameEn,
        displaySeq: detail.displaySeq,
        status: detail.status,
        remark: detail.remark,
    }),
};
