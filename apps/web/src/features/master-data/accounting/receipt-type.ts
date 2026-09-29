import { z } from 'zod';
import { createCrudApi } from '../../../api/crud';
import type { RecordStatus } from '../../../api/types';
import { RECEIPT_PAYMENT_FORM_OPTIONS, REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD, omitFields, } from '../descriptor';
import type { ScreenDescriptor } from '../descriptor';
export interface ReceiptTypeListItem {
    id: string;
    code: string;
    nameTh: string;
    nameEn: string | null;
    paymentForm: string;
    bankCode: string | null;
    isCharged: boolean;
    vatPercent: number | null;
    status: RecordStatus;
}
export interface ReceiptTypeDetail {
    id: string;
    code: string;
    nameTh: string;
    nameEn: string | null;
    bankId: string | null;
    paymentForm: string;
    isCharged: boolean;
    vatPercent: number | null;
    sourceSystem: string;
    status: RecordStatus;
    remark: string | null;
    rowVersion: string;
}
export type ReceiptTypeInput = Omit<ReceiptTypeDetail, 'id' | 'sourceSystem' | 'rowVersion'>;
const receiptTypeSchema = z.object({
    code: z
        .string()
        .trim()
        .min(1, 'โปรดระบุรหัสประเภทการรับเงิน')
        .max(20, 'รหัสประเภทการรับเงินต้องไม่เกิน 20 ตัวอักษร'),
    nameTh: z.string().trim().min(1, 'โปรดระบุรายละเอียด (ภาษาไทย)'),
    nameEn: z.string().trim().nullable(),
    bankId: z.string().nullable(),
    paymentForm: z.string().min(1, 'โปรดเลือกรูปแบบการชำระเงิน'),
    isCharged: z.boolean(),
    vatPercent: z
        .number()
        .min(0, 'ภาษีมูลค่าเพิ่มต้องอยู่ระหว่าง 0 ถึง 100')
        .max(100, 'ภาษีมูลค่าเพิ่มต้องอยู่ระหว่าง 0 ถึง 100')
        .nullable(),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().nullable(),
});
const PAYMENT_FORM_LABELS = new Map(RECEIPT_PAYMENT_FORM_OPTIONS.map((o) => [o.value, o.label]));
export const receiptTypeScreen: ScreenDescriptor<ReceiptTypeListItem, ReceiptTypeDetail, ReceiptTypeInput> = {
    id: 'receipt-type',
    resource: 'receipt-types',
    path: '/master-data/accounting/receipt-type',
    titleTh: 'ข้อมูลประเภทการรับเงิน',
    breadcrumb: [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลักทางบัญชี' }],
    api: createCrudApi<ReceiptTypeListItem, ReceiptTypeDetail, ReceiptTypeInput>('receipt-types'),
    columns: [
        { key: 'code', header: 'รหัสประเภทการรับเงิน', sortable: true, width: '190px' },
        { key: 'nameTh', header: 'รายละเอียด', sortable: true },
        {
            key: 'paymentForm',
            header: 'รูปแบบการชำระเงิน',
            sortable: true,
            width: '180px',
            value: (row) => PAYMENT_FORM_LABELS.get(row.paymentForm) ?? row.paymentForm,
        },
        {
            key: 'isCharged',
            header: 'สถานะการชาร์จ',
            sortable: true,
            width: '150px',
            value: (row) => (row.isCharged ? 'ชาร์จ' : 'ไม่ชาร์จ'),
        },
        {
            key: 'vatPercent',
            header: 'VAT (%)',
            sortable: true,
            align: 'right',
            format: 'amount',
            width: '120px',
        },
        STATUS_COLUMN,
    ],
    filters: [
        {
            kind: 'select',
            name: 'paymentForm',
            label: 'รูปแบบการชำระเงิน',
            options: RECEIPT_PAYMENT_FORM_OPTIONS,
        },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหารหัสหรือรายละเอียดประเภทการรับเงิน',
    defaultSort: 'code',
    rowKey: (row) => row.id,
    emptyHint: 'ประเภทการรับเงินคือวิธีที่คนไข้ชำระเงิน ปกติรับมาจาก HIS',
    sections: [
        {
            title: 'ข้อมูลทั่วไป',
            fields: [
                {
                    kind: 'text',
                    name: 'code',
                    label: 'รหัสประเภทการชำระเงิน',
                    required: true,
                    maxLength: 20,
                    immutableOnEdit: true,
                },
                { kind: 'text', name: 'nameTh', label: 'รายละเอียด (ภาษาไทย)', required: true },
                { kind: 'text', name: 'nameEn', label: 'รายละเอียด (ภาษาอังกฤษ)' },
                {
                    kind: 'select',
                    name: 'paymentForm',
                    label: 'รูปแบบการชำระเงิน',
                    required: true,
                    options: RECEIPT_PAYMENT_FORM_OPTIONS,
                },
                {
                    kind: 'lookup',
                    name: 'bankId',
                    label: 'ธนาคาร',
                    resource: 'banks',
                    emptyLabel: 'ไม่ระบุ',
                    hint: 'บังคับเมื่อรูปแบบการชำระเงินเป็นเช็คหรือบัตรเครดิต',
                },
                {
                    kind: 'bool',
                    name: 'isCharged',
                    label: 'สถานะประเภทการรับเงิน',
                    trueLabel: 'ชาร์จ',
                    falseLabel: 'ไม่ชาร์จ',
                },
                { kind: 'amount', name: 'vatPercent', label: 'ภาษีมูลค่าเพิ่ม (%)' },
                STATUS_FIELD,
                REMARK_FIELD,
            ],
        },
    ],
    schema: receiptTypeSchema,
    defaultValues: {
        code: '',
        nameTh: '',
        nameEn: null,
        bankId: null,
        paymentForm: 'CASH',
        isCharged: false,
        vatPercent: null,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => omitFields(detail, 'id', 'sourceSystem', 'rowVersion'),
};
