import { z } from 'zod';
import { createCrudApi } from '../../../api/crud';
import type { RecordStatus } from '../../../api/types';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD } from '../descriptor';
import type { ScreenDescriptor } from '../descriptor';
export interface ClinicListItem {
    id: string;
    code: string;
    nameTh: string;
    nameEn: string | null;
    location: string | null;
    phone: string | null;
    status: RecordStatus;
}
export interface ClinicDetail extends ClinicListItem {
    fax: string | null;
    remark: string | null;
    rowVersion: string;
}
export interface ClinicInput {
    code: string;
    nameTh: string;
    nameEn: string | null;
    location: string | null;
    phone: string | null;
    fax: string | null;
    status: RecordStatus;
    remark: string | null;
}
const clinicSchema = z.object({
    code: z
        .string()
        .trim()
        .min(1, 'โปรดระบุรหัสคลินิก')
        .max(20, 'รหัสคลินิกต้องไม่เกิน 20 ตัวอักษร'),
    nameTh: z.string().trim().min(1, 'โปรดระบุชื่อคลินิก (ภาษาไทย)'),
    nameEn: z.string().trim().nullable(),
    location: z.string().trim().nullable(),
    phone: z.string().trim().nullable(),
    fax: z.string().trim().nullable(),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().nullable(),
});
export const clinicScreen: ScreenDescriptor<ClinicListItem, ClinicDetail, ClinicInput> = {
    id: 'clinic',
    resource: 'clinics',
    path: '/master-data/general/clinic',
    titleTh: 'คลินิก',
    breadcrumb: [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลักทั่วไป' }],
    api: createCrudApi<ClinicListItem, ClinicDetail, ClinicInput>('clinics'),
    columns: [
        { key: 'code', header: 'รหัสคลินิก', sortable: true, width: '160px' },
        { key: 'nameTh', header: 'ชื่อคลินิก (ภาษาไทย)', sortable: true },
        { key: 'nameEn', header: 'ชื่อคลินิก (ภาษาอังกฤษ)', sortable: true },
        { key: 'location', header: 'Location', sortable: true, width: '180px' },
        { key: 'phone', header: 'เบอร์โทร', width: '140px' },
        STATUS_COLUMN,
    ],
    filters: [{ kind: 'status', name: 'status', label: 'สถานะ' }],
    searchHint: 'ค้นหารหัส ชื่อคลินิก หรือ Location',
    defaultSort: 'code',
    rowKey: (row) => row.id,
    emptyHint: 'คลินิกคือสถานที่ตามตารางออกตรวจของแพทย์ เป็นข้อมูลเฉพาะของแต่ละโรงพยาบาล',
    sections: [
        {
            title: 'ข้อมูลทั่วไป',
            fields: [
                {
                    kind: 'text',
                    name: 'code',
                    label: 'รหัสคลินิก',
                    required: true,
                    maxLength: 20,
                    immutableOnEdit: true,
                },
                { kind: 'text', name: 'nameTh', label: 'ชื่อคลินิก (ภาษาไทย)', required: true },
                { kind: 'text', name: 'nameEn', label: 'ชื่อคลินิก (ภาษาอังกฤษ)' },
                { kind: 'text', name: 'location', label: 'Location' },
                { kind: 'text', name: 'phone', label: 'เบอร์โทร', width: 'sm' },
                { kind: 'text', name: 'fax', label: 'โทรสาร', width: 'sm' },
                STATUS_FIELD,
                REMARK_FIELD,
            ],
        },
    ],
    schema: clinicSchema,
    defaultValues: {
        code: '',
        nameTh: '',
        nameEn: null,
        location: null,
        phone: null,
        fax: null,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => ({
        code: detail.code,
        nameTh: detail.nameTh,
        nameEn: detail.nameEn,
        location: detail.location,
        phone: detail.phone,
        fax: detail.fax,
        status: detail.status,
        remark: detail.remark,
    }),
};
