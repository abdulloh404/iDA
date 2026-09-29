import { z } from 'zod';
import { createCrudApi } from '../../../api/crud';
import type { RecordStatus } from '../../../api/types';
import { ITEM_DIRECTION_OPTIONS, REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD, omitFields, } from '../descriptor';
import type { ScreenDescriptor } from '../descriptor';
export interface AdjustmentTypeListItem {
    id: string;
    code: string;
    nameTh: string;
    nameEn: string | null;
    direction: string;
    departmentCode: string | null;
    status: RecordStatus;
}
export interface AdjustmentTypeDetail {
    id: string;
    code: string;
    nameTh: string;
    nameEn: string | null;
    direction: string;
    departmentId: string | null;
    status: RecordStatus;
    remark: string | null;
    rowVersion: string;
}
export type AdjustmentTypeInput = Omit<AdjustmentTypeDetail, 'id' | 'rowVersion'>;
const adjustmentTypeSchema = z.object({
    code: z
        .string()
        .trim()
        .min(1, 'โปรดระบุรหัสประเภทรายการปรับปรุง')
        .max(20, 'รหัสประเภทรายการปรับปรุงต้องไม่เกิน 20 ตัวอักษร'),
    nameTh: z.string().trim().min(1, 'โปรดระบุประเภทรายการปรับปรุง'),
    nameEn: z.string().trim().nullable(),
    direction: z.string().min(1, 'โปรดเลือกประเภทรายการ'),
    departmentId: z.string().nullable(),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().nullable(),
});
const DIRECTION_LABELS = new Map(ITEM_DIRECTION_OPTIONS.map((o) => [o.value, o.label]));
export const adjustmentTypeScreen: ScreenDescriptor<AdjustmentTypeListItem, AdjustmentTypeDetail, AdjustmentTypeInput> = {
    id: 'adjustment-type',
    resource: 'adjustment-types',
    path: '/master-data/tax-402/adjustment-type',
    titleTh: 'ประเภทรายการปรับปรุง',
    breadcrumb: [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลัก 40(2)' }],
    api: createCrudApi<AdjustmentTypeListItem, AdjustmentTypeDetail, AdjustmentTypeInput>('adjustment-types'),
    columns: [
        { key: 'code', header: 'รหัสประเภท', sortable: true, width: '160px' },
        { key: 'nameTh', header: 'ประเภทรายการปรับปรุง', sortable: true },
        {
            key: 'direction',
            header: 'ประเภทรายการ',
            sortable: true,
            width: '140px',
            value: (row) => DIRECTION_LABELS.get(row.direction) ?? row.direction,
        },
        { key: 'departmentCode', header: 'รหัสแผนก', sortable: true, width: '150px' },
        STATUS_COLUMN,
    ],
    filters: [
        {
            kind: 'select',
            name: 'direction',
            label: 'ประเภทรายการ',
            options: ITEM_DIRECTION_OPTIONS,
        },
        { kind: 'lookup', name: 'departmentId', label: 'แผนก', resource: 'departments' },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหารหัสหรือชื่อประเภทรายการปรับปรุง',
    defaultSort: 'code',
    rowKey: (row) => row.id,
    emptyHint: 'ประเภทรายการปรับปรุงใช้บนหน้าจอรายการปรับปรุง ตอนแก้ยอดค่าแพทย์',
    sections: [
        {
            title: 'ข้อมูลทั่วไป',
            fields: [
                {
                    kind: 'text',
                    name: 'code',
                    label: 'รหัสประเภทรายการปรับปรุง',
                    required: true,
                    maxLength: 20,
                    immutableOnEdit: true,
                },
                { kind: 'text', name: 'nameTh', label: 'ประเภทรายการปรับปรุง', required: true },
                { kind: 'text', name: 'nameEn', label: 'ประเภทรายการปรับปรุง (ภาษาอังกฤษ)' },
                {
                    kind: 'select',
                    name: 'direction',
                    label: 'ประเภทรายการ',
                    required: true,
                    options: ITEM_DIRECTION_OPTIONS,
                },
                {
                    kind: 'lookup',
                    name: 'departmentId',
                    label: 'รหัสแผนก',
                    resource: 'departments',
                    emptyLabel: 'ไม่ระบุ',
                },
                STATUS_FIELD,
                REMARK_FIELD,
            ],
        },
    ],
    schema: adjustmentTypeSchema,
    defaultValues: {
        code: '',
        nameTh: '',
        nameEn: null,
        direction: 'ADD',
        departmentId: null,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => omitFields(detail, 'id', 'rowVersion'),
};
