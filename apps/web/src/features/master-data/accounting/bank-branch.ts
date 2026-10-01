import { z } from 'zod';
import { coreApi, coreApiBlob } from '../../../api/client';
import type { CrudApi } from '../../../api/crud';
import type { AuditEntry, Paged, RecordStatus } from '../../../api/types';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD, omitFields, } from '../descriptor';
import type { ScreenDescriptor } from '../descriptor';
export interface BankBranchListItem {
    id: string;
    code: string;
    nameTh: string;
    nameEn: string | null;
    bankCode: string | null;
    bankNameTh: string | null;
    isHeadOffice: boolean;
    status: RecordStatus;
}
export interface BankBranchDetail {
    id: string;
    code: string;
    bankId: string;
    nameTh: string;
    nameEn: string | null;
    address: string | null;
    contactName: string | null;
    contactPhone: string | null;
    contactFax: string | null;
    contactEmail: string | null;
    isHeadOffice: boolean;
    status: RecordStatus;
    remark: string | null;
    rowVersion: string;
}
export type BankBranchInput = Omit<BankBranchDetail, 'id' | 'rowVersion'>;
const bankBranchSchema = z.object({
    code: z
        .string()
        .trim()
        .min(1, 'โปรดระบุรหัสสาขาธนาคาร')
        .max(20, 'รหัสสาขาธนาคารต้องไม่เกิน 20 ตัวอักษร'),
    bankId: z.string().min(1, 'โปรดเลือกธนาคาร'),
    nameTh: z.string().trim().min(1, 'โปรดระบุชื่อสาขา (ภาษาไทย)'),
    nameEn: z.string().trim().nullable(),
    address: z.string().trim().nullable(),
    contactName: z.string().trim().nullable(),
    contactPhone: z.string().trim().nullable(),
    contactFax: z.string().trim().nullable(),
    contactEmail: z
        .string()
        .trim()
        .nullable()
        .refine((v) => !v || v.includes('@'), 'รูปแบบอีเมลไม่ถูกต้อง'),
    isHeadOffice: z.boolean(),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().nullable(),
});
export const bankBranchScreen: ScreenDescriptor<BankBranchListItem, BankBranchDetail, BankBranchInput> = {
    id: 'bank-branch',
    resource: 'bank-branches',
    path: '/master-data/accounting/bank-branch',
    titleTh: 'ข้อมูลสาขาธนาคาร',
    breadcrumb: [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลักทางบัญชี' }],
    api: {
        resource: 'bank-branches',
        list: (params, signal?: AbortSignal) => coreApi<Paged<BankBranchListItem>>('/api/master-data/bank-branches', { params, signal }),
        get: (id, signal?: AbortSignal) => coreApi<BankBranchDetail>(`/api/master-data/bank-branches/${id}`, { signal }),
        create: (input) => coreApi<BankBranchDetail>('/api/master-data/bank-branches', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => coreApi<BankBranchDetail>(`/api/master-data/bank-branches/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => coreApi<void>(`/api/master-data/bank-branches/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => coreApi<AuditEntry[]>(`/api/master-data/bank-branches/${id}/history`, { signal }),
        exportXlsx: (params) => coreApiBlob('/api/master-data/bank-branches/export', { params }),
    } satisfies CrudApi<BankBranchListItem, BankBranchDetail, BankBranchInput>,
    columns: [
        { key: 'bankCode', header: 'รหัสธนาคาร', sortable: true, width: '140px' },
        { key: 'bankNameTh', header: 'ธนาคาร', width: '200px' },
        { key: 'code', header: 'รหัสสาขา', sortable: true, width: '140px' },
        { key: 'nameTh', header: 'ชื่อสาขา', sortable: true },
        STATUS_COLUMN,
    ],
    filters: [
        { kind: 'lookup', name: 'bankId', label: 'ธนาคาร', resource: 'banks' },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหารหัสสาขา ชื่อสาขา หรือชื่อธนาคาร',
    defaultSort: 'code',
    rowKey: (row) => row.id,
    emptyHint: 'สาขาธนาคารเป็นปลายทางของบัญชีรับเงินแพทย์ ใช้ร่วมกันทั้งเครือ',
    sections: [
        {
            title: 'ข้อมูลสาขา',
            fields: [
                { kind: 'lookup', name: 'bankId', label: 'ธนาคาร', resource: 'banks', required: true },
                {
                    kind: 'text',
                    name: 'code',
                    label: 'รหัสสาขาธนาคาร',
                    required: true,
                    maxLength: 20,
                    immutableOnEdit: true,
                },
                { kind: 'text', name: 'nameTh', label: 'ชื่อสาขา (ภาษาไทย)', required: true },
                { kind: 'text', name: 'nameEn', label: 'ชื่อสาขา (ภาษาอังกฤษ)' },
                {
                    kind: 'bool',
                    name: 'isHeadOffice',
                    label: 'สำนักงานใหญ่',
                    trueLabel: 'ใช่',
                    falseLabel: 'ไม่ใช่',
                },
                { kind: 'textarea', name: 'address', label: 'ที่อยู่', rows: 2 },
                STATUS_FIELD,
            ],
        },
        {
            title: 'ผู้ติดต่อ',
            fields: [
                { kind: 'text', name: 'contactName', label: 'ชื่อ – นามสกุล ผู้ติดต่อ' },
                { kind: 'text', name: 'contactPhone', label: 'เบอร์ติดต่อ', width: 'sm' },
                { kind: 'text', name: 'contactFax', label: 'โทรสาร', width: 'sm' },
                { kind: 'text', name: 'contactEmail', label: 'อีเมล' },
                REMARK_FIELD,
            ],
        },
    ],
    schema: bankBranchSchema,
    defaultValues: {
        code: '',
        bankId: '',
        nameTh: '',
        nameEn: null,
        address: null,
        contactName: null,
        contactPhone: null,
        contactFax: null,
        contactEmail: null,
        isHeadOffice: false,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => omitFields(detail, 'id', 'rowVersion'),
};
