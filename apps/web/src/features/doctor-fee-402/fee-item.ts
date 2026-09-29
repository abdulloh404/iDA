import { z } from 'zod';
import { createCrudApi } from '../../api/crud';
import type { RecordStatus } from '../../api/types';
import type { SelectOption } from '../../components/form/Select';
import { REMARK_FIELD, STATUS_FIELD } from '../master-data/descriptor';
import type { ScreenDescriptor } from '../master-data/descriptor';
import { MONTH_OPTIONS, periodLabel, yearOptions } from '../shared/period';
import { APPROVAL_STATUS_OPTIONS, APPROVE_PERMISSION, BREADCRUMB, CYCLE_OPTIONS, cycleLabel, decideDoctorFees, whyLocked, } from './approval';
import type { ApprovalStatus } from './approval';
export const FEE_ITEM_TYPE_OPTIONS: readonly SelectOption[] = [
    { value: 'MEETING_ALLOWANCE', label: 'ค่าเบี้ยประชุม' },
    { value: 'SPEAKER_PROMOTION', label: 'ค่าแพทย์วิทยากร-Promotion' },
    { value: 'SPEAKER_PROMOTION_EVENT', label: 'ค่าแพทย์วิทยากร-Promotion Event' },
    { value: 'SPEAKER_TRAINING', label: 'ค่าแพทย์วิทยากร-Training' },
    { value: 'ANNUAL_COMPENSATION', label: 'ค่าตอบแทนรายปี' },
    { value: 'PER_CASE_COMPENSATION', label: 'ค่าตอบแทนพิเศษตามจำนวนราย' },
    { value: 'HEALTH_CHECK_CONSULTANT', label: 'ค่าแพทย์ที่ปรึกษาผลการตรวจสุขภาพ' },
    { value: 'TRAVEL_COMPENSATION', label: 'ค่าตอบแทนค่าเดินแพทย์' },
];
const itemTypeLabel = (value: string) => FEE_ITEM_TYPE_OPTIONS.find((o) => o.value === value)?.label ?? value;
export interface FeeItemListItem {
    id: string;
    refDocDate: string;
    refDocNo: string;
    periodYear: number;
    periodMonth: number;
    itemType: string;
    doctorCode: string | null;
    doctorName: string | null;
    departmentName: string | null;
    amount: number;
    approvalStatus: ApprovalStatus;
    cycleClosed: boolean;
    status: RecordStatus;
}
export interface FeeItemDetail {
    id: string;
    refDocDate: string;
    refDocNo: string;
    periodYear: number;
    periodMonth: number;
    itemType: string;
    doctorCodeId: string;
    departmentId: string | null;
    arCodeId: string | null;
    amount: number;
    approvalStatus: ApprovalStatus;
    cycleClosed: boolean;
    decisionComment: string | null;
    decidedBy: string | null;
    decidedAt: string | null;
    status: RecordStatus;
    remark: string | null;
    rowVersion: string;
}
export interface FeeItemInput {
    refDocDate: string;
    refDocNo: string;
    periodYear: number;
    periodMonth: number;
    itemType: string;
    doctorCodeId: string;
    departmentId: string | null;
    arCodeId: string | null;
    amount: number;
    status: RecordStatus;
    remark: string | null;
}
const schema = z.object({
    refDocDate: z.string().min(1, 'โปรดระบุวันที่เอกสารอ้างอิง'),
    refDocNo: z.string().trim().min(1, 'โปรดระบุเลขที่เอกสารอ้างอิง').max(50),
    periodYear: z
        .number({ invalid_type_error: 'โปรดระบุปีเป็น ค.ศ.' })
        .int()
        .min(2000, 'โปรดระบุปีเป็น ค.ศ.')
        .max(2100, 'โปรดระบุปีเป็น ค.ศ.'),
    periodMonth: z.number({ invalid_type_error: 'โปรดระบุค่าแพทย์รอบเดือน' }).int().min(1).max(12),
    itemType: z.string().min(1, 'โปรดระบุประเภทรายการ'),
    doctorCodeId: z.string().min(1, 'โปรดระบุแพทย์'),
    departmentId: z.string().nullable(),
    arCodeId: z.string().nullable(),
    amount: z.number({ invalid_type_error: 'โปรดระบุรายได้ (บาท)' }).positive('โปรดระบุรายได้ (บาท)'),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().max(1000).nullable(),
});
export const feeItemScreen: ScreenDescriptor<FeeItemListItem, FeeItemDetail, FeeItemInput> = {
    id: 'fee-item',
    resource: 'fee-items',
    path: '/doctor-fee-402/fee-item',
    titleTh: 'รายการค่าแพทย์',
    breadcrumb: BREADCRUMB,
    api: createCrudApi<FeeItemListItem, FeeItemDetail, FeeItemInput>('fee-items'),
    columns: [
        { key: 'refDocDate', header: 'วันที่เอกสาร', sortable: true, format: 'date', width: '130px' },
        { key: 'refDocNo', header: 'เลขที่เอกสาร', sortable: true, width: '140px' },
        {
            key: 'period',
            header: 'รอบเดือน',
            sortable: true,
            width: '150px',
            value: (row) => periodLabel(row.periodYear, row.periodMonth),
        },
        {
            key: 'itemType',
            header: 'ประเภทรายการ',
            sortable: true,
            value: (row) => itemTypeLabel(row.itemType),
        },
        { key: 'doctorCode', header: 'รหัสแพทย์', sortable: true, width: '120px' },
        { key: 'doctorName', header: 'แพทย์' },
        {
            key: 'amount',
            header: 'จำนวนเงิน (บาท)',
            sortable: true,
            align: 'right',
            format: 'amount',
            width: '150px',
        },
        { key: 'approvalStatus', header: 'สถานะ', sortable: true, format: 'status', width: '170px' },
        {
            key: 'cycleClosed',
            header: 'รอบชำระ',
            width: '130px',
            value: (row) => cycleLabel(row.cycleClosed),
        },
    ],
    filters: [
        { kind: 'select', name: 'approvalStatus', label: 'สถานะ', options: APPROVAL_STATUS_OPTIONS },
        { kind: 'select', name: 'year', label: 'ปี', options: yearOptions() },
        { kind: 'select', name: 'month', label: 'เดือน', options: MONTH_OPTIONS },
        { kind: 'select', name: 'itemType', label: 'ประเภทรายการ', options: FEE_ITEM_TYPE_OPTIONS },
        { kind: 'select', name: 'cycleClosed', label: 'สถานะรอบชำระ', options: CYCLE_OPTIONS },
        { kind: 'lookup', name: 'doctorCodeId', label: 'แพทย์', resource: 'doctor-codes' },
        { kind: 'lookup', name: 'departmentId', label: 'แผนก', resource: 'departments' },
        { kind: 'date', name: 'refDocDate', label: 'วันที่เอกสาร' },
    ],
    searchHint: 'ค้นหาเลขที่เอกสาร รหัสแพทย์ หรือชื่อแพทย์',
    defaultSort: '-refDocDate',
    rowKey: (row) => row.id,
    emptyHint: 'ค่าตอบแทนครั้งคราวที่ไม่ได้มาจากการรักษา เช่น เบี้ยประชุม ค่าวิทยากร — บันทึกแล้วส่งให้บัญชีแพทย์อนุมัติ',
    sections: [
        {
            title: 'เอกสารอ้างอิง',
            fields: [
                { kind: 'date', name: 'refDocDate', label: 'วันที่เอกสารอ้างอิง', required: true },
                { kind: 'text', name: 'refDocNo', label: 'เลขที่เอกสารอ้างอิง', required: true, maxLength: 50 },
                { kind: 'select', name: 'periodMonth', label: 'ค่าแพทย์รอบเดือน', required: true, options: MONTH_OPTIONS },
                { kind: 'number', name: 'periodYear', label: 'ปี (ค.ศ.)', required: true, min: 2000, max: 2100 },
            ],
        },
        {
            title: 'รายละเอียด',
            fields: [
                {
                    kind: 'select',
                    name: 'itemType',
                    label: 'ประเภทรายการ',
                    required: true,
                    options: FEE_ITEM_TYPE_OPTIONS,
                    width: 'lg',
                },
                {
                    kind: 'lookup',
                    name: 'doctorCodeId',
                    label: 'แพทย์',
                    resource: 'doctor-codes',
                    required: true,
                    width: 'lg',
                },
                { kind: 'lookup', name: 'departmentId', label: 'แผนก', resource: 'departments', emptyLabel: 'ไม่ระบุ' },
                { kind: 'lookup', name: 'arCodeId', label: 'รหัสลูกค้า (AR Code)', resource: 'ar-codes', emptyLabel: 'ไม่ระบุ' },
                { kind: 'amount', name: 'amount', label: 'รายได้ (บาท)', required: true },
            ],
        },
        { title: 'อื่น ๆ', fields: [STATUS_FIELD, REMARK_FIELD] },
    ],
    schema: schema as never,
    defaultValues: {
        refDocDate: '',
        refDocNo: '',
        periodYear: new Date().getFullYear(),
        periodMonth: new Date().getMonth() + 1,
        itemType: '',
        doctorCodeId: '',
        departmentId: null,
        arCodeId: null,
        amount: 0,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => ({
        refDocDate: detail.refDocDate,
        refDocNo: detail.refDocNo,
        periodYear: detail.periodYear,
        periodMonth: detail.periodMonth,
        itemType: detail.itemType,
        doctorCodeId: detail.doctorCodeId,
        departmentId: detail.departmentId,
        arCodeId: detail.arCodeId,
        amount: detail.amount,
        status: detail.status,
        remark: detail.remark,
    }),
    approval: {
        decide: decideDoctorFees('fee-items'),
        permission: APPROVE_PERMISSION,
        statusFilter: 'approvalStatus',
    },
    isLocked: whyLocked,
};
