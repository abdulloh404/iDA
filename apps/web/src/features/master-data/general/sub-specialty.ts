import { z } from 'zod';
import { createCrudApi } from '../../../api/crud';
import type { RecordStatus } from '../../../api/types';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD } from '../descriptor';
import type { ScreenDescriptor } from '../descriptor';
export interface SubSpecialtyListItem {
    id: string;
    code: string;
    nameTh: string;
    nameEn: string | null;
    specialtyCode: string | null;
    specialtyNameTh: string | null;
    status: RecordStatus;
    createdAt: string;
    createdBy: string;
    updatedAt: string;
    updatedBy: string;
}
export interface SubSpecialtyDetail {
    id: string;
    code: string;
    specialtyId: string;
    nameTh: string;
    nameEn: string | null;
    status: RecordStatus;
    remark: string | null;
    createdAt: string;
    createdBy: string;
    updatedAt: string;
    updatedBy: string;
    rowVersion: string;
}
export interface SubSpecialtyInput {
    code: string;
    specialtyId: string;
    nameTh: string;
    nameEn: string | null;
    status: RecordStatus;
    remark: string | null;
}
const subSpecialtySchema = z.object({
    code: z
        .string()
        .trim()
        .min(1, 'โปรดระบุรหัสความเชี่ยวชาญเฉพาะทาง')
        .max(20, 'รหัสความเชี่ยวชาญเฉพาะทางต้องไม่เกิน 20 ตัวอักษร'),
    specialtyId: z.string().min(1, 'โปรดเลือกความเชี่ยวชาญ'),
    nameTh: z.string().trim().min(1, 'โปรดระบุความเชี่ยวชาญเฉพาะทาง (ภาษาไทย)'),
    nameEn: z.string().trim().nullable(),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().nullable(),
});
export const subSpecialtyScreen: ScreenDescriptor<SubSpecialtyListItem, SubSpecialtyDetail, SubSpecialtyInput> = {
    id: 'sub-specialty',
    resource: 'sub-specialties',
    path: '/master-data/general/sub-specialty',
    titleTh: 'ความเชี่ยวชาญเฉพาะทาง',
    breadcrumb: [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลักทั่วไป' }],
    api: createCrudApi<SubSpecialtyListItem, SubSpecialtyDetail, SubSpecialtyInput>('sub-specialties'),
    columns: [
        { key: 'specialtyCode', header: 'รหัสความเชี่ยวชาญ', sortable: true, width: '180px' },
        { key: 'specialtyNameTh', header: 'ความเชี่ยวชาญ', sortable: false },
        { key: 'code', header: 'รหัสเฉพาะทาง', sortable: true, width: '180px' },
        { key: 'nameTh', header: 'ความเชี่ยวชาญเฉพาะทาง', sortable: true },
        STATUS_COLUMN,
    ],
    filters: [
        { kind: 'lookup', name: 'specialtyId', label: 'ความเชี่ยวชาญ', resource: 'specialties' },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหารหัสหรือชื่อความเชี่ยวชาญเฉพาะทาง',
    defaultSort: '-updatedAt',
    rowKey: (row) => row.id,
    emptyHint: 'ความเชี่ยวชาญเฉพาะทางอยู่ใต้ความเชี่ยวชาญหลัก และใช้ร่วมกันทั้งเครือ',
    sections: [
        {
            title: 'ข้อมูลทั่วไป',
            fields: [
                {
                    kind: 'lookup',
                    name: 'specialtyId',
                    label: 'ความเชี่ยวชาญ',
                    resource: 'specialties',
                    required: true,
                    width: 'lg',
                },
                {
                    kind: 'text',
                    name: 'code',
                    label: 'รหัสความเชี่ยวชาญเฉพาะทาง',
                    required: true,
                    maxLength: 20,
                    immutableOnEdit: true,
                },
                {
                    kind: 'text',
                    name: 'nameTh',
                    label: 'ความเชี่ยวชาญเฉพาะทาง (ภาษาไทย)',
                    required: true,
                },
                { kind: 'text', name: 'nameEn', label: 'ความเชี่ยวชาญเฉพาะทาง (ภาษาอังกฤษ)' },
                STATUS_FIELD,
                REMARK_FIELD,
            ],
        },
    ],
    schema: subSpecialtySchema,
    defaultValues: {
        code: '',
        specialtyId: '',
        nameTh: '',
        nameEn: null,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => ({
        code: detail.code,
        specialtyId: detail.specialtyId,
        nameTh: detail.nameTh,
        nameEn: detail.nameEn,
        status: detail.status,
        remark: detail.remark,
    }),
};
