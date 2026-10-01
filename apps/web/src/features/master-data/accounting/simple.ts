import { tenantApi, tenantApiBlob } from '../../../api/client';
import type { AuditEntry, Paged } from '../../../api/types';
import type { MasterDetail, MasterListItem } from '../simple-master';
import { simpleMasterScreen } from '../simple-master';
export const shareCategoryScreen = simpleMasterScreen({
    id: 'share-category',
    resource: 'share-categories',
    api: {
        resource: 'share-categories',
        list: (params, signal?: AbortSignal) => tenantApi<Paged<MasterListItem>>('/api/master-data/share-categories', { params, signal }),
        get: (id, signal?: AbortSignal) => tenantApi<MasterDetail>(`/api/master-data/share-categories/${id}`, { signal }),
        create: (input) => tenantApi<MasterDetail>('/api/master-data/share-categories', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => tenantApi<MasterDetail>(`/api/master-data/share-categories/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => tenantApi<void>(`/api/master-data/share-categories/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/share-categories/${id}/history`, { signal }),
        exportXlsx: (params) => tenantApiBlob('/api/master-data/share-categories/export', { params }),
    },
    path: '/master-data/accounting/share-category',
    titleTh: 'ข้อมูล Category',
    breadcrumb: [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลักทางบัญชี' }],
    codeLabel: 'รหัสประเภทส่วนแบ่ง',
    nameLabel: 'ประเภทส่วนแบ่ง',
    emptyHint: 'ประเภทส่วนแบ่งเป็นตัวเชื่อมระหว่างรายการค่าแพทย์กับผังบัญชีของ Oracle',
});
