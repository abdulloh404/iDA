import { z } from 'zod';
import { createCrudApi } from '../../../api/crud';
import type { RecordStatus } from '../../../api/types';
import { ITEM_DIRECTION_OPTIONS, REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD, omitFields, } from '../descriptor';
import type { ScreenDescriptor } from '../descriptor';
export interface IncomeDeductionItemListItem {
    id: string;
    code: string;
    nameTh: string;
    taxTypeNameTh: string | null;
    direction: string;
    departmentCode: string | null;
    accountNoOpd: string | null;
    accountNoIpd: string | null;
    status: RecordStatus;
}
export interface IncomeDeductionItemDetail {
    id: string;
    code: string;
    nameTh: string;
    nameEn: string | null;
    taxTypeId: string | null;
    direction: string;
    departmentId: string | null;
    accountNoOpd: string | null;
    accountNoIpd: string | null;
    expenseTypeId: string | null;
    jvType: string | null;
    status: RecordStatus;
    remark: string | null;
    rowVersion: string;
}
export type IncomeDeductionItemInput = Omit<IncomeDeductionItemDetail, 'id' | 'rowVersion'>;
const incomeDeductionItemSchema = z
    .object({
    code: z
        .string()
        .trim()
        .min(1, 'โปรดระบุรหัสรายการ')
        .max(20, 'รหัสรายการต้องไม่เกิน 20 ตัวอักษร'),
    nameTh: z.string().trim().min(1, 'โปรดระบุชื่อรายการ'),
    nameEn: z.string().trim().nullable(),
    taxTypeId: z.string().nullable(),
    direction: z.string().min(1, 'โปรดเลือกประเภทรายการ'),
    departmentId: z.string().nullable(),
    accountNoOpd: z.string().trim().nullable(),
    accountNoIpd: z.string().trim().nullable(),
    expenseTypeId: z.string().nullable(),
    jvType: z.string().trim().nullable(),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().nullable(),
})
    .refine((v) => !!(v.accountNoOpd || v.accountNoIpd), {
    path: ['accountNoOpd'],
    message: 'ต้องระบุรหัสบัญชีอย่างน้อยหนึ่งฝั่ง (ผู้ป่วยนอกหรือผู้ป่วยใน)',
});
const DIRECTION_LABELS = new Map(ITEM_DIRECTION_OPTIONS.map((o) => [o.value, o.label]));
export const incomeDeductionItemScreen: ScreenDescriptor<IncomeDeductionItemListItem, IncomeDeductionItemDetail, IncomeDeductionItemInput> = {
    id: 'income-deduction-item',
    resource: 'income-deduction-items',
    path: '/master-data/accounting/income-deduction-item',
    titleTh: 'ข้อมูลรายได้และรายการหัก',
    breadcrumb: [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลักทางบัญชี' }],
    api: createCrudApi<IncomeDeductionItemListItem, IncomeDeductionItemDetail, IncomeDeductionItemInput>('income-deduction-items'),
    columns: [
        { key: 'code', header: 'รหัสรายการ', sortable: true, width: '150px' },
        { key: 'nameTh', header: 'ชื่อรายการ', sortable: true },
        { key: 'taxTypeNameTh', header: 'ประเภทภาษี', width: '160px' },
        {
            key: 'direction',
            header: 'ประเภทรายการ',
            sortable: true,
            width: '140px',
            value: (row) => DIRECTION_LABELS.get(row.direction) ?? row.direction,
        },
        { key: 'departmentCode', header: 'Department', sortable: true, width: '140px' },
        { key: 'accountNoOpd', header: 'ACCOUNT_NO_OPD', width: '170px' },
        { key: 'accountNoIpd', header: 'ACCOUNT_NO_IPD', width: '170px' },
        STATUS_COLUMN,
    ],
    filters: [
        {
            kind: 'select',
            name: 'direction',
            label: 'ประเภทรายการ',
            options: ITEM_DIRECTION_OPTIONS,
        },
        { kind: 'lookup', name: 'departmentId', label: 'Department', resource: 'departments' },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหารหัส ชื่อรายการ หรือรหัสบัญชี',
    defaultSort: 'code',
    rowKey: (row) => row.id,
    emptyHint: 'รายการรายได้และรายการหักเป็นตัวตั้งของการปรับยอดค่าแพทย์และการ post GL',
    sections: [
        {
            title: 'ข้อมูลรายการ',
            fields: [
                {
                    kind: 'text',
                    name: 'code',
                    label: 'รหัสรายการ',
                    required: true,
                    maxLength: 20,
                    immutableOnEdit: true,
                },
                { kind: 'text', name: 'nameTh', label: 'ชื่อรายการปรับปรุง', required: true },
                { kind: 'text', name: 'nameEn', label: 'ชื่อรายการ (ภาษาอังกฤษ)' },
                {
                    kind: 'lookup',
                    name: 'taxTypeId',
                    label: 'ประเภทภาษี',
                    resource: 'tax-types',
                    emptyLabel: 'ไม่กระทบฐานภาษี',
                },
                {
                    kind: 'select',
                    name: 'direction',
                    label: 'ประเภทรายการ',
                    required: true,
                    options: ITEM_DIRECTION_OPTIONS,
                },
                STATUS_FIELD,
            ],
        },
        {
            title: 'การลงบัญชี',
            description: 'ค่าที่ใช้ตอนบันทึกบัญชีไป Oracle — ต้องมีรหัสบัญชีอย่างน้อยหนึ่งฝั่ง',
            fields: [
                {
                    kind: 'lookup',
                    name: 'departmentId',
                    label: 'ลงบัญชี Department',
                    resource: 'departments',
                    emptyLabel: 'ไม่ระบุ',
                },
                { kind: 'text', name: 'accountNoOpd', label: 'ACCOUNT_NO_OPD', width: 'sm' },
                { kind: 'text', name: 'accountNoIpd', label: 'ACCOUNT_NO_IPD', width: 'sm' },
                {
                    kind: 'lookup',
                    name: 'expenseTypeId',
                    label: 'รายการค่าใช้จ่ายที่ผูกกัน',
                    resource: 'expense-types',
                    emptyLabel: 'ไม่ระบุ',
                },
                { kind: 'text', name: 'jvType', label: 'ประเภท JV', width: 'sm' },
                REMARK_FIELD,
            ],
        },
    ],
    schema: incomeDeductionItemSchema,
    defaultValues: {
        code: '',
        nameTh: '',
        nameEn: null,
        taxTypeId: null,
        direction: 'ADD',
        departmentId: null,
        accountNoOpd: null,
        accountNoIpd: null,
        expenseTypeId: null,
        jvType: null,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => omitFields(detail, 'id', 'rowVersion'),
};
