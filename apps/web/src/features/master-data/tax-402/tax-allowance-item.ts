import { z } from 'zod';
import { coreApi, coreApiBlob } from '../../../api/client';
import type { CrudApi } from '../../../api/crud';
import type { AuditEntry, Paged, RecordStatus } from '../../../api/types';
import { STATUS_COLUMN, STATUS_FIELD } from '../descriptor';
import type { ChildTableDef } from '../descriptor';
export interface AllowanceItem {
    id: string;
    taxAllowanceTypeId: string;
    allowanceName: string;
    detail: string | null;
    amount: number;
    displaySeq: number;
    status: RecordStatus;
}
export interface AllowanceItemDetail extends AllowanceItem {
    rowVersion: string;
}
export interface AllowanceItemInput {
    taxAllowanceTypeId: string;
    allowanceName: string;
    detail: string | null;
    amount: number;
    displaySeq: number;
    status: RecordStatus;
}
export const allowanceItemApi = {
    resource: 'tax-allowance-items',
    list: (params, signal?: AbortSignal) => coreApi<Paged<AllowanceItem>>('/api/master-data/tax-allowance-items', { params, signal }),
    get: (id, signal?: AbortSignal) => coreApi<AllowanceItemDetail>(`/api/master-data/tax-allowance-items/${id}`, { signal }),
    create: (input) => coreApi<AllowanceItemDetail>('/api/master-data/tax-allowance-items', { method: 'POST', body: input }),
    update: (id, input, rowVersion) => coreApi<AllowanceItemDetail>(`/api/master-data/tax-allowance-items/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
    remove: (id) => coreApi<void>(`/api/master-data/tax-allowance-items/${id}`, { method: 'DELETE' }),
    history: (id, signal?: AbortSignal) => coreApi<AuditEntry[]>(`/api/master-data/tax-allowance-items/${id}/history`, { signal }),
    exportXlsx: (params) => coreApiBlob('/api/master-data/tax-allowance-items/export', { params }),
} satisfies CrudApi<AllowanceItem, AllowanceItemDetail, AllowanceItemInput>;
export const allowanceItemsChild: ChildTableDef<AllowanceItem, AllowanceItemInput> = {
    title: 'รายการลดหย่อน',
    description: 'รายการลดหย่อนที่ใช้กับปีภาษีนี้ เช่น ค่าลดหย่อนส่วนตัว เบี้ยประกันชีวิต',
    api: allowanceItemApi,
    parentKey: 'taxAllowanceTypeId',
    addLabel: 'เพิ่มรายการลดหย่อน',
    columns: [
        { key: 'displaySeq', header: 'ลำดับ', width: '90px', align: 'right' },
        { key: 'allowanceName', header: 'รายการลดหย่อน' },
        { key: 'detail', header: 'รายละเอียด' },
        {
            key: 'amount',
            header: 'จำนวนเงิน (บาท)',
            align: 'right',
            format: 'amount',
            width: '170px',
        },
        STATUS_COLUMN,
    ],
    fields: [
        { kind: 'number', name: 'displaySeq', label: 'ลำดับ', required: true, min: 1 },
        { kind: 'text', name: 'allowanceName', label: 'รายการลดหย่อน', required: true, width: 'lg' },
        { kind: 'text', name: 'detail', label: 'รายละเอียด', width: 'lg' },
        { kind: 'amount', name: 'amount', label: 'จำนวนเงิน (บาท)', required: true },
        STATUS_FIELD,
    ],
    schema: z.object({
        taxAllowanceTypeId: z.string().min(1),
        allowanceName: z.string().trim().min(1, 'โปรดระบุรายการลดหย่อน'),
        detail: z.string().trim().nullable(),
        amount: z.number().min(0, 'จำนวนเงินต้องไม่ติดลบ'),
        displaySeq: z.number().int().min(1, 'ลำดับต้องเป็นจำนวนเต็มบวก'),
        status: z.enum(['ACTIVE', 'INACTIVE']),
    }),
    defaultValues: {
        taxAllowanceTypeId: '',
        allowanceName: '',
        detail: null,
        amount: 0,
        displaySeq: 1,
        status: 'ACTIVE',
    },
    toInput: (row) => ({
        taxAllowanceTypeId: row.taxAllowanceTypeId,
        allowanceName: row.allowanceName,
        detail: row.detail,
        amount: row.amount,
        displaySeq: row.displaySeq,
        status: row.status,
    }),
    rowKey: (row) => row.id,
    defaultSort: 'displaySeq',
    emptyHint: 'เพิ่มรายการลดหย่อนที่ใช้กับปีภาษีนี้',
};
