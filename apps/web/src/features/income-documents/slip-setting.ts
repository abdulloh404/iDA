import { z } from 'zod';
import { tenantApi, tenantApiBlob } from '../../api/client';
import type { CrudApi } from '../../api/crud';
import type { AuditEntry, Paged, RecordStatus } from '../../api/types';
import type { SelectOption } from '../../components/form/Select';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD } from '../master-data/descriptor';
import type { ScreenDescriptor } from '../master-data/descriptor';
import { SyncFromDoctorsButton } from './SyncFromDoctorsButton';
export interface SlipSettingListItem {
    id: string;
    doctorCode: string | null;
    doctorName: string | null;
    email: string;
    backupEmail: string | null;
    sendPayslip: boolean;
    sendTaxCertificate406: boolean;
    sendTaxCertificate50Tawi: boolean;
    hasPdfPassword: boolean;
    status: RecordStatus;
}
export interface SlipSettingDetail {
    id: string;
    doctorCodeId: string;
    email: string;
    backupEmail: string | null;
    hasPdfPassword: boolean;
    sendPayslip: boolean;
    sendTaxCertificate406: boolean;
    sendTaxCertificate50Tawi: boolean;
    status: RecordStatus;
    remark: string | null;
    rowVersion: string;
}
export interface SlipSettingInput {
    doctorCodeId: string;
    email: string;
    backupEmail: string | null;
    pdfPassword: string | null;
    sendPayslip: boolean;
    sendTaxCertificate406: boolean;
    sendTaxCertificate50Tawi: boolean;
    status: RecordStatus;
    remark: string | null;
}
const REPORT_OPTIONS: readonly SelectOption[] = [
    { value: 'PAYSLIP', label: 'สลิปเงินเดือน' },
    { value: 'TAX_CERTIFICATE_406', label: 'หนังสือรับรอง 40(6)' },
    { value: 'TAX_CERTIFICATE_50_TAWI', label: 'หนังสือรับรอง 50 ทวิ' },
];
const reportsOf = (row: SlipSettingListItem) => [
    row.sendPayslip && 'สลิป',
    row.sendTaxCertificate406 && '40(6)',
    row.sendTaxCertificate50Tawi && '50 ทวิ',
]
    .filter(Boolean)
    .join(' · ');
const email = (required: boolean) => z
    .string()
    .trim()
    .max(100, 'อีเมลต้องไม่เกิน 100 ตัวอักษร')
    .refine((v) => !required || v.length > 0, 'โปรดระบุอีเมล')
    .refine((v) => v.length === 0 || /^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(v), 'รูปแบบอีเมลไม่ถูกต้อง');
const schema = z
    .object({
    doctorCodeId: z.string().min(1, 'โปรดระบุแพทย์'),
    email: email(true),
    backupEmail: email(false).nullable(),
    pdfPassword: z
        .string()
        .nullable()
        .refine((v) => !v || (v.trim().length >= 4 && v.trim().length <= 20), 'รหัสผ่านสำหรับเปิดไฟล์ PDF ต้องยาว 4–20 ตัวอักษร'),
    sendPayslip: z.boolean(),
    sendTaxCertificate406: z.boolean(),
    sendTaxCertificate50Tawi: z.boolean(),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().max(1000).nullable(),
})
    .superRefine((value, ctx) => {
    if (!value.sendPayslip && !value.sendTaxCertificate406 && !value.sendTaxCertificate50Tawi)
        ctx.addIssue({
            code: z.ZodIssueCode.custom,
            path: ['sendPayslip'],
            message: 'โปรดเลือกรายงานที่ส่งอย่างน้อย 1 รายการ',
        });
    if (value.backupEmail && value.backupEmail.trim().toLowerCase() === value.email.trim().toLowerCase())
        ctx.addIssue({
            code: z.ZodIssueCode.custom,
            path: ['backupEmail'],
            message: 'อีเมลสำรองต้องไม่ซ้ำกับอีเมลหลัก',
        });
});
export const slipSettingScreen: ScreenDescriptor<SlipSettingListItem, SlipSettingDetail, SlipSettingInput> = {
    id: 'slip-setting',
    resource: 'slip-settings',
    path: '/income-documents/slip-setting',
    titleTh: 'ตั้งค่าการออกสลิป',
    breadcrumb: [{ label: 'เอกสารรายได้' }],
    api: {
        resource: 'slip-settings',
        list: (params, signal?: AbortSignal) => tenantApi<Paged<SlipSettingListItem>>('/api/master-data/slip-settings', { params, signal }),
        get: (id, signal?: AbortSignal) => tenantApi<SlipSettingDetail>(`/api/master-data/slip-settings/${id}`, { signal }),
        create: (input) => tenantApi<SlipSettingDetail>('/api/master-data/slip-settings', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => tenantApi<SlipSettingDetail>(`/api/master-data/slip-settings/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => tenantApi<void>(`/api/master-data/slip-settings/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/slip-settings/${id}/history`, { signal }),
        exportXlsx: (params) => tenantApiBlob('/api/master-data/slip-settings/export', { params }),
    } satisfies CrudApi<SlipSettingListItem, SlipSettingDetail, SlipSettingInput>,
    columns: [
        { key: 'doctorCode', header: 'รหัสแพทย์', sortable: true, width: '130px' },
        { key: 'doctorName', header: 'แพทย์', sortable: true },
        { key: 'email', header: 'อีเมล', sortable: true },
        { key: 'reports', header: 'รายงานที่ส่ง', width: '190px', value: reportsOf },
        {
            key: 'hasPdfPassword',
            header: 'รหัสผ่าน PDF',
            width: '140px',
            value: (row) => (row.hasPdfPassword ? 'ตั้งแล้ว' : 'ยังไม่ตั้ง'),
        },
        STATUS_COLUMN,
    ],
    filters: [
        { kind: 'select', name: 'report', label: 'รายงานที่ส่ง', options: REPORT_OPTIONS },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหารหัสแพทย์ ชื่อแพทย์ หรืออีเมล',
    defaultSort: 'doctorCode',
    rowKey: (row) => row.id,
    emptyHint: 'อีเมลที่ใช้ส่งสลิปและหนังสือรับรองภาษีให้แพทย์แต่ละรหัส — กด "ดึงจากประวัติแพทย์" เพื่อสร้างจากอีเมลในประวัติแพทย์',
    sections: [
        {
            title: 'ผู้รับ',
            fields: [
                { kind: 'lookup', name: 'doctorCodeId', label: 'แพทย์', resource: 'doctor-codes', required: true, width: 'lg' },
                { kind: 'text', name: 'email', label: 'อีเมล', required: true, maxLength: 100 },
                { kind: 'text', name: 'backupEmail', label: 'อีเมลสำรอง', maxLength: 100 },
                {
                    kind: 'password',
                    name: 'pdfPassword',
                    label: 'รหัสผ่านสำหรับเปิดไฟล์ PDF',
                    maxLength: 20,
                    hint: 'เว้นว่างไว้ = ใช้รหัสเดิม · ระบบไม่แสดงรหัสที่ตั้งไว้แล้ว',
                },
            ],
        },
        {
            title: 'รายงานที่ส่ง',
            description: 'เลือกได้หลายรายการ ต้องเลือกอย่างน้อย 1 รายการ',
            fields: [
                { kind: 'checkbox', name: 'sendPayslip', label: 'สลิปเงินเดือน', width: 'md' },
                { kind: 'checkbox', name: 'sendTaxCertificate406', label: 'หนังสือรับรองภาษีเงินได้ 40(6)', width: 'md' },
                { kind: 'checkbox', name: 'sendTaxCertificate50Tawi', label: 'หนังสือรับรอง 50 ทวิ (ภ.ง.ด.1ก)', width: 'md' },
            ],
        },
        { title: 'อื่น ๆ', fields: [STATUS_FIELD, REMARK_FIELD] },
    ],
    schema: schema as never,
    defaultValues: {
        doctorCodeId: '',
        email: '',
        backupEmail: null,
        pdfPassword: null,
        sendPayslip: true,
        sendTaxCertificate406: true,
        sendTaxCertificate50Tawi: true,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => ({
        doctorCodeId: detail.doctorCodeId,
        email: detail.email,
        backupEmail: detail.backupEmail,
        pdfPassword: null,
        sendPayslip: detail.sendPayslip,
        sendTaxCertificate406: detail.sendTaxCertificate406,
        sendTaxCertificate50Tawi: detail.sendTaxCertificate50Tawi,
        status: detail.status,
        remark: detail.remark,
    }),
    ListActions: SyncFromDoctorsButton,
};
