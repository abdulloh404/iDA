import { z } from 'zod';
import { createCrudApi } from '../../../api/crud';
import type { RecordStatus } from '../../../api/types';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD } from '../descriptor';
import type { ScreenDescriptor } from '../descriptor';
export interface PaymentTypeListItem {
    id: string;
    code: string;
    nameTh: string;
    nameEn: string | null;
    requireBankAccount: boolean;
    status: RecordStatus;
}
export interface PaymentTypeDetail extends PaymentTypeListItem {
    remark: string | null;
    rowVersion: string;
}
export interface PaymentTypeInput {
    code: string;
    nameTh: string;
    nameEn: string | null;
    requireBankAccount: boolean;
    status: RecordStatus;
    remark: string | null;
}
const paymentTypeSchema = z.object({
    code: z
        .string()
        .trim()
        .min(1, 'โปรดระบุรหัสประเภทการจ่ายเงิน')
        .max(20, 'รหัสประเภทการจ่ายเงินต้องไม่เกิน 20 ตัวอักษร'),
    nameTh: z.string().trim().min(1, 'โปรดระบุชื่อประเภทการจ่ายเงิน (ภาษาไทย)'),
    nameEn: z.string().trim().nullable(),
    requireBankAccount: z.boolean(),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().nullable(),
});
export const paymentTypeScreen: ScreenDescriptor<PaymentTypeListItem, PaymentTypeDetail, PaymentTypeInput> = {
    id: 'payment-type',
    resource: 'payment-types',
    path: '/master-data/accounting/payment-type',
    titleTh: 'ข้อมูลประเภทการจ่ายเงิน',
    breadcrumb: [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลักทางบัญชี' }],
    api: createCrudApi<PaymentTypeListItem, PaymentTypeDetail, PaymentTypeInput>('payment-types'),
    columns: [
        { key: 'code', header: 'รหัสประเภทการจ่ายเงิน', sortable: true, width: '200px' },
        { key: 'nameTh', header: 'ชื่อประเภทการจ่ายเงิน', sortable: true },
        {
            key: 'requireBankAccount',
            header: 'ต้องมีบัญชีธนาคาร',
            sortable: true,
            width: '180px',
            value: (row) => (row.requireBankAccount ? 'ต้องมี' : 'ไม่ต้องมี'),
        },
        STATUS_COLUMN,
    ],
    filters: [{ kind: 'status', name: 'status', label: 'สถานะ' }],
    searchHint: 'ค้นหารหัสหรือชื่อประเภทการจ่ายเงิน',
    defaultSort: 'code',
    rowKey: (row) => row.id,
    emptyHint: 'ประเภทการจ่ายเงินเป็นตัวกำหนดว่าค่าแพทย์ออกทางโอนธนาคาร เช็ค หรือ Payroll',
    sections: [
        {
            title: 'ข้อมูลทั่วไป',
            fields: [
                {
                    kind: 'text',
                    name: 'code',
                    label: 'รหัสประเภทการจ่ายเงิน',
                    required: true,
                    maxLength: 20,
                    immutableOnEdit: true,
                },
                {
                    kind: 'text',
                    name: 'nameTh',
                    label: 'ชื่อประเภทการจ่ายเงิน (ภาษาไทย)',
                    required: true,
                },
                { kind: 'text', name: 'nameEn', label: 'ชื่อประเภทการจ่ายเงิน (ภาษาอังกฤษ)' },
                {
                    kind: 'bool',
                    name: 'requireBankAccount',
                    label: 'ต้องมีบัญชีธนาคารก่อนอนุมัติ',
                    trueLabel: 'ต้องมี',
                    falseLabel: 'ไม่ต้องมี',
                },
                STATUS_FIELD,
                REMARK_FIELD,
            ],
        },
    ],
    schema: paymentTypeSchema,
    defaultValues: {
        code: '',
        nameTh: '',
        nameEn: null,
        requireBankAccount: false,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => ({
        code: detail.code,
        nameTh: detail.nameTh,
        nameEn: detail.nameEn,
        requireBankAccount: detail.requireBankAccount,
        status: detail.status,
        remark: detail.remark,
    }),
};
