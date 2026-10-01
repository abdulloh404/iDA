import { z } from 'zod';
import { tenantApi, tenantApiBlob } from '../../../api/client';
import type { CrudApi } from '../../../api/crud';
import type { AuditEntry, Paged, RecordStatus } from '../../../api/types';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD } from '../descriptor';
import type { ScreenDescriptor } from '../descriptor';
export interface DoctorGroupListItem {
    id: string;
    code: string;
    nameTh: string;
    nameEn: string | null;
    doctorTypeCode: string | null;
    doctorTypeNameTh: string | null;
    status: RecordStatus;
}
export interface DoctorGroupDetail {
    id: string;
    code: string;
    doctorTypeId: string | null;
    nameTh: string;
    nameEn: string | null;
    status: RecordStatus;
    remark: string | null;
    rowVersion: string;
}
export interface DoctorGroupInput {
    code: string;
    doctorTypeId: string | null;
    nameTh: string;
    nameEn: string | null;
    status: RecordStatus;
    remark: string | null;
}
const doctorGroupSchema = z.object({
    code: z
        .string()
        .trim()
        .min(1, 'โปรดระบุรหัสกลุ่มแพทย์')
        .max(20, 'รหัสกลุ่มแพทย์ต้องไม่เกิน 20 ตัวอักษร'),
    doctorTypeId: z.string().nullable(),
    nameTh: z.string().trim().min(1, 'โปรดระบุรายละเอียดกลุ่มแพทย์ (ภาษาไทย)'),
    nameEn: z.string().trim().nullable(),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().nullable(),
});
export const doctorGroupScreen: ScreenDescriptor<DoctorGroupListItem, DoctorGroupDetail, DoctorGroupInput> = {
    id: 'doctor-group',
    resource: 'doctor-groups',
    path: '/master-data/general/doctor-group',
    titleTh: 'ข้อมูลกลุ่มแพทย์',
    breadcrumb: [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลักทั่วไป' }],
    api: {
        resource: 'doctor-groups',
        list: (params, signal?: AbortSignal) => tenantApi<Paged<DoctorGroupListItem>>('/api/master-data/doctor-groups', { params, signal }),
        get: (id, signal?: AbortSignal) => tenantApi<DoctorGroupDetail>(`/api/master-data/doctor-groups/${id}`, { signal }),
        create: (input) => tenantApi<DoctorGroupDetail>('/api/master-data/doctor-groups', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => tenantApi<DoctorGroupDetail>(`/api/master-data/doctor-groups/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => tenantApi<void>(`/api/master-data/doctor-groups/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/doctor-groups/${id}/history`, { signal }),
        exportXlsx: (params) => tenantApiBlob('/api/master-data/doctor-groups/export', { params }),
    } satisfies CrudApi<DoctorGroupListItem, DoctorGroupDetail, DoctorGroupInput>,
    columns: [
        { key: 'doctorTypeCode', header: 'รหัสประเภทแพทย์', sortable: true, width: '160px' },
        { key: 'doctorTypeNameTh', header: 'ประเภทแพทย์', width: '180px' },
        { key: 'code', header: 'รหัสกลุ่มแพทย์', sortable: true, width: '160px' },
        { key: 'nameTh', header: 'รายละเอียดกลุ่มแพทย์', sortable: true },
        STATUS_COLUMN,
    ],
    filters: [
        { kind: 'lookup', name: 'doctorTypeId', label: 'ประเภทแพทย์', resource: 'doctor-types' },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหารหัสหรือรายละเอียดกลุ่มแพทย์',
    defaultSort: 'code',
    rowKey: (row) => row.id,
    emptyHint: 'กลุ่มแพทย์ใช้จัดชุดเงื่อนไขส่วนแบ่งและอัตราค่าเวรของแพทย์หลายคนพร้อมกัน',
    sections: [
        {
            title: 'ข้อมูลทั่วไป',
            fields: [
                {
                    kind: 'lookup',
                    name: 'doctorTypeId',
                    label: 'ประเภทแพทย์',
                    resource: 'doctor-types',
                    emptyLabel: 'ไม่ระบุ',
                    width: 'lg',
                },
                {
                    kind: 'text',
                    name: 'code',
                    label: 'รหัสกลุ่มแพทย์',
                    required: true,
                    maxLength: 20,
                    immutableOnEdit: true,
                },
                {
                    kind: 'text',
                    name: 'nameTh',
                    label: 'รายละเอียดกลุ่มแพทย์ (ภาษาไทย)',
                    required: true,
                },
                { kind: 'text', name: 'nameEn', label: 'รายละเอียดกลุ่มแพทย์ (ภาษาอังกฤษ)' },
                STATUS_FIELD,
                REMARK_FIELD,
            ],
        },
    ],
    schema: doctorGroupSchema,
    defaultValues: {
        code: '',
        doctorTypeId: null,
        nameTh: '',
        nameEn: null,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => ({
        code: detail.code,
        doctorTypeId: detail.doctorTypeId,
        nameTh: detail.nameTh,
        nameEn: detail.nameEn,
        status: detail.status,
        remark: detail.remark,
    }),
};
