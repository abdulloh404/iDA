import { z } from 'zod';
import { createCrudApi } from '../../api/crud';
import type { RecordStatus } from '../../api/types';
import type { SelectOption } from '../../components/form/Select';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD, omitFields } from '../master-data/descriptor';
import type { ScreenDescriptor } from '../master-data/descriptor';
export interface BadDebtTierListItem {
    id: string;
    fromPercent: number;
    toPercent: number;
    payActual: boolean;
    payPercent: number | null;
    status: RecordStatus;
}
export interface BadDebtTierDetail extends BadDebtTierListItem {
    remark: string | null;
    rowVersion: string;
}
export type BadDebtTierInput = Omit<BadDebtTierDetail, 'id' | 'rowVersion'>;
const PAY_ACTUAL_OPTIONS: readonly SelectOption[] = [
    { value: 'true', label: 'ใช่' },
    { value: 'false', label: 'ไม่ใช่' },
];
const percent = (required: string) => z
    .number({ invalid_type_error: required, required_error: required })
    .min(0, 'ต้องอยู่ระหว่าง 0 ถึง 100')
    .max(100, 'ต้องอยู่ระหว่าง 0 ถึง 100')
    .refine((v) => Math.round(v * 100) === v * 100, 'มีทศนิยมได้ไม่เกิน 2 ตำแหน่ง');
const schema = z
    .object({
    fromPercent: percent('โปรดระบุเปอร์เซ็นต์ชำระเริ่มต้น (%)'),
    toPercent: percent('โปรดระบุถึง (%)'),
    payActual: z.boolean(),
    payPercent: z.number().nullable(),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().max(1000).nullable(),
})
    .superRefine((value, ctx) => {
    if (value.fromPercent > value.toPercent)
        ctx.addIssue({
            code: z.ZodIssueCode.custom,
            path: ['toPercent'],
            message: 'ถึง (%) ต้องมากกว่าหรือเท่ากับเปอร์เซ็นต์ชำระเริ่มต้น (%)',
        });
    if (!value.payActual) {
        const pay = value.payPercent;
        if (pay === null || Number.isNaN(pay))
            ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['payPercent'], message: 'โปรดระบุเปอร์เซ็นต์จ่ายแพทย์' });
        else if (pay < 0 || pay > 100)
            ctx.addIssue({
                code: z.ZodIssueCode.custom,
                path: ['payPercent'],
                message: 'เปอร์เซ็นต์จ่ายแพทย์ต้องอยู่ระหว่าง 0 ถึง 100',
            });
    }
});
const pct = (value: number | null) => (value === null ? '—' : `${value.toFixed(2)}%`);
export const badDebtTierScreen: ScreenDescriptor<BadDebtTierListItem, BadDebtTierDetail, BadDebtTierInput> = {
    id: 'bad-debt-tier',
    resource: 'bad-debt-tiers',
    path: '/doctor-fee-406/bad-debt-tier',
    titleTh: 'ตั้งค่าขั้นบันไดหนี้สูญ',
    breadcrumb: [{ label: 'จัดการค่าแพทย์ 40(6)' }],
    api: createCrudApi<BadDebtTierListItem, BadDebtTierDetail, BadDebtTierInput>('bad-debt-tiers'),
    columns: [
        {
            key: 'fromPercent',
            header: 'เปอร์เซ็นต์ชำระเริ่มต้น',
            sortable: true,
            align: 'right',
            width: '190px',
            value: (row) => pct(row.fromPercent),
        },
        {
            key: 'toPercent',
            header: 'ถึง',
            sortable: true,
            align: 'right',
            width: '120px',
            value: (row) => pct(row.toPercent),
        },
        {
            key: 'payActual',
            header: 'จ่ายตาม % ที่รับชำระจริง',
            sortable: true,
            width: '200px',
            value: (row) => (row.payActual ? 'ใช่' : 'ไม่ใช่'),
        },
        {
            key: 'payPercent',
            header: 'เปอร์เซ็นต์จ่ายแพทย์',
            sortable: true,
            align: 'right',
            width: '180px',
            value: (row) => (row.payActual ? 'ตามที่รับชำระจริง' : pct(row.payPercent)),
        },
        STATUS_COLUMN,
    ],
    filters: [
        { kind: 'select', name: 'payActual', label: 'จ่ายตามจริง', options: PAY_ACTUAL_OPTIONS },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหาด้วยเปอร์เซ็นต์รับชำระ เช่น 45',
    defaultSort: 'fromPercent',
    rowKey: (row) => row.id,
    emptyHint: 'กำหนดว่าเมื่อลูกหนี้ที่ตัดเป็นหนี้สูญรับชำระเข้ามากี่เปอร์เซ็นต์ จะจ่ายค่าแพทย์เท่าไร — ช่วงที่ใช้งานอยู่ต้องไม่ทับกัน',
    sections: [
        {
            title: 'ช่วงเปอร์เซ็นต์รับชำระ',
            description: 'รวมขอบทั้งสองข้าง — ขั้น 0–50 กับ 50.01–100 ไม่ทับกัน',
            fields: [
                { kind: 'number', name: 'fromPercent', label: 'เปอร์เซ็นต์รับชำระเริ่มต้น (%)', required: true, min: 0, max: 100 },
                { kind: 'number', name: 'toPercent', label: 'ถึง (%)', required: true, min: 0, max: 100 },
            ],
        },
        {
            title: 'การจ่ายแพทย์',
            fields: [
                {
                    kind: 'bool',
                    name: 'payActual',
                    label: 'จ่ายตามเปอร์เซ็นต์ที่รับชำระจริง',
                    trueLabel: 'ใช่',
                    falseLabel: 'ไม่ใช่',
                },
                {
                    kind: 'number',
                    name: 'payPercent',
                    label: 'เปอร์เซ็นต์จ่ายแพทย์ (%)',
                    min: 0,
                    max: 100,
                    hint: 'ใช้เมื่อเลือก "ไม่ใช่" เท่านั้น — ถ้าจ่ายตามจริงระบบไม่ใช้ค่านี้',
                },
            ],
        },
        { title: 'อื่น ๆ', fields: [STATUS_FIELD, REMARK_FIELD] },
    ],
    schema: schema as never,
    defaultValues: {
        fromPercent: 0,
        toPercent: 0,
        payActual: true,
        payPercent: null,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => omitFields(detail, 'id', 'rowVersion'),
};
