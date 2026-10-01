import { z } from 'zod';
import { tenantApi, tenantApiBlob } from '../../../api/client';
import type { CrudApi } from '../../../api/crud';
import type { AuditEntry, Paged, RecordStatus } from '../../../api/types';
import { INVOICE_CALC_MODE_OPTIONS, REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD, omitFields, } from '../descriptor';
import type { ScreenDescriptor } from '../descriptor';
export interface InvoicePrefixRuleListItem {
    id: string;
    invoicePrefix: string;
    paymentLocation: string | null;
    calcMode: string;
    status: RecordStatus;
}
export interface InvoicePrefixRuleDetail extends InvoicePrefixRuleListItem {
    remark: string | null;
    rowVersion: string;
}
export type InvoicePrefixRuleInput = Omit<InvoicePrefixRuleDetail, 'id' | 'rowVersion'>;
const invoicePrefixRuleSchema = z.object({
    invoicePrefix: z.string().trim().min(1, 'โปรดระบุรหัสขึ้นต้นของ Invoice'),
    paymentLocation: z.string().trim().nullable(),
    calcMode: z.string().min(1, 'โปรดเลือกรูปแบบการคำนวณ'),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().nullable(),
});
const CALC_MODE_LABELS = new Map(INVOICE_CALC_MODE_OPTIONS.map((o) => [o.value, o.label]));
export const invoicePrefixRuleScreen: ScreenDescriptor<InvoicePrefixRuleListItem, InvoicePrefixRuleDetail, InvoicePrefixRuleInput> = {
    id: 'invoice-prefix-rule',
    resource: 'invoice-prefix-rules',
    path: '/master-data/tax-406/invoice-prefix-rule',
    titleTh: 'ข้อมูล Import Invoice',
    breadcrumb: [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลัก 40(6)' }],
    api: {
        resource: 'invoice-prefix-rules',
        list: (params, signal?: AbortSignal) => tenantApi<Paged<InvoicePrefixRuleListItem>>('/api/master-data/invoice-prefix-rules', { params, signal }),
        get: (id, signal?: AbortSignal) => tenantApi<InvoicePrefixRuleDetail>(`/api/master-data/invoice-prefix-rules/${id}`, { signal }),
        create: (input) => tenantApi<InvoicePrefixRuleDetail>('/api/master-data/invoice-prefix-rules', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => tenantApi<InvoicePrefixRuleDetail>(`/api/master-data/invoice-prefix-rules/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => tenantApi<void>(`/api/master-data/invoice-prefix-rules/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/invoice-prefix-rules/${id}/history`, { signal }),
        exportXlsx: (params) => tenantApiBlob('/api/master-data/invoice-prefix-rules/export', { params }),
    } satisfies CrudApi<InvoicePrefixRuleListItem, InvoicePrefixRuleDetail, InvoicePrefixRuleInput>,
    columns: [
        { key: 'invoicePrefix', header: 'Invoice ขึ้นต้นด้วย', sortable: true, width: '200px' },
        {
            key: 'paymentLocation',
            header: 'Location จ่ายเงิน',
            sortable: true,
            width: '200px',
            value: (row) => row.paymentLocation ?? 'ทุก Location',
        },
        {
            key: 'calcMode',
            header: 'รูปแบบการคำนวณ',
            sortable: true,
            value: (row) => CALC_MODE_LABELS.get(row.calcMode) ?? row.calcMode,
        },
        STATUS_COLUMN,
    ],
    filters: [
        {
            kind: 'select',
            name: 'calcMode',
            label: 'รูปแบบการคำนวณ',
            options: INVOICE_CALC_MODE_OPTIONS,
        },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหารหัสขึ้นต้นหรือ Location',
    defaultSort: 'invoicePrefix',
    rowKey: (row) => row.id,
    emptyHint: 'ยังไม่มีกฎ — Invoice ทุกใบจะคำนวณส่งแบ่งให้แพทย์ตามปกติ',
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
                    kind: 'text',
                    name: 'paymentLocation',
                    label: 'Location จ่ายเงิน',
                    hint: 'เว้นว่างคือใช้กับทุก Location',
                },
                {
                    kind: 'radio',
                    name: 'calcMode',
                    label: 'รูปแบบการคำนวณ',
                    required: true,
                    options: INVOICE_CALC_MODE_OPTIONS,
                    width: 'lg',
                },
                STATUS_FIELD,
                REMARK_FIELD,
            ],
        },
    ],
    schema: invoicePrefixRuleSchema,
    defaultValues: {
        invoicePrefix: '',
        paymentLocation: null,
        calcMode: 'NORMAL_SHARE',
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => omitFields(detail, 'id', 'rowVersion'),
};
