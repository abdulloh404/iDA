import { z } from 'zod';
import { tenantApi, tenantApiBlob } from '../../../api/client';
import type { CrudApi } from '../../../api/crud';
import type { AuditEntry, Paged, RecordStatus } from '../../../api/types';
import { ITEM_DIRECTION_OPTIONS, REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD, omitFields, } from '../descriptor';
import type { ScreenDescriptor } from '../descriptor';
export interface ExpenseTypeListItem {
    id: string;
    code: string;
    nameTh: string;
    nameEn: string | null;
    direction: string;
    accountNo: string | null;
    departmentCode: string | null;
    status: RecordStatus;
}
export interface ExpenseTypeDetail {
    id: string;
    code: string;
    nameTh: string;
    nameEn: string | null;
    direction: string;
    accountNo: string | null;
    departmentId: string | null;
    status: RecordStatus;
    remark: string | null;
    rowVersion: string;
}
export type ExpenseTypeInput = Omit<ExpenseTypeDetail, 'id' | 'rowVersion'>;
const expenseTypeSchema = z.object({
    code: z
        .string()
        .trim()
        .min(1, 'โปรดระบุรหัสประเภทรายการค่าใช้จ่าย')
        .max(20, 'รหัสประเภทรายการค่าใช้จ่ายต้องไม่เกิน 20 ตัวอักษร'),
    nameTh: z.string().trim().min(1, 'โปรดระบุประเภทรายการค่าใช้จ่าย'),
    nameEn: z.string().trim().nullable(),
    direction: z.string().min(1, 'โปรดเลือกประเภทรายการ'),
    accountNo: z.string().trim().nullable(),
    departmentId: z.string().nullable(),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().nullable(),
});
const DIRECTION_LABELS = new Map(ITEM_DIRECTION_OPTIONS.map((o) => [o.value, o.label]));
export const expenseTypeScreen: ScreenDescriptor<ExpenseTypeListItem, ExpenseTypeDetail, ExpenseTypeInput> = {
    id: 'expense-type',
    resource: 'expense-types',
    path: '/master-data/tax-402/expense-type',
    titleTh: 'ประเภทรายการค่าใช้จ่าย',
    breadcrumb: [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลัก 40(2)' }],
    api: {
        resource: 'expense-types',
        list: (params, signal?: AbortSignal) => tenantApi<Paged<ExpenseTypeListItem>>('/api/master-data/expense-types', { params, signal }),
        get: (id, signal?: AbortSignal) => tenantApi<ExpenseTypeDetail>(`/api/master-data/expense-types/${id}`, { signal }),
        create: (input) => tenantApi<ExpenseTypeDetail>('/api/master-data/expense-types', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => tenantApi<ExpenseTypeDetail>(`/api/master-data/expense-types/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => tenantApi<void>(`/api/master-data/expense-types/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/expense-types/${id}/history`, { signal }),
        exportXlsx: (params) => tenantApiBlob('/api/master-data/expense-types/export', { params }),
    } satisfies CrudApi<ExpenseTypeListItem, ExpenseTypeDetail, ExpenseTypeInput>,
    columns: [
        { key: 'code', header: 'รหัสประเภท', sortable: true, width: '160px' },
        { key: 'nameTh', header: 'ประเภทรายการค่าใช้จ่าย', sortable: true },
        {
            key: 'direction',
            header: 'ประเภทรายการ',
            sortable: true,
            width: '140px',
            value: (row) => DIRECTION_LABELS.get(row.direction) ?? row.direction,
        },
        { key: 'accountNo', header: 'รหัสบัญชี', sortable: true, width: '150px' },
        { key: 'departmentCode', header: 'รหัสแผนก', sortable: true, width: '150px' },
        STATUS_COLUMN,
    ],
    filters: [
        {
            kind: 'select',
            name: 'direction',
            label: 'ประเภทรายการ',
            options: ITEM_DIRECTION_OPTIONS,
        },
        { kind: 'lookup', name: 'departmentId', label: 'แผนก', resource: 'departments' },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหารหัส ชื่อประเภท หรือรหัสบัญชี',
    defaultSort: 'code',
    rowKey: (row) => row.id,
    emptyHint: 'ประเภทรายการค่าใช้จ่ายใช้ผูกกับรายการหักในข้อมูลรายได้และรายการหัก',
    sections: [
        {
            title: 'ข้อมูลทั่วไป',
            fields: [
                {
                    kind: 'text',
                    name: 'code',
                    label: 'รหัสประเภทรายการค่าใช้จ่าย',
                    required: true,
                    maxLength: 20,
                    immutableOnEdit: true,
                },
                { kind: 'text', name: 'nameTh', label: 'ประเภทรายการค่าใช้จ่าย', required: true },
                { kind: 'text', name: 'nameEn', label: 'ประเภทรายการค่าใช้จ่าย (ภาษาอังกฤษ)' },
                {
                    kind: 'select',
                    name: 'direction',
                    label: 'ประเภทรายการ',
                    required: true,
                    options: ITEM_DIRECTION_OPTIONS,
                },
                { kind: 'text', name: 'accountNo', label: 'รหัสบัญชี', width: 'sm' },
                {
                    kind: 'lookup',
                    name: 'departmentId',
                    label: 'รหัสแผนก',
                    resource: 'departments',
                    emptyLabel: 'ไม่ระบุ',
                },
                STATUS_FIELD,
                REMARK_FIELD,
            ],
        },
    ],
    schema: expenseTypeSchema,
    defaultValues: {
        code: '',
        nameTh: '',
        nameEn: null,
        direction: 'DEDUCT',
        accountNo: null,
        departmentId: null,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => omitFields(detail, 'id', 'rowVersion'),
};
