import { z } from 'zod';
import { createCrudApi } from '../../../api/crud';
import type { RecordStatus } from '../../../api/types';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD, omitFields, } from '../descriptor';
import type { ScreenDescriptor } from '../descriptor';
export interface ArCodeListItem {
    id: string;
    code: string;
    nameTh: string;
    nameEn: string | null;
    province: string | null;
    phone: string | null;
    status: RecordStatus;
}
export interface ArCodeDetail {
    id: string;
    code: string;
    nameTh: string;
    nameEn: string | null;
    address1: string | null;
    address2: string | null;
    address3: string | null;
    province: string | null;
    postcode: string | null;
    phone: string | null;
    fax: string | null;
    sourceSystem: string;
    status: RecordStatus;
    remark: string | null;
    rowVersion: string;
}
export type ArCodeInput = Omit<ArCodeDetail, 'id' | 'sourceSystem' | 'rowVersion'>;
const arCodeSchema = z.object({
    code: z
        .string()
        .trim()
        .min(1, 'โปรดระบุรหัส AR Code')
        .max(20, 'รหัส AR Code ต้องไม่เกิน 20 ตัวอักษร'),
    nameTh: z.string().trim().min(1, 'โปรดระบุชื่อ AR Code (ภาษาไทย)'),
    nameEn: z.string().trim().nullable(),
    address1: z.string().trim().nullable(),
    address2: z.string().trim().nullable(),
    address3: z.string().trim().nullable(),
    province: z.string().trim().nullable(),
    postcode: z
        .string()
        .trim()
        .nullable()
        .refine((v) => !v || /^\d{5}$/.test(v), 'รหัสไปรษณีย์ต้องเป็นตัวเลข 5 หลัก'),
    phone: z.string().trim().nullable(),
    fax: z.string().trim().nullable(),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().nullable(),
});
export const arCodeScreen: ScreenDescriptor<ArCodeListItem, ArCodeDetail, ArCodeInput> = {
    id: 'ar-code',
    resource: 'ar-codes',
    path: '/master-data/accounting/ar-code',
    titleTh: 'AR Code',
    breadcrumb: [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลักทางบัญชี' }],
    api: createCrudApi<ArCodeListItem, ArCodeDetail, ArCodeInput>('ar-codes'),
    columns: [
        { key: 'code', header: 'รหัส AR Code', sortable: true, width: '160px' },
        { key: 'nameTh', header: 'ชื่อ AR Code (ภาษาไทย)', sortable: true },
        { key: 'nameEn', header: 'ชื่อ AR Code (ภาษาอังกฤษ)', sortable: true },
        { key: 'province', header: 'จังหวัด', sortable: true, width: '150px' },
        { key: 'phone', header: 'เบอร์โทรติดต่อ', width: '150px' },
        STATUS_COLUMN,
    ],
    filters: [{ kind: 'status', name: 'status', label: 'สถานะ' }],
    searchHint: 'ค้นหารหัสหรือชื่อ AR Code',
    defaultSort: 'code',
    rowKey: (row) => row.id,
    emptyHint: 'AR Code คือคู่สัญญาที่โรงพยาบาลตั้งหนี้ถึง ปกติรับมาจาก HIS',
    sections: [
        {
            title: 'ข้อมูลทั่วไป',
            fields: [
                {
                    kind: 'text',
                    name: 'code',
                    label: 'รหัส AR Code',
                    required: true,
                    maxLength: 20,
                    immutableOnEdit: true,
                },
                { kind: 'text', name: 'nameTh', label: 'ชื่อ AR Code (ภาษาไทย)', required: true },
                { kind: 'text', name: 'nameEn', label: 'ชื่อ AR Code (ภาษาอังกฤษ)' },
                STATUS_FIELD,
            ],
        },
        {
            title: 'ที่อยู่และการติดต่อ',
            fields: [
                { kind: 'text', name: 'address1', label: 'Address 1', width: 'full' },
                { kind: 'text', name: 'address2', label: 'Address 2', width: 'full' },
                { kind: 'text', name: 'address3', label: 'Address 3', width: 'full' },
                { kind: 'text', name: 'province', label: 'จังหวัด', width: 'sm' },
                { kind: 'text', name: 'postcode', label: 'รหัสไปรษณีย์', width: 'sm' },
                { kind: 'text', name: 'phone', label: 'เบอร์โทรติดต่อ', width: 'sm' },
                { kind: 'text', name: 'fax', label: 'โทรสาร', width: 'sm' },
                REMARK_FIELD,
            ],
        },
    ],
    schema: arCodeSchema,
    defaultValues: {
        code: '',
        nameTh: '',
        nameEn: null,
        address1: null,
        address2: null,
        address3: null,
        province: null,
        postcode: null,
        phone: null,
        fax: null,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => omitFields(detail, 'id', 'sourceSystem', 'rowVersion'),
};
