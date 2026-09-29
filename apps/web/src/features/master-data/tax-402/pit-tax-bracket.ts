import { z } from 'zod';
import { createCrudApi } from '../../../api/crud';
import type { RecordStatus } from '../../../api/types';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD, omitFields, } from '../descriptor';
import type { ScreenDescriptor } from '../descriptor';
export interface PitTaxBracketListItem {
    id: string;
    taxYear: number;
    incomeFrom: number;
    incomeTo: number | null;
    percent: number;
    baseAmount: number;
    status: RecordStatus;
}
export interface PitTaxBracketDetail extends PitTaxBracketListItem {
    remark: string | null;
    rowVersion: string;
}
export type PitTaxBracketInput = Omit<PitTaxBracketDetail, 'id' | 'rowVersion'>;
const pitTaxBracketSchema = z
    .object({
    taxYear: z
        .number()
        .int()
        .min(2000, 'ปีภาษีต้องเป็น ค.ศ. ระหว่าง 2000 ถึง 2100')
        .max(2100, 'ปีภาษีต้องเป็น ค.ศ. ระหว่าง 2000 ถึง 2100'),
    incomeFrom: z.number().min(0, 'เงินได้ตั้งแต่ต้องไม่ติดลบ'),
    incomeTo: z.number().nullable(),
    percent: z
        .number()
        .min(0, 'เปอร์เซ็นต์ที่ใช้คำนวณต้องอยู่ระหว่าง 0 ถึง 100')
        .max(100, 'เปอร์เซ็นต์ที่ใช้คำนวณต้องอยู่ระหว่าง 0 ถึง 100'),
    baseAmount: z.number().min(0, 'ฐานที่ใช้คำนวณต้องไม่ติดลบ'),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().nullable(),
})
    .refine((v) => v.incomeTo === null || v.incomeTo > v.incomeFrom, {
    path: ['incomeTo'],
    message: 'เงินได้ถึงต้องมากกว่าเงินได้ตั้งแต่',
});
export const pitTaxBracketScreen: ScreenDescriptor<PitTaxBracketListItem, PitTaxBracketDetail, PitTaxBracketInput> = {
    id: 'pit-tax-bracket',
    resource: 'pit-tax-brackets',
    path: '/master-data/tax-402/pit-tax-bracket',
    titleTh: 'เงื่อนไขภาษีเงินได้บุคคลธรรมดา',
    breadcrumb: [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลัก 40(2)' }],
    api: createCrudApi<PitTaxBracketListItem, PitTaxBracketDetail, PitTaxBracketInput>('pit-tax-brackets'),
    columns: [
        { key: 'taxYear', header: 'ปีภาษี (ค.ศ.)', sortable: true, width: '130px' },
        {
            key: 'incomeFrom',
            header: 'เงินได้ตั้งแต่',
            sortable: true,
            align: 'right',
            format: 'amount',
            width: '170px',
        },
        {
            key: 'incomeTo',
            header: 'ถึง',
            sortable: true,
            align: 'right',
            format: 'amount',
            width: '170px',
            value: (row) => row.incomeTo ?? 'ไม่มีเพดาน',
        },
        {
            key: 'percent',
            header: 'เปอร์เซ็นต์ที่ใช้คำนวณ',
            sortable: true,
            align: 'right',
            format: 'amount',
            width: '180px',
        },
        {
            key: 'baseAmount',
            header: 'ฐานที่ใช้คำนวณ',
            sortable: true,
            align: 'right',
            format: 'amount',
            width: '170px',
        },
        STATUS_COLUMN,
    ],
    filters: [{ kind: 'status', name: 'status', label: 'สถานะ' }],
    searchHint: 'ค้นหาด้วยปีภาษี (ค.ศ.)',
    defaultSort: 'incomeFrom',
    rowKey: (row) => row.id,
    emptyHint: 'ขั้นบันไดภาษีต้องเรียงต่อกันครบทั้งปีภาษี ไม่ขาดช่วงและไม่ทับกัน',
    sections: [
        {
            title: 'ขั้นภาษี',
            description: 'ช่วงเงินได้ของปีเดียวกันต้องไม่ทับกับขั้นอื่น',
            fields: [
                { kind: 'number', name: 'taxYear', label: 'ปีภาษี (ค.ศ.)', required: true, min: 2000 },
                { kind: 'amount', name: 'incomeFrom', label: 'เงินได้ตั้งแต่ (บาท)', required: true },
                {
                    kind: 'amount',
                    name: 'incomeTo',
                    label: 'ถึง (บาท)',
                    hint: 'เว้นว่างสำหรับขั้นสูงสุดที่ไม่มีเพดาน',
                },
                { kind: 'amount', name: 'percent', label: 'เปอร์เซ็นต์ที่ใช้คำนวณ', required: true },
                { kind: 'amount', name: 'baseAmount', label: 'ฐานที่ใช้คำนวณ (บาท)', required: true },
                STATUS_FIELD,
                REMARK_FIELD,
            ],
        },
    ],
    schema: pitTaxBracketSchema,
    defaultValues: {
        taxYear: new Date().getFullYear(),
        incomeFrom: 0,
        incomeTo: null,
        percent: 0,
        baseAmount: 0,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => omitFields(detail, 'id', 'rowVersion'),
};
