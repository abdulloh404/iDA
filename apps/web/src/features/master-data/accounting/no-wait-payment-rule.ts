import { z } from 'zod';
import { createCrudApi } from '../../../api/crud';
import type { RecordStatus } from '../../../api/types';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD, omitFields, } from '../descriptor';
import type { ScreenDescriptor } from '../descriptor';
export interface NoWaitPaymentRuleListItem {
    id: string;
    effectiveFrom: string;
    effectiveTo: string | null;
    activityCode: string | null;
    doctorCode: string | null;
    treatmentCode: string | null;
    treatmentCategoryCode: string | null;
    arCode: string | null;
    receiptTypeCode: string | null;
    subInvoice: string | null;
    status: RecordStatus;
}
export interface NoWaitPaymentRuleDetail {
    id: string;
    effectiveFrom: string;
    effectiveTo: string | null;
    activityCode: string | null;
    doctorCodeId: string | null;
    treatmentId: string | null;
    treatmentCategoryId: string | null;
    arCodeId: string | null;
    receiptTypeId: string | null;
    subInvoice: string | null;
    status: RecordStatus;
    remark: string | null;
    rowVersion: string;
}
export type NoWaitPaymentRuleInput = Omit<NoWaitPaymentRuleDetail, 'id' | 'rowVersion'>;
const noWaitPaymentRuleSchema = z
    .object({
    effectiveFrom: z.string().min(1, 'โปรดระบุวันที่เริ่มใช้'),
    effectiveTo: z.string().nullable(),
    activityCode: z.string().trim().nullable(),
    doctorCodeId: z.string().nullable(),
    treatmentId: z.string().nullable(),
    treatmentCategoryId: z.string().nullable(),
    arCodeId: z.string().nullable(),
    receiptTypeId: z.string().nullable(),
    subInvoice: z.string().trim().nullable(),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().nullable(),
})
    .refine((v) => !v.effectiveTo || v.effectiveTo >= v.effectiveFrom, {
    path: ['effectiveTo'],
    message: 'วันที่สิ้นสุดต้องไม่ก่อนวันที่เริ่มใช้',
});
const ANY = 'ทุกรายการ';
export const noWaitPaymentRuleScreen: ScreenDescriptor<NoWaitPaymentRuleListItem, NoWaitPaymentRuleDetail, NoWaitPaymentRuleInput> = {
    id: 'no-wait-payment-rule',
    resource: 'no-wait-payment-rules',
    path: '/master-data/accounting/no-wait-payment-rule',
    titleTh: 'ตั้งค่ารายการไม่รอรับชำระ',
    breadcrumb: [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลักทางบัญชี' }],
    api: createCrudApi<NoWaitPaymentRuleListItem, NoWaitPaymentRuleDetail, NoWaitPaymentRuleInput>('no-wait-payment-rules'),
    columns: [
        {
            key: 'effectiveFrom',
            header: 'วันที่เริ่มใช้',
            sortable: true,
            format: 'date',
            width: '140px',
        },
        {
            key: 'effectiveTo',
            header: 'วันที่สิ้นสุด',
            sortable: true,
            format: 'date',
            width: '140px',
            value: (row) => row.effectiveTo ?? 'ไม่กำหนด',
        },
        {
            key: 'activityCode',
            header: 'Activity Code',
            sortable: true,
            width: '150px',
            value: (row) => row.activityCode ?? ANY,
        },
        { key: 'doctorCode', header: 'แพทย์', width: '140px', value: (row) => row.doctorCode ?? ANY },
        {
            key: 'treatmentCode',
            header: 'Treatment',
            width: '150px',
            value: (row) => row.treatmentCode ?? ANY,
        },
        { key: 'arCode', header: 'AR Code', width: '140px', value: (row) => row.arCode ?? ANY },
        {
            key: 'subInvoice',
            header: 'Sub Invoice',
            width: '140px',
            value: (row) => row.subInvoice ?? ANY,
        },
        STATUS_COLUMN,
    ],
    filters: [
        { kind: 'lookup', name: 'doctorCodeId', label: 'แพทย์', resource: 'doctor-codes' },
        { kind: 'lookup', name: 'arCodeId', label: 'AR Code', resource: 'ar-codes' },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหา Activity Code, Sub Invoice, รหัสแพทย์ หรือ Treatment',
    defaultSort: '-effectiveFrom',
    rowKey: (row) => row.id,
    emptyHint: 'ยังไม่มีข้อยกเว้น — ค่าแพทย์ทุกรายการจะจ่ายหลังโรงพยาบาลรับชำระแล้วเท่านั้น',
    sections: [
        {
            title: 'ช่วงเวลาที่ใช้',
            fields: [
                { kind: 'date', name: 'effectiveFrom', label: 'วันที่เริ่มใช้', required: true },
                {
                    kind: 'date',
                    name: 'effectiveTo',
                    label: 'วันที่สิ้นสุดการใช้',
                    hint: 'ว่างไว้คือใช้ต่อเนื่องไม่มีกำหนดสิ้นสุด',
                },
                STATUS_FIELD,
            ],
        },
        {
            title: 'เงื่อนไข',
            description: 'ช่องที่เว้นว่างหมายถึงกฎนี้ใช้กับทุกค่าของช่องนั้น',
            fields: [
                { kind: 'text', name: 'activityCode', label: 'Activity Code', width: 'sm' },
                {
                    kind: 'lookup',
                    name: 'doctorCodeId',
                    label: 'แพทย์',
                    resource: 'doctor-codes',
                    emptyLabel: ANY,
                },
                {
                    kind: 'lookup',
                    name: 'treatmentId',
                    label: 'Treatment',
                    resource: 'treatments',
                    emptyLabel: ANY,
                },
                {
                    kind: 'lookup',
                    name: 'treatmentCategoryId',
                    label: 'Category',
                    resource: 'treatment-categories',
                    emptyLabel: ANY,
                },
                {
                    kind: 'lookup',
                    name: 'arCodeId',
                    label: 'AR Code',
                    resource: 'ar-codes',
                    emptyLabel: ANY,
                },
                {
                    kind: 'lookup',
                    name: 'receiptTypeId',
                    label: 'Receipt Type',
                    resource: 'receipt-types',
                    emptyLabel: ANY,
                },
                { kind: 'text', name: 'subInvoice', label: 'Sub Invoice', width: 'sm' },
                REMARK_FIELD,
            ],
        },
    ],
    schema: noWaitPaymentRuleSchema,
    defaultValues: {
        effectiveFrom: '',
        effectiveTo: null,
        activityCode: null,
        doctorCodeId: null,
        treatmentId: null,
        treatmentCategoryId: null,
        arCodeId: null,
        receiptTypeId: null,
        subInvoice: null,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => omitFields(detail, 'id', 'rowVersion'),
};
