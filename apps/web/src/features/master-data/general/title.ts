import { z } from 'zod';
import { coreApi, coreApiBlob } from '../../../api/client';
import type { CrudApi } from '../../../api/crud';
import type { AuditEntry, Paged, RecordStatus } from '../../../api/types';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD } from '../descriptor';
import type { ScreenDescriptor } from '../descriptor';
export interface TitleListItem {
    id: string;
    code: string;
    nameTh: string;
    nameEn: string | null;
    status: RecordStatus;
}
export interface TitleDetail extends TitleListItem {
    remark: string | null;
    rowVersion: string;
}
export interface TitleInput {
    code: string;
    nameTh: string;
    nameEn: string | null;
    status: RecordStatus;
    remark: string | null;
}
const titleSchema = z.object({
    code: z
        .string()
        .trim()
        .min(1, 'โปรดระบุรหัสคำนำหน้าชื่อ')
        .max(20, 'รหัสคำนำหน้าชื่อต้องไม่เกิน 20 ตัวอักษร'),
    nameTh: z.string().trim().min(1, 'โปรดระบุคำนำหน้าชื่อ (ภาษาไทย)'),
    nameEn: z.string().trim().nullable(),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().nullable(),
});
export const titleScreen: ScreenDescriptor<TitleListItem, TitleDetail, TitleInput> = {
    id: 'title',
    resource: 'titles',
    path: '/master-data/general/title',
    titleTh: 'คำนำหน้าชื่อ',
    breadcrumb: [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลักทั่วไป' }],
    api: {
        resource: 'titles',
        list: (params, signal?: AbortSignal) => coreApi<Paged<TitleListItem>>('/api/master-data/titles', { params, signal }),
        get: (id, signal?: AbortSignal) => coreApi<TitleDetail>(`/api/master-data/titles/${id}`, { signal }),
        create: (input) => coreApi<TitleDetail>('/api/master-data/titles', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => coreApi<TitleDetail>(`/api/master-data/titles/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => coreApi<void>(`/api/master-data/titles/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => coreApi<AuditEntry[]>(`/api/master-data/titles/${id}/history`, { signal }),
        exportXlsx: (params) => coreApiBlob('/api/master-data/titles/export', { params }),
    } satisfies CrudApi<TitleListItem, TitleDetail, TitleInput>,
    columns: [
        { key: 'code', header: 'รหัสคำนำหน้าชื่อ', sortable: true, width: '200px' },
        { key: 'nameTh', header: 'คำนำหน้าชื่อ (ภาษาไทย)', sortable: true },
        { key: 'nameEn', header: 'คำนำหน้าชื่อ (ภาษาอังกฤษ)', sortable: true },
        STATUS_COLUMN,
    ],
    filters: [{ kind: 'status', name: 'status', label: 'สถานะ' }],
    searchHint: 'ค้นหารหัสหรือคำนำหน้าชื่อ',
    defaultSort: 'code',
    rowKey: (row) => row.id,
    emptyHint: 'คำนำหน้าชื่อเป็นรายการกลางที่ใช้ร่วมกันทุกโรงพยาบาลในเครือ',
    sections: [
        {
            title: 'ข้อมูลทั่วไป',
            fields: [
                {
                    kind: 'text',
                    name: 'code',
                    label: 'รหัสคำนำหน้าชื่อ',
                    required: true,
                    maxLength: 20,
                    immutableOnEdit: true,
                },
                { kind: 'text', name: 'nameTh', label: 'คำนำหน้าชื่อ (ภาษาไทย)', required: true },
                { kind: 'text', name: 'nameEn', label: 'คำนำหน้าชื่อ (ภาษาอังกฤษ)' },
                STATUS_FIELD,
                REMARK_FIELD,
            ],
        },
    ],
    schema: titleSchema,
    defaultValues: {
        code: '',
        nameTh: '',
        nameEn: null,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => ({
        code: detail.code,
        nameTh: detail.nameTh,
        nameEn: detail.nameEn,
        status: detail.status,
        remark: detail.remark,
    }),
};
