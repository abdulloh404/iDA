import { tenantApi, tenantApiBlob } from '../../../api/client';
import type { AuditEntry, Paged } from '../../../api/types';
import type { MasterDetail, MasterListItem } from '../simple-master';
import { simpleMasterScreen } from '../simple-master';
const BREADCRUMB = [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลักทั่วไป' }] as const;
export const doctorTypeScreen = simpleMasterScreen({
    id: 'doctor-type',
    resource: 'doctor-types',
    api: {
        resource: 'doctor-types',
        list: (params, signal?: AbortSignal) => tenantApi<Paged<MasterListItem>>('/api/master-data/doctor-types', { params, signal }),
        get: (id, signal?: AbortSignal) => tenantApi<MasterDetail>(`/api/master-data/doctor-types/${id}`, { signal }),
        create: (input) => tenantApi<MasterDetail>('/api/master-data/doctor-types', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => tenantApi<MasterDetail>(`/api/master-data/doctor-types/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => tenantApi<void>(`/api/master-data/doctor-types/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/doctor-types/${id}/history`, { signal }),
        exportXlsx: (params) => tenantApiBlob('/api/master-data/doctor-types/export', { params }),
    },
    path: '/master-data/general/doctor-type',
    titleTh: 'ประเภทแพทย์',
    breadcrumb: BREADCRUMB,
    codeLabel: 'รหัสประเภทแพทย์',
    nameLabel: 'ประเภทแพทย์',
    emptyHint: 'ประเภทแพทย์ เช่น AS แพทย์ประจำ CS แพทย์ที่ปรึกษา PT Part Time — แต่ละโรงพยาบาลกำหนดเอง',
});
export const statusPrivilegeScreen = simpleMasterScreen({
    id: 'status-privilege',
    resource: 'status-privileges',
    api: {
        resource: 'status-privileges',
        list: (params, signal?: AbortSignal) => tenantApi<Paged<MasterListItem>>('/api/master-data/status-privileges', { params, signal }),
        get: (id, signal?: AbortSignal) => tenantApi<MasterDetail>(`/api/master-data/status-privileges/${id}`, { signal }),
        create: (input) => tenantApi<MasterDetail>('/api/master-data/status-privileges', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => tenantApi<MasterDetail>(`/api/master-data/status-privileges/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => tenantApi<void>(`/api/master-data/status-privileges/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/status-privileges/${id}/history`, { signal }),
        exportXlsx: (params) => tenantApiBlob('/api/master-data/status-privileges/export', { params }),
    },
    path: '/master-data/general/status-privilege',
    titleTh: 'ข้อมูล Status Privilege',
    breadcrumb: BREADCRUMB,
    codeLabel: 'รหัส Privilege',
    nameLabel: 'Privilege',
    emptyHint: 'สถานะสิทธิ์การทำหัตถการของแพทย์ เช่น Full, Provisional, Temporary',
});
export const privilegeTypeScreen = simpleMasterScreen({
    id: 'privilege-type',
    resource: 'privilege-types',
    api: {
        resource: 'privilege-types',
        list: (params, signal?: AbortSignal) => tenantApi<Paged<MasterListItem>>('/api/master-data/privilege-types', { params, signal }),
        get: (id, signal?: AbortSignal) => tenantApi<MasterDetail>(`/api/master-data/privilege-types/${id}`, { signal }),
        create: (input) => tenantApi<MasterDetail>('/api/master-data/privilege-types', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => tenantApi<MasterDetail>(`/api/master-data/privilege-types/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => tenantApi<void>(`/api/master-data/privilege-types/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/privilege-types/${id}/history`, { signal }),
        exportXlsx: (params) => tenantApiBlob('/api/master-data/privilege-types/export', { params }),
    },
    path: '/master-data/general/privilege-type',
    titleTh: 'ข้อมูล Privilege Type',
    breadcrumb: BREADCRUMB,
    codeLabel: 'รหัส Privilege Type',
    nameLabel: 'Privilege Type',
    emptyHint: 'ประเภทสิทธิ์การทำหัตถการ เป็นหัวของ Privilege SubType',
});
