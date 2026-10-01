import { z } from 'zod';
import { tenantApi, tenantApiBlob } from '../../../api/client';
import type { CrudApi } from '../../../api/crud';
import type { AuditEntry, Paged, RecordStatus } from '../../../api/types';
import { GL_POSTING_DATE_RULE_OPTIONS, REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD, omitFields, } from '../descriptor';
import type { ScreenDescriptor } from '../descriptor';
export interface GlPostingSetupListItem {
    id: string;
    shareCategoryCode: string | null;
    shareCategoryNameTh: string | null;
    debitAccountNo: string;
    debitDepartment: string | null;
    creditAccountNo: string;
    creditDepartment: string | null;
    doctorCode: string | null;
    postingDateRule: string;
    status: RecordStatus;
}
export interface GlPostingSetupDetail {
    id: string;
    shareCategoryId: string;
    debitAccountNo: string;
    debitDepartment: string | null;
    creditAccountNo: string;
    creditDepartment: string | null;
    doctorCodeId: string | null;
    postingDateRule: string;
    status: RecordStatus;
    remark: string | null;
    rowVersion: string;
}
export type GlPostingSetupInput = Omit<GlPostingSetupDetail, 'id' | 'rowVersion'>;
const glPostingSetupSchema = z.object({
    shareCategoryId: z.string().min(1, 'โปรดเลือกประเภทส่วนแบ่ง'),
    debitAccountNo: z.string().trim().min(1, 'โปรดระบุรหัสบัญชี (Debit)'),
    debitDepartment: z.string().trim().nullable(),
    creditAccountNo: z.string().trim().min(1, 'โปรดระบุรหัสบัญชี (Credit)'),
    creditDepartment: z.string().trim().nullable(),
    doctorCodeId: z.string().nullable(),
    postingDateRule: z.string().min(1, 'โปรดเลือกวันที่บันทึกบัญชี'),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().nullable(),
});
const DATE_RULE_LABELS = new Map(GL_POSTING_DATE_RULE_OPTIONS.map((o) => [o.value, o.label]));
export const glPostingSetupScreen: ScreenDescriptor<GlPostingSetupListItem, GlPostingSetupDetail, GlPostingSetupInput> = {
    id: 'gl-posting-setup',
    resource: 'gl-posting-setups',
    path: '/master-data/accounting/gl-posting-setup',
    titleTh: 'ข้อมูลตั้งค่าบันทึกบัญชี',
    breadcrumb: [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลักทางบัญชี' }],
    api: {
        resource: 'gl-posting-setups',
        list: (params, signal?: AbortSignal) => tenantApi<Paged<GlPostingSetupListItem>>('/api/master-data/gl-posting-setups', { params, signal }),
        get: (id, signal?: AbortSignal) => tenantApi<GlPostingSetupDetail>(`/api/master-data/gl-posting-setups/${id}`, { signal }),
        create: (input) => tenantApi<GlPostingSetupDetail>('/api/master-data/gl-posting-setups', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => tenantApi<GlPostingSetupDetail>(`/api/master-data/gl-posting-setups/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => tenantApi<void>(`/api/master-data/gl-posting-setups/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/gl-posting-setups/${id}/history`, { signal }),
        exportXlsx: (params) => tenantApiBlob('/api/master-data/gl-posting-setups/export', { params }),
    } satisfies CrudApi<GlPostingSetupListItem, GlPostingSetupDetail, GlPostingSetupInput>,
    columns: [
        { key: 'shareCategoryCode', header: 'รหัสประเภทส่วนแบ่ง', sortable: true, width: '180px' },
        { key: 'shareCategoryNameTh', header: 'ประเภทส่วนแบ่ง' },
        { key: 'debitAccountNo', header: 'รหัสบัญชี (Debit)', sortable: true, width: '160px' },
        { key: 'creditAccountNo', header: 'รหัสบัญชี (Credit)', sortable: true, width: '160px' },
        {
            key: 'doctorCode',
            header: 'ใช้เฉพาะแพทย์',
            width: '150px',
            value: (row) => row.doctorCode ?? 'ทุกแพทย์',
        },
        {
            key: 'postingDateRule',
            header: 'วันที่บันทึกบัญชี',
            sortable: true,
            width: '170px',
            value: (row) => DATE_RULE_LABELS.get(row.postingDateRule) ?? row.postingDateRule,
        },
        STATUS_COLUMN,
    ],
    filters: [
        {
            kind: 'lookup',
            name: 'shareCategoryId',
            label: 'ประเภทส่วนแบ่ง',
            resource: 'share-categories',
        },
        {
            kind: 'select',
            name: 'postingDateRule',
            label: 'วันที่บันทึกบัญชี',
            options: GL_POSTING_DATE_RULE_OPTIONS,
        },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหารหัสบัญชี ประเภทส่วนแบ่ง หรือรหัสแพทย์',
    defaultSort: 'shareCategoryCode',
    rowKey: (row) => row.id,
    emptyHint: 'ตั้งค่านี้เป็นตัวบอก Oracle ว่าค่าแพทย์แต่ละประเภทส่วนแบ่งลงบัญชีคู่ไหน',
    sections: [
        {
            title: 'ประเภทส่วนแบ่ง',
            fields: [
                {
                    kind: 'lookup',
                    name: 'shareCategoryId',
                    label: 'ประเภทส่วนแบ่ง',
                    resource: 'share-categories',
                    required: true,
                    width: 'lg',
                },
                {
                    kind: 'lookup',
                    name: 'doctorCodeId',
                    label: 'ใช้เฉพาะแพทย์',
                    resource: 'doctor-codes',
                    emptyLabel: 'ใช้กับทุกแพทย์',
                    width: 'lg',
                    hint: 'ตั้งไว้เมื่อแพทย์รายนี้ลงบัญชีคู่ที่ต่างจากคนอื่น',
                },
                {
                    kind: 'select',
                    name: 'postingDateRule',
                    label: 'วันที่บันทึกบัญชี',
                    required: true,
                    options: GL_POSTING_DATE_RULE_OPTIONS,
                },
                STATUS_FIELD,
            ],
        },
        {
            title: 'คู่บัญชี',
            description: 'ลง Debit และ Credit ที่บัญชีและแผนกเดียวกันไม่ได้ — รายการจะหักล้างตัวเอง',
            fields: [
                {
                    kind: 'text',
                    name: 'debitAccountNo',
                    label: 'รหัสบัญชี (Debit)',
                    required: true,
                    width: 'sm',
                },
                { kind: 'text', name: 'debitDepartment', label: 'แผนก (Debit)', width: 'sm' },
                {
                    kind: 'text',
                    name: 'creditAccountNo',
                    label: 'รหัสบัญชี (Credit)',
                    required: true,
                    width: 'sm',
                },
                { kind: 'text', name: 'creditDepartment', label: 'แผนก (Credit)', width: 'sm' },
                REMARK_FIELD,
            ],
        },
    ],
    schema: glPostingSetupSchema,
    defaultValues: {
        shareCategoryId: '',
        debitAccountNo: '',
        debitDepartment: null,
        creditAccountNo: '',
        creditDepartment: null,
        doctorCodeId: null,
        postingDateRule: 'BATCH_DATE',
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => omitFields(detail, 'id', 'rowVersion'),
};
