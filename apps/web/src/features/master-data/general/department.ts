import { z } from 'zod';
import { tenantApi, tenantApiBlob } from '../../../api/client';
import type { CrudApi } from '../../../api/crud';
import type { AuditEntry, Paged, RecordStatus } from '../../../api/types';
import type { ScreenDescriptor } from '../descriptor';
export interface DepartmentListItem {
    id: string;
    code: string;
    nameTh: string;
    nameEn: string | null;
    costCenter: string | null;
    status: RecordStatus;
}
export interface DepartmentDetail extends DepartmentListItem {
    remark: string | null;
    rowVersion: string;
}
export interface DepartmentInput {
    code: string;
    nameTh: string;
    nameEn: string | null;
    costCenter: string | null;
    status: RecordStatus;
    remark: string | null;
}
export const departmentApi = {
    resource: 'departments',
    list: (params, signal?: AbortSignal) => tenantApi<Paged<DepartmentListItem>>('/api/master-data/departments', { params, signal }),
    get: (id, signal?: AbortSignal) => tenantApi<DepartmentDetail>(`/api/master-data/departments/${id}`, { signal }),
    create: (input) => tenantApi<DepartmentDetail>('/api/master-data/departments', { method: 'POST', body: input }),
    update: (id, input, rowVersion) => tenantApi<DepartmentDetail>(`/api/master-data/departments/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
    remove: (id) => tenantApi<void>(`/api/master-data/departments/${id}`, { method: 'DELETE' }),
    history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/departments/${id}/history`, { signal }),
    exportXlsx: (params) => tenantApiBlob('/api/master-data/departments/export', { params }),
} satisfies CrudApi<DepartmentListItem, DepartmentDetail, DepartmentInput>;
const departmentSchema = z.object({
    code: z
        .string()
        .trim()
        .min(1, 'โปรดระบุรหัสแผนก')
        .max(20, 'รหัสแผนกต้องไม่เกิน 20 ตัวอักษร'),
    nameTh: z.string().trim().min(1, 'โปรดระบุชื่อแผนก'),
    nameEn: z.string().trim().nullable(),
    costCenter: z.string().trim().nullable(),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().nullable(),
});
export const departmentScreen: ScreenDescriptor<DepartmentListItem, DepartmentDetail, DepartmentInput> = {
    id: 'department',
    resource: 'departments',
    path: '/master-data/general/department',
    titleTh: 'แผนกตามศูนย์รายได้ค่าใช้จ่าย',
    breadcrumb: [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลักทั่วไป' }],
    api: departmentApi,
    columns: [
        { key: 'code', header: 'รหัสแผนก', sortable: true, width: '180px' },
        { key: 'nameTh', header: 'ชื่อแผนก', sortable: true },
        { key: 'nameEn', header: 'Department (EN)', sortable: true },
        { key: 'costCenter', header: 'ศูนย์รายได้/ค่าใช้จ่าย', sortable: true, width: '200px' },
        { key: 'status', header: 'สถานะ', sortable: true, format: 'status', width: '140px' },
    ],
    filters: [{ kind: 'status', name: 'status', label: 'สถานะ' }],
    searchHint: 'ค้นหารหัส ชื่อ หรือศูนย์รายได้/ค่าใช้จ่าย',
    defaultSort: 'code',
    rowKey: (row) => row.id,
    emptyHint: 'แผนกเป็นข้อมูลเฉพาะของแต่ละโรงพยาบาล ข้อมูลของโรงพยาบาลอื่นจะไม่แสดงที่นี่',
    sections: [
        {
            title: 'ข้อมูลทั่วไป',
            fields: [
                {
                    kind: 'text',
                    name: 'code',
                    label: 'รหัสแผนก',
                    required: true,
                    maxLength: 20,
                    immutableOnEdit: true,
                },
                { kind: 'text', name: 'nameTh', label: 'ชื่อแผนก (ภาษาไทย)', required: true },
                { kind: 'text', name: 'nameEn', label: 'ชื่อแผนก (ภาษาอังกฤษ)' },
                { kind: 'text', name: 'costCenter', label: 'ศูนย์รายได้/ค่าใช้จ่าย' },
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
    schema: departmentSchema,
    defaultValues: {
        code: '',
        nameTh: '',
        nameEn: null,
        costCenter: null,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => ({
        code: detail.code,
        nameTh: detail.nameTh,
        nameEn: detail.nameEn,
        costCenter: detail.costCenter,
        status: detail.status,
        remark: detail.remark,
    }),
};
