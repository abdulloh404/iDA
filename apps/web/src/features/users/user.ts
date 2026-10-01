import { z } from 'zod';
import { coreApi, coreApiBlob } from '../../api/client';
import type { CrudApi } from '../../api/crud';
import type { AuditEntry, Paged, RecordStatus } from '../../api/types';
import type { SelectOption } from '../../components/form/Select';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD, omitFields } from '../master-data/descriptor';
import type { ChildTableDef, ScreenDescriptor } from '../master-data/descriptor';
import { UserAccountPanel } from './UserAccountPanel';
import { formatDateTime } from './format';
export const BREADCRUMB = [{ label: 'จัดการผู้ใช้' }] as const;
const AUTH_TYPE_OPTIONS: readonly SelectOption[] = [
    { value: 'AD', label: 'AD' },
    { value: 'LOCAL', label: 'Local' },
];
export interface UserListItem {
    id: string;
    username: string;
    displayName: string;
    employeeCode: string | null;
    email: string | null;
    mobile: string | null;
    authType: 'AD' | 'LOCAL';
    roles: string | null;
    lastLoginAt: string | null;
    status: RecordStatus;
}
export interface UserDetail {
    id: string;
    username: string;
    displayName: string;
    employeeCode: string | null;
    position: string | null;
    email: string | null;
    mobile: string | null;
    authType: 'AD' | 'LOCAL';
    status: RecordStatus;
    remark: string | null;
    rowVersion: string;
}
export type UserInput = Omit<UserDetail, 'id' | 'username' | 'rowVersion'>;
const optional = (max: number, label: string) => z.string().trim().max(max, `${label}ต้องไม่เกิน ${max} ตัวอักษร`).nullable();
const schema = z.object({
    displayName: z.string().trim().min(1, 'โปรดระบุชื่อ-นามสกุล').max(100, 'ชื่อ-นามสกุลต้องไม่เกิน 100 ตัวอักษร'),
    employeeCode: optional(10, 'รหัสพนักงาน'),
    position: optional(100, 'ตำแหน่ง'),
    email: z
        .string({ invalid_type_error: 'โปรดระบุอีเมล' })
        .trim()
        .min(1, 'โปรดระบุอีเมล')
        .max(100, 'อีเมลต้องไม่เกิน 100 ตัวอักษร')
        .regex(/^[^@\s]+@[^@\s]+\.[^@\s]+$/, 'รูปแบบอีเมลไม่ถูกต้อง'),
    mobile: z
        .string({ invalid_type_error: 'โปรดระบุเบอร์โทรศัพท์มือถือ' })
        .trim()
        .min(1, 'โปรดระบุเบอร์โทรศัพท์มือถือ')
        .regex(/^0\d{9}$/, 'เบอร์โทรศัพท์มือถือต้องเป็นตัวเลข 10 หลัก ขึ้นต้นด้วย 0'),
    authType: z.enum(['AD', 'LOCAL'], { errorMap: () => ({ message: 'โปรดระบุประเภทผู้ใช้งาน' }) }),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().max(1000).nullable(),
});
interface UserHospitalRoleRow {
    id: string;
    userId: string;
    hospitalId: string;
    hospitalName: string | null;
    roleId: string;
    roleName: string | null;
    isDefault: boolean;
    status: RecordStatus;
}
interface UserHospitalRoleInput {
    userId: string;
    hospitalId: string;
    roleId: string;
    isDefault: boolean;
    status: RecordStatus;
}
const hospitalRolesChild: ChildTableDef<UserHospitalRoleRow, UserHospitalRoleInput> = {
    title: 'สิทธิ์การใช้งานและสังกัดโรงพยาบาล',
    description: 'ผู้ใช้หนึ่งคนเข้าได้หลายโรงพยาบาล คนละสิทธิ์ — โรงพยาบาลเริ่มต้นคือที่ระบบเปิดให้หลังเข้าสู่ระบบ',
    api: {
        resource: 'user-hospital-roles',
        list: (params, signal?: AbortSignal) => coreApi<Paged<UserHospitalRoleRow>>('/api/master-data/user-hospital-roles', { params, signal }),
        get: (id, signal?: AbortSignal) => coreApi<UserHospitalRoleRow & { rowVersion: string; }>(`/api/master-data/user-hospital-roles/${id}`, { signal }),
        create: (input) => coreApi<UserHospitalRoleRow & { rowVersion: string; }>('/api/master-data/user-hospital-roles', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => coreApi<UserHospitalRoleRow & { rowVersion: string; }>(`/api/master-data/user-hospital-roles/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => coreApi<void>(`/api/master-data/user-hospital-roles/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => coreApi<AuditEntry[]>(`/api/master-data/user-hospital-roles/${id}/history`, { signal }),
        exportXlsx: (params) => coreApiBlob('/api/master-data/user-hospital-roles/export', { params }),
    } satisfies CrudApi<UserHospitalRoleRow, UserHospitalRoleRow & { rowVersion: string; }, UserHospitalRoleInput>,
    parentKey: 'userId',
    addLabel: 'เพิ่มสิทธิ์',
    emptyHint: 'ยังไม่มีสิทธิ์ที่โรงพยาบาลใด — ผู้ใช้จะเข้าสู่ระบบไม่ได้จนกว่าจะเพิ่มอย่างน้อยหนึ่งแถว',
    columns: [
        { key: 'hospitalName', header: 'สังกัดโรงพยาบาล', value: (r) => r.hospitalName ?? r.hospitalId },
        { key: 'roleName', header: 'สิทธิ์การใช้งาน' },
        {
            key: 'isDefault',
            header: 'โรงพยาบาลเริ่มต้น',
            width: '160px',
            value: (r) => (r.isDefault ? 'เริ่มต้น' : '—'),
        },
        STATUS_COLUMN,
    ],
    fields: [
        { kind: 'lookup', name: 'hospitalId', label: 'สังกัดโรงพยาบาล', resource: 'hospitals', required: true },
        { kind: 'lookup', name: 'roleId', label: 'สิทธิ์การใช้งาน', resource: 'roles', required: true },
        { kind: 'checkbox', name: 'isDefault', label: 'เปิดโรงพยาบาลนี้อัตโนมัติหลังเข้าสู่ระบบ' },
        STATUS_FIELD,
    ],
    schema: z.object({
        userId: z.string(),
        hospitalId: z.string().min(1, 'โปรดเลือกสังกัดโรงพยาบาล'),
        roleId: z.string().min(1, 'โปรดเลือกสิทธิ์การใช้งาน'),
        isDefault: z.boolean(),
        status: z.enum(['ACTIVE', 'INACTIVE']),
    }),
    defaultValues: { userId: '', hospitalId: '', roleId: '', isDefault: false, status: 'ACTIVE' },
    toInput: (row) => ({
        userId: row.userId,
        hospitalId: row.hospitalId,
        roleId: row.roleId,
        isDefault: row.isDefault,
        status: row.status,
    }),
    rowKey: (row) => row.id,
    defaultSort: 'hospitalId',
};
export const userScreen: ScreenDescriptor<UserListItem, UserDetail, UserInput> = {
    id: 'user',
    resource: 'users',
    path: '/users/user',
    titleTh: 'จัดการผู้ใช้งาน',
    breadcrumb: BREADCRUMB,
    api: {
        resource: 'users',
        list: (params, signal?: AbortSignal) => coreApi<Paged<UserListItem>>('/api/master-data/users', { params, signal }),
        get: (id, signal?: AbortSignal) => coreApi<UserDetail>(`/api/master-data/users/${id}`, { signal }),
        create: (input) => coreApi<UserDetail>('/api/master-data/users', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => coreApi<UserDetail>(`/api/master-data/users/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => coreApi<void>(`/api/master-data/users/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => coreApi<AuditEntry[]>(`/api/master-data/users/${id}/history`, { signal }),
        exportXlsx: (params) => coreApiBlob('/api/master-data/users/export', { params }),
    } satisfies CrudApi<UserListItem, UserDetail, UserInput>,
    columns: [
        { key: 'displayName', header: 'ชื่อ-นามสกุล', sortable: true },
        { key: 'employeeCode', header: 'รหัสพนักงาน', sortable: true, width: '130px' },
        { key: 'email', header: 'อีเมล', sortable: true },
        { key: 'roles', header: 'สิทธิ์การใช้งาน', value: (row) => row.roles || '—' },
        {
            key: 'authType',
            header: 'ประเภทผู้ใช้งาน',
            sortable: true,
            width: '140px',
            value: (row) => (row.authType === 'AD' ? 'AD' : 'Local'),
        },
        {
            key: 'lastLoginAt',
            header: 'เข้าใช้งานล่าสุด',
            sortable: true,
            width: '170px',
            value: (row) => (row.lastLoginAt ? formatDateTime(row.lastLoginAt) : '—'),
        },
        STATUS_COLUMN,
    ],
    filters: [
        { kind: 'lookup', name: 'roleId', label: 'สิทธิ์การใช้งาน', resource: 'roles' },
        { kind: 'select', name: 'authType', label: 'ประเภทผู้ใช้งาน', options: AUTH_TYPE_OPTIONS },
        { kind: 'lookup', name: 'hospitalId', label: 'สังกัดโรงพยาบาล', resource: 'hospitals' },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหาชื่อ รหัสพนักงาน อีเมล เบอร์มือถือ หรือชื่อบัญชี',
    defaultSort: 'displayName',
    rowKey: (row) => row.id,
    emptyHint: 'บัญชีผู้ใช้งานระบบของทั้งเครือ — หนึ่งบัญชีเข้าได้หลายโรงพยาบาล คนละสิทธิ์',
    sections: [
        {
            title: 'รายละเอียดข้อมูลผู้ใช้',
            fields: [
                { kind: 'text', name: 'displayName', label: 'ชื่อ-นามสกุล', required: true, maxLength: 100, width: 'lg' },
                { kind: 'text', name: 'employeeCode', label: 'รหัสพนักงาน', maxLength: 10 },
                { kind: 'text', name: 'position', label: 'ตำแหน่ง', maxLength: 100 },
                { kind: 'text', name: 'email', label: 'อีเมล', required: true, maxLength: 100 },
                { kind: 'text', name: 'mobile', label: 'เบอร์โทรศัพท์มือถือ', required: true, maxLength: 10 },
            ],
        },
        {
            title: 'ประเภทบัญชี',
            description: 'ชื่อบัญชีผู้ใช้ตั้งจากอีเมล (AD) หรือเบอร์มือถือ (Local) ตอนสร้าง และไม่เปลี่ยนตามภายหลัง',
            fields: [
                { kind: 'radio', name: 'authType', label: 'ประเภทผู้ใช้งาน', required: true, options: AUTH_TYPE_OPTIONS },
            ],
        },
        { title: 'อื่น ๆ', fields: [STATUS_FIELD, REMARK_FIELD] },
    ],
    schema: schema as never,
    defaultValues: {
        displayName: '',
        employeeCode: null,
        position: null,
        email: '',
        mobile: '',
        authType: 'AD',
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => omitFields(detail, 'id', 'username', 'rowVersion'),
    childTables: [hospitalRolesChild],
    formPanels: [UserAccountPanel],
};
