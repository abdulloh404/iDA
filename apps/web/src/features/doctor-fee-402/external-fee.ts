import { z } from 'zod';
import { tenantApi, tenantApiBlob } from '../../api/client';
import type { CrudApi } from '../../api/crud';
import type { AuditEntry, Paged, RecordStatus } from '../../api/types';
import { REMARK_FIELD, STATUS_FIELD } from '../master-data/descriptor';
import type { ChildTableDef, ScreenDescriptor } from '../master-data/descriptor';
import { MONTH_OPTIONS, periodLabel, yearOptions } from '../shared/period';
import { APPROVAL_STATUS_OPTIONS, APPROVE_PERMISSION, BREADCRUMB, CYCLE_OPTIONS, cycleLabel, decideDoctorFees, whyLocked, } from './approval';
import type { ApprovalStatus } from './approval';
export type ExternalFeeKind = 'LUMP_SUM_UNIT' | 'OUT_CLINIC';
export interface ExternalFeeListItem {
    id: string;
    kind: ExternalFeeKind;
    refDocDate: string;
    refDocNo: string;
    periodYear: number;
    periodMonth: number;
    companyCode: string | null;
    companyName: string | null;
    lineCount: number;
    totalAmount: number;
    approvalStatus: ApprovalStatus;
    cycleClosed: boolean;
    status: RecordStatus;
}
export interface ExternalFeeDetail {
    id: string;
    kind: ExternalFeeKind;
    refDocDate: string;
    refDocNo: string;
    periodYear: number;
    periodMonth: number;
    arCodeId: string;
    totalAmount: number;
    approvalStatus: ApprovalStatus;
    cycleClosed: boolean;
    decisionComment: string | null;
    decidedBy: string | null;
    decidedAt: string | null;
    status: RecordStatus;
    remark: string | null;
    rowVersion: string;
}
export interface ExternalFeeInput {
    kind: ExternalFeeKind;
    refDocDate: string;
    refDocNo: string;
    periodYear: number;
    periodMonth: number;
    arCodeId: string;
    status: RecordStatus;
    remark: string | null;
}
export const externalFeeApi = {
    resource: 'external-fees',
    list: (params, signal?: AbortSignal) => tenantApi<Paged<ExternalFeeListItem>>('/api/master-data/external-fees', { params, signal }),
    get: (id, signal?: AbortSignal) => tenantApi<ExternalFeeDetail>(`/api/master-data/external-fees/${id}`, { signal }),
    create: (input) => tenantApi<ExternalFeeDetail>('/api/master-data/external-fees', { method: 'POST', body: input }),
    update: (id, input, rowVersion) => tenantApi<ExternalFeeDetail>(`/api/master-data/external-fees/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
    remove: (id) => tenantApi<void>(`/api/master-data/external-fees/${id}`, { method: 'DELETE' }),
    history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/external-fees/${id}/history`, { signal }),
    exportXlsx: (params) => tenantApiBlob('/api/master-data/external-fees/export', { params }),
} satisfies CrudApi<ExternalFeeListItem, ExternalFeeDetail, ExternalFeeInput>;
export interface ExternalFeeLineRow {
    id: string;
    feeId: string;
    issueDate: string;
    doctorCodeId: string;
    doctorCode: string | null;
    doctorName: string | null;
    description: string | null;
    compareGuarantee: boolean;
    amount: number;
    attachmentUrl: string | null;
}
export interface ExternalFeeLineInput {
    feeId: string;
    issueDate: string;
    doctorCodeId: string;
    description: string | null;
    compareGuarantee: boolean;
    amount: number;
    attachmentUrl: string | null;
}
export const externalFeeLineApi = {
    resource: 'external-fee-lines',
    list: (params, signal?: AbortSignal) => tenantApi<Paged<ExternalFeeLineRow>>('/api/master-data/external-fee-lines', { params, signal }),
    get: (id, signal?: AbortSignal) => tenantApi<ExternalFeeLineRow & { rowVersion: string; }>(`/api/master-data/external-fee-lines/${id}`, { signal }),
    create: (input) => tenantApi<ExternalFeeLineRow & { rowVersion: string; }>('/api/master-data/external-fee-lines', { method: 'POST', body: input }),
    update: (id, input, rowVersion) => tenantApi<ExternalFeeLineRow & { rowVersion: string; }>(`/api/master-data/external-fee-lines/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
    remove: (id) => tenantApi<void>(`/api/master-data/external-fee-lines/${id}`, { method: 'DELETE' }),
    history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/external-fee-lines/${id}/history`, { signal }),
    exportXlsx: (params) => tenantApiBlob('/api/master-data/external-fee-lines/export', { params }),
} satisfies CrudApi<ExternalFeeLineRow, ExternalFeeLineRow & { rowVersion: string; }, ExternalFeeLineInput>;
const linesChild: ChildTableDef<ExternalFeeLineRow, ExternalFeeLineInput> = {
    title: 'รายละเอียดค่าแพทย์',
    description: 'แพทย์แต่ละท่านที่ออกไปในงานนี้ ยอดรวมของเอกสารคำนวณจากตารางนี้',
    api: externalFeeLineApi,
    parentKey: 'feeId',
    addLabel: 'เพิ่มแพทย์',
    emptyHint: 'ยังไม่มีรายการแพทย์ — เอกสารที่ไม่มีรายการแพทย์อนุมัติไม่ได้',
    columns: [
        { key: 'issueDate', header: 'วันที่ออกเอกสาร', format: 'date', width: '150px' },
        { key: 'doctorCode', header: 'รหัสแพทย์', width: '130px' },
        { key: 'doctorName', header: 'แพทย์' },
        { key: 'description', header: 'รายละเอียด' },
        {
            key: 'compareGuarantee',
            header: 'เทียบประกันรายได้',
            width: '160px',
            value: (row) => (row.compareGuarantee ? 'เทียบ' : 'ไม่เทียบ'),
        },
        {
            key: 'amount',
            header: 'รายได้ต่อครั้ง (บาท)',
            align: 'right',
            format: 'amount',
            width: '170px',
        },
    ],
    fields: [
        { kind: 'date', name: 'issueDate', label: 'วันที่ออกเอกสาร', required: true },
        {
            kind: 'lookup',
            name: 'doctorCodeId',
            label: 'แพทย์',
            resource: 'doctor-codes',
            required: true,
            width: 'lg',
        },
        { kind: 'text', name: 'description', label: 'รายละเอียด', maxLength: 500, width: 'full' },
        {
            kind: 'bool',
            name: 'compareGuarantee',
            label: 'เทียบประกันรายได้',
            trueLabel: 'เทียบ',
            falseLabel: 'ไม่เทียบ',
        },
        { kind: 'amount', name: 'amount', label: 'รายได้ต่อครั้ง (บาท)', required: true },
    ],
    schema: z.object({
        feeId: z.string(),
        issueDate: z.string().min(1, 'โปรดระบุวันที่ออกเอกสาร'),
        doctorCodeId: z.string().min(1, 'โปรดระบุแพทย์'),
        description: z.string().trim().max(500).nullable(),
        compareGuarantee: z.boolean(),
        amount: z
            .number({ invalid_type_error: 'โปรดระบุรายได้ต่อครั้ง (บาท)' })
            .positive('โปรดระบุรายได้ต่อครั้ง (บาท)'),
        attachmentUrl: z.string().nullable(),
    }),
    defaultValues: {
        feeId: '',
        issueDate: '',
        doctorCodeId: '',
        description: null,
        compareGuarantee: false,
        amount: 0,
        attachmentUrl: null,
    },
    toInput: (row) => ({
        feeId: row.feeId,
        issueDate: row.issueDate,
        doctorCodeId: row.doctorCodeId,
        description: row.description,
        compareGuarantee: row.compareGuarantee,
        amount: row.amount,
        attachmentUrl: row.attachmentUrl,
    }),
    rowKey: (row) => row.id,
    defaultSort: 'issueDate',
};
const schema = z.object({
    kind: z.enum(['LUMP_SUM_UNIT', 'OUT_CLINIC']),
    refDocDate: z.string().min(1, 'โปรดระบุวันที่เอกสารอ้างอิง'),
    refDocNo: z.string().trim().min(1, 'โปรดระบุเลขที่เอกสารอ้างอิง').max(50),
    periodYear: z
        .number({ invalid_type_error: 'โปรดระบุปีเป็น ค.ศ.' })
        .int()
        .min(2000, 'โปรดระบุปีเป็น ค.ศ.')
        .max(2100, 'โปรดระบุปีเป็น ค.ศ.'),
    periodMonth: z.number({ invalid_type_error: 'โปรดระบุค่าแพทย์รอบเดือน' }).int().min(1).max(12),
    arCodeId: z.string().min(1, 'โปรดระบุบริษัท'),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().max(1000).nullable(),
});
function externalFeeScreen(options: {
    id: string;
    path: string;
    titleTh: string;
    kind: ExternalFeeKind;
    emptyHint: string;
}): ScreenDescriptor<ExternalFeeListItem, ExternalFeeDetail, ExternalFeeInput> {
    return {
        id: options.id,
        resource: 'external-fees',
        path: options.path,
        titleTh: options.titleTh,
        breadcrumb: BREADCRUMB,
        api: externalFeeApi,
        columns: [
            { key: 'refDocDate', header: 'วันที่เอกสาร', sortable: true, format: 'date', width: '130px' },
            { key: 'refDocNo', header: 'เลขที่เอกสาร', sortable: true, width: '150px' },
            {
                key: 'period',
                header: 'รอบเดือน',
                sortable: true,
                width: '160px',
                value: (row) => periodLabel(row.periodYear, row.periodMonth),
            },
            { key: 'companyName', header: 'บริษัท', sortable: true },
            { key: 'lineCount', header: 'แพทย์', align: 'right', width: '90px' },
            {
                key: 'totalAmount',
                header: 'รวมยอดจ่าย (บาท)',
                sortable: true,
                align: 'right',
                format: 'amount',
                width: '170px',
            },
            {
                key: 'approvalStatus',
                header: 'สถานะ',
                sortable: true,
                format: 'status',
                width: '170px',
            },
            {
                key: 'cycleClosed',
                header: 'รอบชำระ',
                width: '140px',
                value: (row) => cycleLabel(row.cycleClosed),
            },
        ],
        filters: [
            { kind: 'select', name: 'approvalStatus', label: 'สถานะ', options: APPROVAL_STATUS_OPTIONS },
            { kind: 'select', name: 'year', label: 'ปี', options: yearOptions() },
            { kind: 'select', name: 'month', label: 'เดือน', options: MONTH_OPTIONS },
            { kind: 'select', name: 'cycleClosed', label: 'สถานะรอบชำระ', options: CYCLE_OPTIONS },
            { kind: 'lookup', name: 'arCodeId', label: 'บริษัท', resource: 'ar-codes' },
            { kind: 'date', name: 'refDocDate', label: 'วันที่เอกสาร' },
        ],
        fixedFilters: { kind: options.kind },
        searchHint: 'ค้นหาเลขที่เอกสาร หรือชื่อบริษัท',
        defaultSort: '-refDocDate',
        rowKey: (row) => row.id,
        emptyHint: options.emptyHint,
        sections: [
            {
                title: 'เอกสารอ้างอิง',
                description: 'บันทึกเอกสารก่อน แล้วจึงเพิ่มรายการแพทย์ในตารางด้านล่าง',
                fields: [
                    { kind: 'date', name: 'refDocDate', label: 'วันที่เอกสารอ้างอิง', required: true },
                    { kind: 'text', name: 'refDocNo', label: 'เลขที่เอกสารอ้างอิง', required: true, maxLength: 50 },
                    {
                        kind: 'lookup',
                        name: 'arCodeId',
                        label: 'บริษัท',
                        resource: 'ar-codes',
                        required: true,
                        width: 'lg',
                    },
                ],
            },
            {
                title: 'ค่าแพทย์รอบเดือน',
                description: 'งวดที่รายการนี้เข้าไปอยู่ในการคำนวณรายเดือน ไม่จำเป็นต้องเป็นเดือนของวันที่เอกสาร',
                fields: [
                    {
                        kind: 'select',
                        name: 'periodMonth',
                        label: 'เดือน',
                        required: true,
                        options: MONTH_OPTIONS,
                    },
                    { kind: 'number', name: 'periodYear', label: 'ปี (ค.ศ.)', required: true, min: 2000, max: 2100 },
                ],
            },
            { title: 'อื่น ๆ', fields: [STATUS_FIELD, REMARK_FIELD] },
        ],
        schema: schema as never,
        defaultValues: {
            kind: options.kind,
            refDocDate: '',
            refDocNo: '',
            periodYear: new Date().getFullYear(),
            periodMonth: new Date().getMonth() + 1,
            arCodeId: '',
            status: 'ACTIVE',
            remark: null,
        },
        toInput: (detail) => ({
            kind: detail.kind,
            refDocDate: detail.refDocDate,
            refDocNo: detail.refDocNo,
            periodYear: detail.periodYear,
            periodMonth: detail.periodMonth,
            arCodeId: detail.arCodeId,
            status: detail.status,
            remark: detail.remark,
        }),
        childTables: [linesChild],
        approval: {
            decide: decideDoctorFees('external-fees'),
            permission: APPROVE_PERMISSION,
            statusFilter: 'approvalStatus',
        },
        isLocked: whyLocked,
    };
}
export const lumpSumUnitFeeScreen = externalFeeScreen({
    id: 'lump-sum-unit-fee',
    path: '/doctor-fee-402/lump-sum-unit',
    titleTh: 'ค่าแพทย์ออกหน่วยเหมาจ่าย',
    kind: 'LUMP_SUM_UNIT',
    emptyHint: 'บันทึกจากเอกสารของบริษัทที่ว่าจ้างให้แพทย์ออกหน่วยตรวจนอกสถานที่ แล้วส่งให้บัญชีแพทย์อนุมัติ',
});
export const outClinicFeeScreen = externalFeeScreen({
    id: 'out-clinic-fee',
    path: '/doctor-fee-402/out-clinic',
    titleTh: 'ค่าแพทย์ Out Clinic',
    kind: 'OUT_CLINIC',
    emptyHint: 'บันทึกจากเอกสารของคลินิกภายนอกที่แพทย์ออกไปตรวจ แล้วส่งให้บัญชีแพทย์อนุมัติ',
});
