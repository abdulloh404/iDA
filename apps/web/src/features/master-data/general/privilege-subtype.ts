import { z } from 'zod';
import { createCrudApi } from '../../../api/crud';
import type { RecordStatus } from '../../../api/types';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD } from '../descriptor';
import type { ScreenDescriptor } from '../descriptor';
export interface PrivilegeSubtypeListItem {
    id: string;
    code: string;
    nameTh: string;
    nameEn: string | null;
    privilegeTypeCode: string | null;
    privilegeTypeNameTh: string | null;
    status: RecordStatus;
}
export interface PrivilegeSubtypeDetail {
    id: string;
    code: string;
    privilegeTypeId: string;
    nameTh: string;
    nameEn: string | null;
    status: RecordStatus;
    remark: string | null;
    rowVersion: string;
}
export interface PrivilegeSubtypeInput {
    code: string;
    privilegeTypeId: string;
    nameTh: string;
    nameEn: string | null;
    status: RecordStatus;
    remark: string | null;
}
const privilegeSubtypeSchema = z.object({
    code: z
        .string()
        .trim()
        .min(1, 'โปรดระบุรหัส Privilege SubType')
        .max(20, 'รหัส Privilege SubType ต้องไม่เกิน 20 ตัวอักษร'),
    privilegeTypeId: z.string().min(1, 'โปรดเลือก Privilege Type'),
    nameTh: z.string().trim().min(1, 'โปรดระบุชื่อ Privilege SubType (ภาษาไทย)'),
    nameEn: z.string().trim().nullable(),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().nullable(),
});
export const privilegeSubtypeScreen: ScreenDescriptor<PrivilegeSubtypeListItem, PrivilegeSubtypeDetail, PrivilegeSubtypeInput> = {
    id: 'privilege-subtype',
    resource: 'privilege-subtypes',
    path: '/master-data/general/privilege-subtype',
    titleTh: 'ข้อมูล Privilege SubType',
    breadcrumb: [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลักทั่วไป' }],
    api: createCrudApi<PrivilegeSubtypeListItem, PrivilegeSubtypeDetail, PrivilegeSubtypeInput>('privilege-subtypes'),
    columns: [
        { key: 'privilegeTypeCode', header: 'รหัส Privilege Type', sortable: true, width: '180px' },
        { key: 'privilegeTypeNameTh', header: 'Privilege Type', width: '200px' },
        { key: 'code', header: 'รหัส SubType', sortable: true, width: '160px' },
        { key: 'nameTh', header: 'Privilege SubType', sortable: true },
        STATUS_COLUMN,
    ],
    filters: [
        {
            kind: 'lookup',
            name: 'privilegeTypeId',
            label: 'Privilege Type',
            resource: 'privilege-types',
        },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหารหัสหรือชื่อ Privilege SubType',
    defaultSort: 'code',
    rowKey: (row) => row.id,
    emptyHint: 'Privilege SubType อยู่ใต้ Privilege Type ใช้ระบุสิทธิ์การทำหัตถการย่อย',
    sections: [
        {
            title: 'ข้อมูลทั่วไป',
            fields: [
                {
                    kind: 'lookup',
                    name: 'privilegeTypeId',
                    label: 'Privilege Type',
                    resource: 'privilege-types',
                    required: true,
                    width: 'lg',
                },
                {
                    kind: 'text',
                    name: 'code',
                    label: 'รหัส Privilege SubType',
                    required: true,
                    maxLength: 20,
                    immutableOnEdit: true,
                },
                {
                    kind: 'text',
                    name: 'nameTh',
                    label: 'ชื่อ Privilege SubType (ภาษาไทย)',
                    required: true,
                },
                { kind: 'text', name: 'nameEn', label: 'ชื่อ Privilege SubType (ภาษาอังกฤษ)' },
                STATUS_FIELD,
                REMARK_FIELD,
            ],
        },
    ],
    schema: privilegeSubtypeSchema,
    defaultValues: {
        code: '',
        privilegeTypeId: '',
        nameTh: '',
        nameEn: null,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => ({
        code: detail.code,
        privilegeTypeId: detail.privilegeTypeId,
        nameTh: detail.nameTh,
        nameEn: detail.nameEn,
        status: detail.status,
        remark: detail.remark,
    }),
};
