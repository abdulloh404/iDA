import { z } from 'zod';
import { tenantApi, tenantApiBlob } from '../../../api/client';
import type { CrudApi } from '../../../api/crud';
import type { AuditEntry, Paged, RecordStatus } from '../../../api/types';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD, omitFields, } from '../descriptor';
import type { ScreenDescriptor } from '../descriptor';
export interface IncomeType402ListItem {
    id: string;
    code: string;
    nameTh: string;
    nameEn: string | null;
    departmentCode: string | null;
    accountNo: string | null;
    status: RecordStatus;
}
export interface IncomeType402Detail {
    id: string;
    code: string;
    nameTh: string;
    nameEn: string | null;
    departmentId: string | null;
    accountNo: string | null;
    status: RecordStatus;
    remark: string | null;
    rowVersion: string;
}
export type IncomeType402Input = Omit<IncomeType402Detail, 'id' | 'rowVersion'>;
const incomeType402Schema = z.object({
    code: z
        .string()
        .trim()
        .min(1, 'โปรดระบุรหัสประเภทเงินได้')
        .max(20, 'รหัสประเภทเงินได้ต้องไม่เกิน 20 ตัวอักษร'),
    nameTh: z.string().trim().min(1, 'โปรดระบุประเภทเงินได้'),
    nameEn: z.string().trim().nullable(),
    departmentId: z.string().nullable(),
    accountNo: z.string().trim().nullable(),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().nullable(),
});
export const incomeType402Screen: ScreenDescriptor<IncomeType402ListItem, IncomeType402Detail, IncomeType402Input> = {
    id: 'income-type-402',
    resource: 'income-types-402',
    path: '/master-data/tax-402/income-type',
    titleTh: 'ประเภทเงินได้',
    breadcrumb: [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลัก 40(2)' }],
    api: {
        resource: 'income-types-402',
        list: (params, signal?: AbortSignal) => tenantApi<Paged<IncomeType402ListItem>>('/api/master-data/income-types-402', { params, signal }),
        get: (id, signal?: AbortSignal) => tenantApi<IncomeType402Detail>(`/api/master-data/income-types-402/${id}`, { signal }),
        create: (input) => tenantApi<IncomeType402Detail>('/api/master-data/income-types-402', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => tenantApi<IncomeType402Detail>(`/api/master-data/income-types-402/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => tenantApi<void>(`/api/master-data/income-types-402/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/income-types-402/${id}/history`, { signal }),
        exportXlsx: (params) => tenantApiBlob('/api/master-data/income-types-402/export', { params }),
    } satisfies CrudApi<IncomeType402ListItem, IncomeType402Detail, IncomeType402Input>,
    columns: [
        { key: 'code', header: 'รหัสประเภทเงินได้', sortable: true, width: '180px' },
        { key: 'nameTh', header: 'ประเภทเงินได้', sortable: true },
        { key: 'departmentCode', header: 'รหัสแผนก', sortable: true, width: '150px' },
        { key: 'accountNo', header: 'รหัสบัญชี', sortable: true, width: '150px' },
        STATUS_COLUMN,
    ],
    filters: [
        { kind: 'lookup', name: 'departmentId', label: 'แผนก', resource: 'departments' },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหารหัส ประเภทเงินได้ หรือรหัสบัญชี',
    defaultSort: 'code',
    rowKey: (row) => row.id,
    emptyHint: 'ประเภทเงินได้ 40(2) ใช้แยกรายการค่าแพทย์ที่เป็นเงินเดือนหรือค่าตอบแทนประจำ',
    sections: [
        {
            title: 'ข้อมูลทั่วไป',
            fields: [
                {
                    kind: 'text',
                    name: 'code',
                    label: 'รหัสประเภทเงินได้',
                    required: true,
                    maxLength: 20,
                    immutableOnEdit: true,
                },
                { kind: 'text', name: 'nameTh', label: 'ประเภทเงินได้', required: true },
                { kind: 'text', name: 'nameEn', label: 'ประเภทเงินได้ (ภาษาอังกฤษ)' },
                {
                    kind: 'lookup',
                    name: 'departmentId',
                    label: 'รหัสแผนก',
                    resource: 'departments',
                    emptyLabel: 'ไม่ระบุ',
                },
                { kind: 'text', name: 'accountNo', label: 'รหัสบัญชี', width: 'sm' },
                STATUS_FIELD,
                REMARK_FIELD,
            ],
        },
    ],
    schema: incomeType402Schema,
    defaultValues: {
        code: '',
        nameTh: '',
        nameEn: null,
        departmentId: null,
        accountNo: null,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => omitFields(detail, 'id', 'rowVersion'),
};
