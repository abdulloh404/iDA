import { z } from 'zod';
import { coreApi, coreApiBlob } from '../../../api/client';
import type { CrudApi } from '../../../api/crud';
import type { AuditEntry, Paged, RecordStatus } from '../../../api/types';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD } from '../descriptor';
import type { ScreenDescriptor } from '../descriptor';
export interface BankListItem {
    id: string;
    code: string;
    nameTh: string;
    nameEn: string | null;
    swiftCode: string | null;
    status: RecordStatus;
}
export interface BankDetail extends BankListItem {
    remark: string | null;
    rowVersion: string;
}
export interface BankInput {
    code: string;
    nameTh: string;
    nameEn: string | null;
    swiftCode: string | null;
    status: RecordStatus;
    remark: string | null;
}
const bankSchema = z.object({
    code: z
        .string()
        .trim()
        .min(1, 'โปรดระบุรหัสธนาคาร')
        .max(20, 'รหัสธนาคารต้องไม่เกิน 20 ตัวอักษร'),
    nameTh: z.string().trim().min(1, 'โปรดระบุชื่อธนาคาร (ภาษาไทย)'),
    nameEn: z.string().trim().nullable(),
    swiftCode: z
        .string()
        .trim()
        .nullable()
        .refine((v) => !v || v.length === 8 || v.length === 11, 'SWIFT Code ต้องมี 8 หรือ 11 ตัวอักษร'),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().nullable(),
});
export const bankScreen: ScreenDescriptor<BankListItem, BankDetail, BankInput> = {
    id: 'bank',
    resource: 'banks',
    path: '/master-data/accounting/bank',
    titleTh: 'ข้อมูลธนาคาร',
    breadcrumb: [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลักทางบัญชี' }],
    api: {
        resource: 'banks',
        list: (params, signal?: AbortSignal) => coreApi<Paged<BankListItem>>('/api/master-data/banks', { params, signal }),
        get: (id, signal?: AbortSignal) => coreApi<BankDetail>(`/api/master-data/banks/${id}`, { signal }),
        create: (input) => coreApi<BankDetail>('/api/master-data/banks', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => coreApi<BankDetail>(`/api/master-data/banks/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => coreApi<void>(`/api/master-data/banks/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => coreApi<AuditEntry[]>(`/api/master-data/banks/${id}/history`, { signal }),
        exportXlsx: (params) => coreApiBlob('/api/master-data/banks/export', { params }),
    } satisfies CrudApi<BankListItem, BankDetail, BankInput>,
    columns: [
        { key: 'code', header: 'รหัสธนาคาร', sortable: true, width: '160px' },
        { key: 'nameTh', header: 'ชื่อธนาคาร (ภาษาไทย)', sortable: true },
        { key: 'nameEn', header: 'ชื่อธนาคาร (ภาษาอังกฤษ)', sortable: true },
        { key: 'swiftCode', header: 'SWIFT Code', sortable: true, width: '160px' },
        STATUS_COLUMN,
    ],
    filters: [{ kind: 'status', name: 'status', label: 'สถานะ' }],
    searchHint: 'ค้นหารหัส ชื่อธนาคาร หรือ SWIFT Code',
    defaultSort: 'code',
    rowKey: (row) => row.id,
    emptyHint: 'ธนาคารเป็นรายการกลางของเครือ ใช้กับบัญชีรับเงินของแพทย์และไฟล์จ่ายเงิน PFEM',
    sections: [
        {
            title: 'ข้อมูลทั่วไป',
            fields: [
                {
                    kind: 'text',
                    name: 'code',
                    label: 'รหัสธนาคาร',
                    required: true,
                    maxLength: 20,
                    immutableOnEdit: true,
                },
                { kind: 'text', name: 'nameTh', label: 'ชื่อธนาคาร (ภาษาไทย)', required: true },
                { kind: 'text', name: 'nameEn', label: 'ชื่อธนาคาร (ภาษาอังกฤษ)' },
                {
                    kind: 'text',
                    name: 'swiftCode',
                    label: 'SWIFT Code',
                    width: 'sm',
                    hint: '8 หรือ 11 ตัวอักษร',
                },
                STATUS_FIELD,
                REMARK_FIELD,
            ],
        },
    ],
    schema: bankSchema,
    defaultValues: {
        code: '',
        nameTh: '',
        nameEn: null,
        swiftCode: null,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => ({
        code: detail.code,
        nameTh: detail.nameTh,
        nameEn: detail.nameEn,
        swiftCode: detail.swiftCode,
        status: detail.status,
        remark: detail.remark,
    }),
};
