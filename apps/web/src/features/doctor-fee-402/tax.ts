import { z } from 'zod';
import { createCrudApi } from '../../api/crud';
import type { RecordStatus } from '../../api/types';
import type { SelectOption } from '../../components/form/Select';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD, omitFields } from '../master-data/descriptor';
import type { ChildTableDef, ScreenDescriptor } from '../master-data/descriptor';
import { BREADCRUMB } from './approval';
const DOCTOR_FIELD = {
    kind: 'lookup',
    name: 'doctorCodeId',
    label: 'แพทย์',
    resource: 'doctor-codes',
    required: true,
    width: 'lg',
} as const;
const TAX_YEAR_FIELD = {
    kind: 'number',
    name: 'taxYear',
    label: 'ปีภาษี (ค.ศ.)',
    required: true,
    min: 2000,
    max: 2100,
} as const;
const taxYearSchema = z
    .number({ invalid_type_error: 'โปรดระบุปีภาษี' })
    .int()
    .min(2000, 'โปรดระบุปีภาษีเป็น ค.ศ.')
    .max(2100, 'โปรดระบุปีภาษีเป็น ค.ศ.');
const taxYearOptions = (): readonly SelectOption[] => {
    const year = new Date().getFullYear();
    return Array.from({ length: 5 }, (_, i) => ({ value: String(year - i), label: String(year - i) }));
};
export interface HospitalPaidTaxListItem {
    id: string;
    doctorCode: string | null;
    doctorName: string | null;
    taxId: string | null;
    status: RecordStatus;
}
export interface HospitalPaidTaxDetail {
    id: string;
    doctorCodeId: string;
    status: RecordStatus;
    remark: string | null;
    rowVersion: string;
}
export type HospitalPaidTaxInput = Omit<HospitalPaidTaxDetail, 'id' | 'rowVersion'>;
export const hospitalPaidTaxScreen: ScreenDescriptor<HospitalPaidTaxListItem, HospitalPaidTaxDetail, HospitalPaidTaxInput> = {
    id: 'hospital-paid-tax',
    resource: 'hospital-paid-taxes',
    path: '/doctor-fee-402/hospital-paid-tax',
    titleTh: 'ตั้งค่าภาษีโรงพยาบาลออกให้',
    breadcrumb: BREADCRUMB,
    api: createCrudApi<HospitalPaidTaxListItem, HospitalPaidTaxDetail, HospitalPaidTaxInput>('hospital-paid-taxes'),
    columns: [
        { key: 'doctorCode', header: 'รหัสแพทย์', sortable: true, width: '150px' },
        { key: 'doctorName', header: 'แพทย์' },
        { key: 'taxId', header: 'เลขประจำตัวผู้เสียภาษี', width: '200px' },
        STATUS_COLUMN,
    ],
    filters: [{ kind: 'status', name: 'status', label: 'สถานะ' }],
    searchHint: 'ค้นหารหัสแพทย์ หรือชื่อแพทย์',
    defaultSort: 'doctorCode',
    rowKey: (row) => row.id,
    emptyHint: 'แพทย์ที่โรงพยาบาลเป็นผู้ออกภาษีหัก ณ ที่จ่ายแทน ระบบจะไม่หักภาษีจากยอดจ่ายของแพทย์ท่านนั้น',
    sections: [{ fields: [DOCTOR_FIELD, STATUS_FIELD, REMARK_FIELD] }],
    schema: z.object({
        doctorCodeId: z.string().min(1, 'โปรดระบุแพทย์'),
        status: z.enum(['ACTIVE', 'INACTIVE']),
        remark: z.string().trim().max(1000).nullable(),
    }),
    defaultValues: { doctorCodeId: '', status: 'ACTIVE', remark: null },
    toInput: (detail) => omitFields(detail, 'id', 'rowVersion'),
};
export interface TaxDeductionListItem {
    id: string;
    doctorCode: string | null;
    doctorName: string | null;
    taxId: string | null;
    taxYear: number;
    childCount: number;
    itemCount: number;
    totalAmount: number;
    status: RecordStatus;
}
export interface TaxDeductionDetail {
    id: string;
    doctorCodeId: string;
    taxYear: number;
    childCount: number;
    status: RecordStatus;
    remark: string | null;
    rowVersion: string;
}
export type TaxDeductionInput = Omit<TaxDeductionDetail, 'id' | 'rowVersion'>;
export interface TaxDeductionItemRow {
    id: string;
    deductionId: string;
    taxAllowanceItemId: string;
    allowanceName: string | null;
    allowanceLimit: number | null;
    amount: number;
}
export interface TaxDeductionItemInput {
    deductionId: string;
    taxAllowanceItemId: string;
    amount: number;
}
const deductionItemsChild: ChildTableDef<TaxDeductionItemRow, TaxDeductionItemInput> = {
    title: 'รายการลดหย่อน',
    description: 'ยอดลดหย่อนแต่ละรายการตามทะเบียนรายการลดหย่อนของปีภาษีนั้น (หน้าจอ 32)',
    api: createCrudApi<TaxDeductionItemRow, TaxDeductionItemRow & {
        rowVersion: string;
    }, TaxDeductionItemInput>('tax-deduction-items'),
    parentKey: 'deductionId',
    addLabel: 'เพิ่มรายการลดหย่อน',
    emptyHint: 'ยังไม่มีรายการลดหย่อนของปีภาษีนี้',
    columns: [
        { key: 'allowanceName', header: 'รายการลดหย่อน' },
        {
            key: 'allowanceLimit',
            header: 'เพดาน (บาท)',
            align: 'right',
            format: 'amount',
            width: '160px',
        },
        { key: 'amount', header: 'ยอดลดหย่อน (บาท)', align: 'right', format: 'amount', width: '170px' },
    ],
    fields: [
        {
            kind: 'lookup',
            name: 'taxAllowanceItemId',
            label: 'รายการลดหย่อน',
            resource: 'tax-allowance-items',
            required: true,
            width: 'lg',
            hint: 'ตัวเลือกขึ้นต้นด้วยปีภาษี — เลือกรายการของปีเดียวกับข้อมูลลดหย่อนนี้',
        },
        { kind: 'amount', name: 'amount', label: 'ยอดลดหย่อน (บาท)', required: true },
    ],
    schema: z.object({
        deductionId: z.string(),
        taxAllowanceItemId: z.string().min(1, 'โปรดเลือกรายการลดหย่อน'),
        amount: z.number({ invalid_type_error: 'โปรดระบุยอดลดหย่อน' }).min(0, 'ยอดลดหย่อนต้องไม่ติดลบ'),
    }),
    defaultValues: { deductionId: '', taxAllowanceItemId: '', amount: 0 },
    toInput: (row) => ({
        deductionId: row.deductionId,
        taxAllowanceItemId: row.taxAllowanceItemId,
        amount: row.amount,
    }),
    rowKey: (row) => row.id,
    defaultSort: 'allowanceName',
};
export const taxDeductionScreen: ScreenDescriptor<TaxDeductionListItem, TaxDeductionDetail, TaxDeductionInput> = {
    id: 'tax-deduction',
    resource: 'tax-deductions',
    path: '/doctor-fee-402/tax-deduction',
    titleTh: 'ตั้งค่าภาษีลดหย่อน',
    breadcrumb: BREADCRUMB,
    api: createCrudApi<TaxDeductionListItem, TaxDeductionDetail, TaxDeductionInput>('tax-deductions'),
    columns: [
        { key: 'doctorCode', header: 'รหัสแพทย์', sortable: true, width: '130px' },
        { key: 'doctorName', header: 'แพทย์' },
        { key: 'taxId', header: 'เลขประจำตัวผู้เสียภาษี', width: '190px' },
        { key: 'taxYear', header: 'ปีภาษี', sortable: true, width: '100px' },
        { key: 'childCount', header: 'จำนวนบุตร', sortable: true, align: 'right', width: '110px' },
        {
            key: 'totalAmount',
            header: 'ยอดลดหย่อนรวม (บาท)',
            align: 'right',
            format: 'amount',
            width: '180px',
        },
        STATUS_COLUMN,
    ],
    filters: [
        { kind: 'select', name: 'taxYear', label: 'ปีภาษี', options: taxYearOptions() },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหารหัสแพทย์ ชื่อแพทย์ หรือเลขประจำตัวผู้เสียภาษี',
    defaultSort: '-taxYear',
    rowKey: (row) => row.id,
    emptyHint: 'ข้อมูลลดหย่อนของแพทย์รายปีภาษี ใช้คำนวณภาษีเงินได้ 40(2) และออกหนังสือรับรอง 50 ทวิ',
    sections: [
        {
            title: 'ข้อมูลแพทย์',
            description: 'บันทึกปีภาษีก่อน แล้วจึงเพิ่มรายการลดหย่อนในตารางด้านล่าง',
            fields: [
                DOCTOR_FIELD,
                TAX_YEAR_FIELD,
                { kind: 'number', name: 'childCount', label: 'จำนวนบุตร (คน)', min: 0, max: 20 },
            ],
        },
        { title: 'อื่น ๆ', fields: [STATUS_FIELD, REMARK_FIELD] },
    ],
    schema: z.object({
        doctorCodeId: z.string().min(1, 'โปรดระบุแพทย์'),
        taxYear: taxYearSchema,
        childCount: z.number({ invalid_type_error: 'โปรดระบุจำนวนบุตร' }).int().min(0, 'จำนวนบุตรต้องไม่ติดลบ'),
        status: z.enum(['ACTIVE', 'INACTIVE']),
        remark: z.string().trim().max(1000).nullable(),
    }),
    defaultValues: {
        doctorCodeId: '',
        taxYear: new Date().getFullYear(),
        childCount: 0,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => omitFields(detail, 'id', 'rowVersion'),
    childTables: [deductionItemsChild],
};
export interface TaxExemptionListItem {
    id: string;
    doctorCode: string | null;
    doctorName: string | null;
    taxId: string | null;
    taxYear: number;
    isExempt: boolean;
    status: RecordStatus;
}
export interface TaxExemptionDetail {
    id: string;
    doctorCodeId: string;
    taxYear: number;
    isExempt: boolean;
    status: RecordStatus;
    remark: string | null;
    rowVersion: string;
}
export type TaxExemptionInput = Omit<TaxExemptionDetail, 'id' | 'rowVersion'>;
export const taxExemptionScreen: ScreenDescriptor<TaxExemptionListItem, TaxExemptionDetail, TaxExemptionInput> = {
    id: 'tax-exemption',
    resource: 'tax-exemptions',
    path: '/doctor-fee-402/tax-exemption',
    titleTh: 'ข้อมูลยกเว้นภาษี',
    breadcrumb: BREADCRUMB,
    api: createCrudApi<TaxExemptionListItem, TaxExemptionDetail, TaxExemptionInput>('tax-exemptions'),
    columns: [
        { key: 'doctorCode', header: 'รหัสแพทย์', sortable: true, width: '130px' },
        { key: 'doctorName', header: 'แพทย์' },
        { key: 'taxId', header: 'เลขประจำตัวผู้เสียภาษี', width: '190px' },
        { key: 'taxYear', header: 'ปีภาษี', sortable: true, width: '100px' },
        {
            key: 'isExempt',
            header: 'ยกเว้นภาษี',
            sortable: true,
            width: '130px',
            value: (row) => (row.isExempt ? 'ยกเว้น' : 'ไม่ยกเว้น'),
        },
        STATUS_COLUMN,
    ],
    filters: [
        { kind: 'select', name: 'taxYear', label: 'ปีภาษี', options: taxYearOptions() },
        {
            kind: 'select',
            name: 'isExempt',
            label: 'ยกเว้นภาษี',
            options: [
                { value: 'true', label: 'ยกเว้น' },
                { value: 'false', label: 'ไม่ยกเว้น' },
            ],
        },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหารหัสแพทย์ ชื่อแพทย์ หรือเลขประจำตัวผู้เสียภาษี',
    defaultSort: '-taxYear',
    rowKey: (row) => row.id,
    emptyHint: 'แพทย์ที่ได้รับยกเว้นภาษีในปีภาษีนั้น การคำนวณภาษี 40(2) จะไม่หักภาษีจากยอดจ่ายของแพทย์ท่านนั้น',
    sections: [
        {
            fields: [
                DOCTOR_FIELD,
                TAX_YEAR_FIELD,
                {
                    kind: 'bool',
                    name: 'isExempt',
                    label: 'ยกเว้นภาษี',
                    trueLabel: 'ยกเว้น',
                    falseLabel: 'ไม่ยกเว้น',
                },
                STATUS_FIELD,
                REMARK_FIELD,
            ],
        },
    ],
    schema: z.object({
        doctorCodeId: z.string().min(1, 'โปรดระบุแพทย์'),
        taxYear: taxYearSchema,
        isExempt: z.boolean(),
        status: z.enum(['ACTIVE', 'INACTIVE']),
        remark: z.string().trim().max(1000).nullable(),
    }),
    defaultValues: {
        doctorCodeId: '',
        taxYear: new Date().getFullYear(),
        isExempt: true,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => omitFields(detail, 'id', 'rowVersion'),
};
