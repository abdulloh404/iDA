import { z } from 'zod';
import { tenantApi, tenantApiBlob } from '../../../api/client';
import type { CrudApi } from '../../../api/crud';
import type { AuditEntry, Paged, RecordStatus } from '../../../api/types';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD, omitFields, } from '../descriptor';
import type { ScreenDescriptor } from '../descriptor';
export interface InvoiceArCashRuleListItem {
    id: string;
    invoicePrefix: string;
    isAr: boolean;
    status: RecordStatus;
}
export interface InvoiceArCashRuleDetail extends InvoiceArCashRuleListItem {
    remark: string | null;
    rowVersion: string;
}
export type InvoiceArCashRuleInput = Omit<InvoiceArCashRuleDetail, 'id' | 'rowVersion'>;
const invoiceArCashRuleSchema = z.object({
    invoicePrefix: z.string().trim().min(1, 'โปรดระบุรหัสขึ้นต้นของ Invoice'),
    isAr: z.boolean(),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().nullable(),
});
export const invoiceArCashRuleScreen: ScreenDescriptor<InvoiceArCashRuleListItem, InvoiceArCashRuleDetail, InvoiceArCashRuleInput> = {
    id: 'invoice-ar-cash-rule',
    resource: 'invoice-ar-cash-rules',
    path: '/master-data/tax-406/invoice-ar-cash-rule',
    titleTh: 'ข้อมูล Invoice AR/Cash',
    breadcrumb: [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลัก 40(6)' }],
    api: {
        resource: 'invoice-ar-cash-rules',
        list: (params, signal?: AbortSignal) => tenantApi<Paged<InvoiceArCashRuleListItem>>('/api/master-data/invoice-ar-cash-rules', { params, signal }),
        get: (id, signal?: AbortSignal) => tenantApi<InvoiceArCashRuleDetail>(`/api/master-data/invoice-ar-cash-rules/${id}`, { signal }),
        create: (input) => tenantApi<InvoiceArCashRuleDetail>('/api/master-data/invoice-ar-cash-rules', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => tenantApi<InvoiceArCashRuleDetail>(`/api/master-data/invoice-ar-cash-rules/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => tenantApi<void>(`/api/master-data/invoice-ar-cash-rules/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/invoice-ar-cash-rules/${id}/history`, { signal }),
        exportXlsx: (params) => tenantApiBlob('/api/master-data/invoice-ar-cash-rules/export', { params }),
    } satisfies CrudApi<InvoiceArCashRuleListItem, InvoiceArCashRuleDetail, InvoiceArCashRuleInput>,
    columns: [
        { key: 'invoicePrefix', header: 'Invoice ขึ้นต้นด้วย', sortable: true, width: '220px' },
        {
            key: 'isAr',
            header: 'AR/Cash',
            sortable: true,
            value: (row) => (row.isAr ? 'AR (ตั้งหนี้)' : 'Cash (เงินสด)'),
        },
        STATUS_COLUMN,
    ],
    filters: [
        {
            kind: 'select',
            name: 'isAr',
            label: 'AR/Cash',
            options: [
                { value: 'true', label: 'AR (ตั้งหนี้)' },
                { value: 'false', label: 'Cash (เงินสด)' },
            ],
        },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหารหัสขึ้นต้นของ Invoice',
    defaultSort: 'invoicePrefix',
    rowKey: (row) => row.id,
    emptyHint: 'ยังไม่มีกฎ — ระบบจะอ่านสถานะ AR/Cash จากข้อมูลที่ HIS ส่งมาตามเดิม',
    sections: [
        {
            title: 'เงื่อนไข',
            fields: [
                {
                    kind: 'text',
                    name: 'invoicePrefix',
                    label: 'Invoice ขึ้นต้นด้วย',
                    required: true,
                    width: 'sm',
                    hint: 'บันทึกเป็นตัวพิมพ์ใหญ่เสมอ',
                },
                {
                    kind: 'bool',
                    name: 'isAr',
                    label: 'AR/Cash',
                    trueLabel: 'AR (ตั้งหนี้)',
                    falseLabel: 'Cash (เงินสด)',
                    width: 'lg',
                },
                STATUS_FIELD,
                REMARK_FIELD,
            ],
        },
    ],
    schema: invoiceArCashRuleSchema,
    defaultValues: {
        invoicePrefix: '',
        isAr: true,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => omitFields(detail, 'id', 'rowVersion'),
};
