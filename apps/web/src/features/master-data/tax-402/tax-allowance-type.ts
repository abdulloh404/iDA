import { z } from 'zod';
import { createCrudApi } from '../../../api/crud';
import type { RecordStatus } from '../../../api/types';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD, omitFields, } from '../descriptor';
import type { ScreenDescriptor } from '../descriptor';
import { allowanceItemsChild } from './tax-allowance-item';
export interface AllowanceTypeListItem {
    id: string;
    taxYear: number;
    itemCount: number;
    totalAmount: number;
    status: RecordStatus;
}
export interface AllowanceTypeDetail {
    id: string;
    taxYear: number;
    status: RecordStatus;
    remark: string | null;
    rowVersion: string;
}
export type AllowanceTypeInput = Omit<AllowanceTypeDetail, 'id' | 'rowVersion'>;
const allowanceTypeSchema = z.object({
    taxYear: z
        .number()
        .int()
        .min(2000, 'ปีภาษีต้องเป็น ค.ศ. ระหว่าง 2000 ถึง 2100')
        .max(2100, 'ปีภาษีต้องเป็น ค.ศ. ระหว่าง 2000 ถึง 2100'),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().nullable(),
});
export const taxAllowanceTypeScreen: ScreenDescriptor<AllowanceTypeListItem, AllowanceTypeDetail, AllowanceTypeInput> = {
    id: 'tax-allowance-type',
    resource: 'tax-allowance-types',
    path: '/master-data/tax-402/tax-allowance-type',
    titleTh: 'ตั้งค่าประเภทลดหย่อน',
    breadcrumb: [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลัก 40(2)' }],
    api: createCrudApi<AllowanceTypeListItem, AllowanceTypeDetail, AllowanceTypeInput>('tax-allowance-types'),
    columns: [
        { key: 'taxYear', header: 'ปีภาษี (ค.ศ.)', sortable: true, width: '150px' },
        {
            key: 'itemCount',
            header: 'จำนวนรายการลดหย่อน',
            sortable: true,
            align: 'right',
            width: '200px',
        },
        {
            key: 'totalAmount',
            header: 'รวมจำนวนเงิน (บาท)',
            align: 'right',
            format: 'amount',
            width: '200px',
        },
        STATUS_COLUMN,
    ],
    filters: [{ kind: 'status', name: 'status', label: 'สถานะ' }],
    searchHint: 'ค้นหาด้วยปีภาษี (ค.ศ.)',
    defaultSort: '-taxYear',
    rowKey: (row) => row.id,
    emptyHint: 'ชุดลดหย่อนกำหนดเป็นรายปีภาษี และใช้ตอนคำนวณภาษีเงินได้ของแพทย์สิ้นปี',
    sections: [
        {
            title: 'ปีภาษี',
            description: 'บันทึกปีภาษีก่อน แล้วจึงเพิ่มรายการลดหย่อนในตารางด้านล่าง',
            fields: [
                {
                    kind: 'number',
                    name: 'taxYear',
                    label: 'ปีภาษี (ค.ศ.)',
                    required: true,
                    min: 2000,
                    max: 2100,
                },
                STATUS_FIELD,
                REMARK_FIELD,
            ],
        },
    ],
    schema: allowanceTypeSchema,
    defaultValues: {
        taxYear: new Date().getFullYear(),
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => omitFields(detail, 'id', 'rowVersion'),
    childTables: [allowanceItemsChild],
};
