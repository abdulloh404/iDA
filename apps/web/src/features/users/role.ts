import { z } from 'zod';
import { coreApi, coreApiBlob } from '../../api/client';
import type { CrudApi } from '../../api/crud';
import type { AuditEntry, Paged, RecordStatus } from '../../api/types';
import { STATUS_COLUMN, STATUS_FIELD } from '../master-data/descriptor';
import type { ScreenDescriptor } from '../master-data/descriptor';
import { BREADCRUMB } from './user';
import { RolePermissionPanel } from './RolePermissionPanel';
export interface RoleListItem {
    id: string;
    code: string;
    nameTh: string;
    isGroupLevel: boolean;
    isSystem: boolean;
    permissionCount: number;
    status: RecordStatus;
    updatedAt: string;
}
export interface RoleDetail {
    id: string;
    code: string;
    nameTh: string;
    nameEn: string | null;
    description: string | null;
    isGroupLevel: boolean;
    isSystem: boolean;
    status: RecordStatus;
    rowVersion: string;
}
export interface RoleInput {
    code: string;
    nameTh: string;
    nameEn: string | null;
    description: string | null;
    isGroupLevel: boolean;
    status: RecordStatus;
}
const schema = z.object({
    code: z
        .string()
        .trim()
        .min(1, 'โปรดระบุรหัสสิทธิ์การใช้งาน')
        .max(20, 'รหัสสิทธิ์การใช้งานต้องไม่เกิน 20 ตัวอักษร')
        .regex(/^[A-Za-z0-9_]+$/, 'รหัสสิทธิ์การใช้งานใช้ได้เฉพาะตัวอักษรอังกฤษ ตัวเลข และ _'),
    nameTh: z.string().trim().min(1, 'โปรดระบุสิทธิ์การใช้งาน').max(100),
    nameEn: z.string().trim().max(100).nullable(),
    description: z.string().trim().max(1000).nullable(),
    isGroupLevel: z.boolean(),
    status: z.enum(['ACTIVE', 'INACTIVE']),
});
export const roleScreen: ScreenDescriptor<RoleListItem, RoleDetail, RoleInput> = {
    id: 'role',
    resource: 'roles',
    path: '/users/role',
    titleTh: 'จัดการสิทธิ์',
    breadcrumb: BREADCRUMB,
    api: {
        resource: 'roles',
        list: (params, signal?: AbortSignal) => coreApi<Paged<RoleListItem>>('/api/master-data/roles', { params, signal }),
        get: (id, signal?: AbortSignal) => coreApi<RoleDetail>(`/api/master-data/roles/${id}`, { signal }),
        create: (input) => coreApi<RoleDetail>('/api/master-data/roles', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => coreApi<RoleDetail>(`/api/master-data/roles/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => coreApi<void>(`/api/master-data/roles/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => coreApi<AuditEntry[]>(`/api/master-data/roles/${id}/history`, { signal }),
        exportXlsx: (params) => coreApiBlob('/api/master-data/roles/export', { params }),
    } satisfies CrudApi<RoleListItem, RoleDetail, RoleInput>,
    columns: [
        { key: 'nameTh', header: 'สิทธิ์การใช้งาน', sortable: true },
        { key: 'code', header: 'รหัส', sortable: true, width: '190px' },
        {
            key: 'isGroupLevel',
            header: 'ระดับ',
            width: '150px',
            value: (row) => (row.isGroupLevel ? 'ระดับเครือ' : 'ระดับโรงพยาบาล'),
        },
        {
            key: 'permissionCount',
            header: 'จำนวนสิทธิ์',
            align: 'right',
            width: '130px',
            value: (row) => row.permissionCount.toLocaleString('th-TH'),
        },
        STATUS_COLUMN,
        {
            key: 'updatedAt',
            header: 'วันที่แก้ไขล่าสุด',
            sortable: true,
            format: 'date',
            width: '160px',
        },
    ],
    filters: [{ kind: 'status', name: 'status', label: 'สถานะ' }],
    searchHint: 'ค้นหาชื่อหรือรหัสสิทธิ์การใช้งาน',
    defaultSort: 'nameTh',
    rowKey: (row) => row.id,
    emptyHint: 'ชุดสิทธิ์ที่ผูกให้ผู้ใช้ในแต่ละโรงพยาบาล — กำหนดว่าเห็นเมนูไหน และสร้าง แก้ไข ลบได้แค่ไหน',
    sections: [
        {
            title: 'รายละเอียด',
            fields: [
                { kind: 'text', name: 'nameTh', label: 'สิทธิ์การใช้งาน', required: true, maxLength: 100, width: 'lg' },
                { kind: 'text', name: 'code', label: 'รหัส', required: true, maxLength: 20, immutableOnEdit: true },
                { kind: 'text', name: 'nameEn', label: 'ชื่อภาษาอังกฤษ', maxLength: 100 },
                {
                    kind: 'bool',
                    name: 'isGroupLevel',
                    label: 'ระดับ',
                    trueLabel: 'ระดับเครือ',
                    falseLabel: 'ระดับโรงพยาบาล',
                    hint: 'ระดับเครือแก้ข้อมูลหลักที่ใช้ร่วมกันทุกโรงพยาบาลได้ · กำหนดได้ตอนสร้างเท่านั้น',
                },
            ],
        },
        {
            title: 'อื่น ๆ',
            fields: [STATUS_FIELD, { kind: 'textarea', name: 'description', label: 'หมายเหตุ', rows: 3 }],
        },
    ],
    schema: schema as never,
    defaultValues: {
        code: '',
        nameTh: '',
        nameEn: null,
        description: null,
        isGroupLevel: false,
        status: 'ACTIVE',
    },
    toInput: (detail) => ({
        code: detail.code,
        nameTh: detail.nameTh,
        nameEn: detail.nameEn,
        description: detail.description,
        isGroupLevel: detail.isGroupLevel,
        status: detail.status,
    }),
    formPanels: [RolePermissionPanel],
};
