import { z } from 'zod';
import { tenantApi, tenantApiBlob } from '../../api/client';
import type { CrudApi } from '../../api/crud';
import type { AuditEntry, Paged, RecordStatus } from '../../api/types';
import { STATUS_COLUMN, STATUS_FIELD } from '../master-data/descriptor';
import type { ChildTableDef } from '../master-data/descriptor';
import { ACCOUNT_TYPE_OPTIONS, CONTRACT_STATUS_OPTIONS, CONTRACT_TYPE_OPTIONS, } from './options';
export interface BankAccountRow {
    id: string;
    doctorId: string;
    bankName: string | null;
    branchName: string | null;
    accountNoLast4: string;
    accountName: string;
    accountType: string;
    paymentTypeName: string | null;
    effectiveFrom: string;
    isActive: boolean;
    status: RecordStatus;
}
interface BankAccountInput {
    doctorId: string;
    bankBranchId: string;
    accountNo: string | null;
    accountName: string;
    accountType: string;
    paymentTypeId: string;
    payeeName: string | null;
    pfemVendorCode: string | null;
    bookBankDocUrl: string | null;
    effectiveFrom: string;
    effectiveTo: string | null;
    isActive: boolean;
    status: RecordStatus;
    remark: string | null;
}
export const bankAccountsChild: ChildTableDef<BankAccountRow, BankAccountInput> = {
    title: 'บัญชีธนาคารสำหรับทำจ่าย',
    description: 'แพทย์หนึ่งท่านมีบัญชีที่ใช้งานอยู่ได้เพียงบัญชีเดียว',
    api: {
        resource: 'doctor-bank-accounts',
        list: (params, signal?: AbortSignal) => tenantApi<Paged<BankAccountRow>>('/api/master-data/doctor-bank-accounts', { params, signal }),
        get: (id, signal?: AbortSignal) => tenantApi<BankAccountRow & { rowVersion: string; }>(`/api/master-data/doctor-bank-accounts/${id}`, { signal }),
        create: (input) => tenantApi<BankAccountRow & { rowVersion: string; }>('/api/master-data/doctor-bank-accounts', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => tenantApi<BankAccountRow & { rowVersion: string; }>(`/api/master-data/doctor-bank-accounts/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => tenantApi<void>(`/api/master-data/doctor-bank-accounts/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/doctor-bank-accounts/${id}/history`, { signal }),
        exportXlsx: (params) => tenantApiBlob('/api/master-data/doctor-bank-accounts/export', { params }),
    } satisfies CrudApi<BankAccountRow, BankAccountRow & { rowVersion: string; }, BankAccountInput>,
    parentKey: 'doctorId',
    parentValueFrom: 'doctorId',
    rowKey: (row) => row.id,
    addLabel: 'เพิ่มบัญชีธนาคาร',
    columns: [
        { key: 'bankName', header: 'ธนาคาร', width: '200px' },
        { key: 'branchName', header: 'สาขา', width: '200px' },
        { key: 'accountNoLast4', header: 'เลขบัญชี (4 ตัวท้าย)', width: '180px' },
        { key: 'accountName', header: 'ชื่อบัญชี' },
        { key: 'paymentTypeName', header: 'ประเภทการจ่าย', width: '170px' },
        {
            key: 'isActive',
            header: 'ใช้งานอยู่',
            width: '130px',
            value: (row) => (row.isActive ? 'ใช้อยู่' : 'ปิดแล้ว'),
        },
        STATUS_COLUMN,
    ],
    fields: [
        {
            kind: 'lookup',
            name: 'bankBranchId',
            label: 'สาขาธนาคาร',
            resource: 'bank-branches',
            required: true,
            width: 'lg',
        },
        {
            kind: 'text',
            name: 'accountNo',
            label: 'เลขที่บัญชีธนาคาร',
            hint: 'เก็บแบบเข้ารหัส เว้นว่างไว้คือไม่แก้ของเดิม',
        },
        { kind: 'text', name: 'accountName', label: 'ชื่อบัญชี', required: true, width: 'lg' },
        {
            kind: 'select',
            name: 'accountType',
            label: 'ชนิดบัญชี',
            required: true,
            options: ACCOUNT_TYPE_OPTIONS,
        },
        {
            kind: 'lookup',
            name: 'paymentTypeId',
            label: 'ประเภทการจ่ายเงิน',
            resource: 'payment-types',
            required: true,
        },
        { kind: 'text', name: 'payeeName', label: 'ชื่อผู้รับเช็ค', hint: 'ใช้เมื่อจ่ายด้วยเช็ค' },
        { kind: 'text', name: 'pfemVendorCode', label: 'PFEM Vendor Code', width: 'sm' },
        { kind: 'text', name: 'bookBankDocUrl', label: 'สำเนาหน้าบัญชี', width: 'lg' },
        { kind: 'date', name: 'effectiveFrom', label: 'ใช้ตั้งแต่', required: true },
        { kind: 'date', name: 'effectiveTo', label: 'ถึงวันที่' },
        {
            kind: 'bool',
            name: 'isActive',
            label: 'บัญชีที่ใช้งานอยู่',
            trueLabel: 'ใช้อยู่',
            falseLabel: 'ปิดแล้ว',
        },
        STATUS_FIELD,
        { kind: 'textarea', name: 'remark', label: 'หมายเหตุ', rows: 2 },
    ],
    schema: z
        .object({
        doctorId: z.string().min(1),
        bankBranchId: z.string().min(1, 'โปรดเลือกสาขาธนาคาร'),
        accountNo: z
            .string()
            .trim()
            .nullable()
            .refine((v) => !v || /^\d+$/.test(v), 'เลขที่บัญชีต้องเป็นตัวเลขเท่านั้น'),
        accountName: z.string().trim().min(1, 'โปรดระบุชื่อบัญชี'),
        accountType: z.string().min(1),
        paymentTypeId: z.string().min(1, 'โปรดเลือกประเภทการจ่ายเงิน'),
        payeeName: z.string().trim().nullable(),
        pfemVendorCode: z.string().trim().nullable(),
        bookBankDocUrl: z.string().trim().nullable(),
        effectiveFrom: z.string().min(1, 'โปรดระบุวันที่เริ่มใช้'),
        effectiveTo: z.string().nullable(),
        isActive: z.boolean(),
        status: z.enum(['ACTIVE', 'INACTIVE']),
        remark: z.string().trim().nullable(),
    })
        .refine((v) => !v.effectiveTo || v.effectiveTo >= v.effectiveFrom, {
        path: ['effectiveTo'],
        message: 'วันที่สิ้นสุดต้องไม่ก่อนวันที่เริ่มใช้',
    }),
    defaultValues: {
        doctorId: '',
        bankBranchId: '',
        accountNo: null,
        accountName: '',
        accountType: 'SAVING',
        paymentTypeId: '',
        payeeName: null,
        pfemVendorCode: null,
        bookBankDocUrl: null,
        effectiveFrom: '',
        effectiveTo: null,
        isActive: true,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (row) => ({
        doctorId: row.doctorId,
        bankBranchId: (row as unknown as {
            bankBranchId: string;
        }).bankBranchId,
        accountNo: null,
        accountName: row.accountName,
        accountType: row.accountType,
        paymentTypeId: (row as unknown as {
            paymentTypeId: string;
        }).paymentTypeId,
        payeeName: (row as unknown as {
            payeeName: string | null;
        }).payeeName,
        pfemVendorCode: (row as unknown as {
            pfemVendorCode: string | null;
        }).pfemVendorCode,
        bookBankDocUrl: (row as unknown as {
            bookBankDocUrl: string | null;
        }).bookBankDocUrl,
        effectiveFrom: row.effectiveFrom,
        effectiveTo: (row as unknown as {
            effectiveTo: string | null;
        }).effectiveTo,
        isActive: row.isActive,
        status: row.status,
        remark: (row as unknown as {
            remark: string | null;
        }).remark,
    }),
    defaultSort: '-effectiveFrom',
    emptyHint: 'บัญชีที่ใช้ทำจ่ายค่าแพทย์ผ่าน PFEM',
};
export interface CodeSpecialtyRow {
    id: string;
    doctorId: string;
    doctorCodeId: string | null;
    specialtyName: string | null;
    subSpecialtyName: string | null;
    isPrimary: boolean;
    otherSpecialty: string | null;
    boardCertNo: string | null;
    status: RecordStatus;
}
interface CodeSpecialtyInput {
    doctorId: string;
    doctorCodeId: string | null;
    specialtyId: string;
    subSpecialtyId: string | null;
    isPrimary: boolean;
    otherSpecialty: string | null;
    boardCertNo: string | null;
    boardCertDate: string | null;
    displaySeq: number | null;
    publishChannels: string[];
    effectiveFrom: string;
    effectiveTo: string | null;
    status: RecordStatus;
    remark: string | null;
}
export const codeSpecialtiesChild: ChildTableDef<CodeSpecialtyRow, CodeSpecialtyInput> = {
    title: 'ความเชี่ยวชาญที่ประกาศ',
    description: 'ความเชี่ยวชาญที่แสดงบนเว็บไซต์และช่องทางอื่นของโรงพยาบาลนี้',
    api: {
        resource: 'doctor-specialties',
        list: (params, signal?: AbortSignal) => tenantApi<Paged<CodeSpecialtyRow>>('/api/master-data/doctor-specialties', { params, signal }),
        get: (id, signal?: AbortSignal) => tenantApi<CodeSpecialtyRow & { rowVersion: string; }>(`/api/master-data/doctor-specialties/${id}`, { signal }),
        create: (input) => tenantApi<CodeSpecialtyRow & { rowVersion: string; }>('/api/master-data/doctor-specialties', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => tenantApi<CodeSpecialtyRow & { rowVersion: string; }>(`/api/master-data/doctor-specialties/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => tenantApi<void>(`/api/master-data/doctor-specialties/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/doctor-specialties/${id}/history`, { signal }),
        exportXlsx: (params) => tenantApiBlob('/api/master-data/doctor-specialties/export', { params }),
    } satisfies CrudApi<CodeSpecialtyRow, CodeSpecialtyRow & { rowVersion: string; }, CodeSpecialtyInput>,
    parentKey: 'doctorCodeId',
    rowKey: (row) => row.id,
    addLabel: 'เพิ่มความเชี่ยวชาญ',
    columns: [
        { key: 'specialtyName', header: 'ความเชี่ยวชาญ' },
        { key: 'subSpecialtyName', header: 'เฉพาะทาง' },
        {
            key: 'isPrimary',
            header: 'ความเชี่ยวชาญหลัก',
            width: '170px',
            value: (row) => (row.isPrimary ? 'ใช่' : 'ไม่ใช่'),
        },
        { key: 'boardCertNo', header: 'เลขที่วุฒิบัตร', width: '170px' },
        STATUS_COLUMN,
    ],
    fields: [
        {
            kind: 'lookup',
            name: 'specialtyId',
            label: 'ความเชี่ยวชาญ',
            resource: 'specialties',
            required: true,
            width: 'lg',
        },
        {
            kind: 'lookup',
            name: 'subSpecialtyId',
            label: 'ความเชี่ยวชาญเฉพาะทาง',
            resource: 'sub-specialties',
            emptyLabel: 'ไม่ระบุ',
            width: 'lg',
        },
        { kind: 'text', name: 'otherSpecialty', label: 'ความเชี่ยวชาญอื่น ๆ', width: 'lg' },
        {
            kind: 'bool',
            name: 'isPrimary',
            label: 'ความเชี่ยวชาญหลัก',
            trueLabel: 'ใช่',
            falseLabel: 'ไม่ใช่',
        },
        { kind: 'text', name: 'boardCertNo', label: 'เลขที่วุฒิบัตร' },
        { kind: 'date', name: 'boardCertDate', label: 'วันที่ได้วุฒิบัตร' },
        { kind: 'number', name: 'displaySeq', label: 'ลำดับการแสดงผล', min: 0 },
        { kind: 'date', name: 'effectiveFrom', label: 'มีผลตั้งแต่', required: true },
        { kind: 'date', name: 'effectiveTo', label: 'ถึงวันที่' },
        STATUS_FIELD,
    ],
    schema: z
        .object({
        doctorId: z.string().min(1),
        doctorCodeId: z.string().nullable(),
        specialtyId: z.string().min(1, 'โปรดเลือกความเชี่ยวชาญ'),
        subSpecialtyId: z.string().nullable(),
        isPrimary: z.boolean(),
        otherSpecialty: z.string().trim().nullable(),
        boardCertNo: z.string().trim().nullable(),
        boardCertDate: z.string().nullable(),
        displaySeq: z.number().int().min(0).nullable(),
        publishChannels: z.array(z.string()),
        effectiveFrom: z.string().min(1, 'โปรดระบุวันที่เริ่มมีผล'),
        effectiveTo: z.string().nullable(),
        status: z.enum(['ACTIVE', 'INACTIVE']),
        remark: z.string().trim().nullable(),
    })
        .refine((v) => !v.effectiveTo || v.effectiveTo >= v.effectiveFrom, {
        path: ['effectiveTo'],
        message: 'วันที่สิ้นสุดต้องไม่ก่อนวันที่เริ่มมีผล',
    }),
    defaultValues: {
        doctorId: '',
        doctorCodeId: null,
        specialtyId: '',
        subSpecialtyId: null,
        isPrimary: false,
        otherSpecialty: null,
        boardCertNo: null,
        boardCertDate: null,
        displaySeq: null,
        publishChannels: [],
        effectiveFrom: '',
        effectiveTo: null,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (row) => {
        const full = row as unknown as CodeSpecialtyInput & {
            rowVersion: string;
        };
        return {
            doctorId: full.doctorId,
            doctorCodeId: full.doctorCodeId,
            specialtyId: full.specialtyId,
            subSpecialtyId: full.subSpecialtyId,
            isPrimary: full.isPrimary,
            otherSpecialty: full.otherSpecialty,
            boardCertNo: full.boardCertNo,
            boardCertDate: full.boardCertDate,
            displaySeq: full.displaySeq,
            publishChannels: full.publishChannels,
            effectiveFrom: full.effectiveFrom,
            effectiveTo: full.effectiveTo,
            status: full.status,
            remark: full.remark,
        };
    },
    defaultSort: 'displaySeq',
    emptyHint: 'ความเชี่ยวชาญของแพทย์ตามที่โรงพยาบาลนี้ประกาศ',
};
export interface ContractRow {
    id: string;
    doctorId: string;
    doctorCodeId: string | null;
    contractNo: string;
    contractType: string;
    contractName: string | null;
    startDate: string;
    endDate: string | null;
    guaranteeAmount: number | null;
    contractStatus: string;
    status: RecordStatus;
}
interface ContractInput {
    doctorId: string;
    doctorCodeId: string | null;
    contractNo: string;
    contractType: string;
    contractName: string | null;
    startDate: string;
    endDate: string | null;
    autoRenew: boolean;
    noticeDays: number | null;
    guaranteeAmount: number | null;
    documentUrl: string | null;
    signedDate: string | null;
    contractStatus: string;
    status: RecordStatus;
    remark: string | null;
}
const contractTypeLabel = new Map(CONTRACT_TYPE_OPTIONS.map((o) => [o.value, o.label]));
const contractStatusLabel = new Map(CONTRACT_STATUS_OPTIONS.map((o) => [o.value, o.label]));
export const contractsChild: ChildTableDef<ContractRow, ContractInput> = {
    title: 'สัญญาแพทย์',
    description: 'สัญญาชนิดเดียวกันของแพทย์รายนี้ห้ามมีช่วงวันที่ทับกัน',
    api: {
        resource: 'doctor-contracts',
        list: (params, signal?: AbortSignal) => tenantApi<Paged<ContractRow>>('/api/master-data/doctor-contracts', { params, signal }),
        get: (id, signal?: AbortSignal) => tenantApi<ContractRow & { rowVersion: string; }>(`/api/master-data/doctor-contracts/${id}`, { signal }),
        create: (input) => tenantApi<ContractRow & { rowVersion: string; }>('/api/master-data/doctor-contracts', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => tenantApi<ContractRow & { rowVersion: string; }>(`/api/master-data/doctor-contracts/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => tenantApi<void>(`/api/master-data/doctor-contracts/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/doctor-contracts/${id}/history`, { signal }),
        exportXlsx: (params) => tenantApiBlob('/api/master-data/doctor-contracts/export', { params }),
    } satisfies CrudApi<ContractRow, ContractRow & { rowVersion: string; }, ContractInput>,
    parentKey: 'doctorCodeId',
    rowKey: (row) => row.id,
    addLabel: 'เพิ่มสัญญา',
    columns: [
        { key: 'contractNo', header: 'เลขที่สัญญา', width: '170px' },
        {
            key: 'contractType',
            header: 'ชนิดสัญญา',
            width: '230px',
            value: (row) => contractTypeLabel.get(row.contractType) ?? row.contractType,
        },
        { key: 'contractName', header: 'ชื่อสัญญา' },
        { key: 'startDate', header: 'เริ่ม', format: 'date', width: '130px' },
        { key: 'endDate', header: 'สิ้นสุด', format: 'date', width: '130px' },
        {
            key: 'guaranteeAmount',
            header: 'วงเงินประกันรายได้',
            align: 'right',
            format: 'amount',
            width: '190px',
        },
        {
            key: 'contractStatus',
            header: 'สถานะสัญญา',
            width: '160px',
            value: (row) => contractStatusLabel.get(row.contractStatus) ?? row.contractStatus,
        },
    ],
    fields: [
        { kind: 'text', name: 'contractNo', label: 'เลขที่สัญญา', required: true },
        {
            kind: 'select',
            name: 'contractType',
            label: 'ชนิดสัญญา',
            required: true,
            options: CONTRACT_TYPE_OPTIONS,
            width: 'lg',
        },
        { kind: 'text', name: 'contractName', label: 'ชื่อสัญญา', width: 'lg' },
        { kind: 'date', name: 'startDate', label: 'วันที่เริ่มสัญญา', required: true },
        { kind: 'date', name: 'endDate', label: 'วันที่สิ้นสุดสัญญา' },
        { kind: 'date', name: 'signedDate', label: 'วันที่ลงนาม' },
        {
            kind: 'bool',
            name: 'autoRenew',
            label: 'ต่อสัญญาอัตโนมัติ',
            trueLabel: 'ต่ออัตโนมัติ',
            falseLabel: 'ไม่ต่ออัตโนมัติ',
        },
        { kind: 'number', name: 'noticeDays', label: 'บอกกล่าวล่วงหน้า (วัน)', min: 0 },
        { kind: 'amount', name: 'guaranteeAmount', label: 'วงเงินประกันรายได้ (บาท)' },
        {
            kind: 'select',
            name: 'contractStatus',
            label: 'สถานะสัญญา',
            required: true,
            options: CONTRACT_STATUS_OPTIONS,
        },
        { kind: 'text', name: 'documentUrl', label: 'ไฟล์สัญญา', width: 'lg' },
        STATUS_FIELD,
        { kind: 'textarea', name: 'remark', label: 'หมายเหตุ', rows: 2 },
    ],
    schema: z
        .object({
        doctorId: z.string().min(1),
        doctorCodeId: z.string().nullable(),
        contractNo: z.string().trim().min(1, 'โปรดระบุเลขที่สัญญา'),
        contractType: z.string().min(1, 'โปรดเลือกชนิดสัญญา'),
        contractName: z.string().trim().nullable(),
        startDate: z.string().min(1, 'โปรดระบุวันที่เริ่มสัญญา'),
        endDate: z.string().nullable(),
        autoRenew: z.boolean(),
        noticeDays: z.number().int().min(0, 'จำนวนวันต้องไม่ติดลบ').nullable(),
        guaranteeAmount: z.number().min(0, 'วงเงินต้องไม่ติดลบ').nullable(),
        documentUrl: z.string().trim().nullable(),
        signedDate: z.string().nullable(),
        contractStatus: z.string().min(1),
        status: z.enum(['ACTIVE', 'INACTIVE']),
        remark: z.string().trim().nullable(),
    })
        .refine((v) => !v.endDate || v.endDate >= v.startDate, {
        path: ['endDate'],
        message: 'วันที่สิ้นสุดสัญญาต้องไม่ก่อนวันที่เริ่ม',
    }),
    defaultValues: {
        doctorId: '',
        doctorCodeId: null,
        contractNo: '',
        contractType: 'PRACTICE_SPACE',
        contractName: null,
        startDate: '',
        endDate: null,
        autoRenew: false,
        noticeDays: null,
        guaranteeAmount: null,
        documentUrl: null,
        signedDate: null,
        contractStatus: 'ACTIVE',
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (row) => {
        const full = row as unknown as ContractInput & {
            rowVersion: string;
        };
        return {
            doctorId: full.doctorId,
            doctorCodeId: full.doctorCodeId,
            contractNo: full.contractNo,
            contractType: full.contractType,
            contractName: full.contractName,
            startDate: full.startDate,
            endDate: full.endDate,
            autoRenew: full.autoRenew,
            noticeDays: full.noticeDays,
            guaranteeAmount: full.guaranteeAmount,
            documentUrl: full.documentUrl,
            signedDate: full.signedDate,
            contractStatus: full.contractStatus,
            status: full.status,
            remark: full.remark,
        };
    },
    defaultSort: '-startDate',
    emptyHint: 'สัญญาเช่าพื้นที่ สัญญาจ้าง หรือสัญญาประกันรายได้ของแพทย์รายนี้',
};
type ReadOnlyInput = Record<string, never>;
export interface ScheduleRow {
    id: number;
    doctorCodeId: string;
    clinicName: string | null;
    scheduleDate: string;
    startTime: string;
    endTime: string;
    roomNo: string | null;
    scheduleType: string;
    sourceSystem: string;
    syncedAt: string;
}
export const schedulesChild: ChildTableDef<ScheduleRow, ReadOnlyInput> = {
    title: 'ตารางออกตรวจ',
    description: 'ข้อมูลจากระบบ SSB หรือ iMED — แก้ไขที่ระบบต้นทาง',
    api: {
        resource: 'doctor-schedules',
        list: (params, signal?: AbortSignal) => tenantApi<Paged<ScheduleRow>>('/api/master-data/doctor-schedules', { params, signal }),
        get: (id, signal?: AbortSignal) => tenantApi<ScheduleRow & { rowVersion: string; }>(`/api/master-data/doctor-schedules/${id}`, { signal }),
        create: (input) => tenantApi<ScheduleRow & { rowVersion: string; }>('/api/master-data/doctor-schedules', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => tenantApi<ScheduleRow & { rowVersion: string; }>(`/api/master-data/doctor-schedules/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => tenantApi<void>(`/api/master-data/doctor-schedules/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/doctor-schedules/${id}/history`, { signal }),
        exportXlsx: (params) => tenantApiBlob('/api/master-data/doctor-schedules/export', { params }),
    } satisfies CrudApi<ScheduleRow, ScheduleRow & { rowVersion: string; }, ReadOnlyInput>,
    parentKey: 'doctorCodeId',
    rowKey: (row) => String(row.id),
    readOnly: true,
    columns: [
        { key: 'scheduleDate', header: 'วันที่', format: 'date', width: '140px' },
        { key: 'startTime', header: 'เริ่ม', width: '110px' },
        { key: 'endTime', header: 'สิ้นสุด', width: '110px' },
        { key: 'clinicName', header: 'คลินิก' },
        { key: 'roomNo', header: 'ห้อง', width: '110px' },
        { key: 'scheduleType', header: 'ประเภท', width: '130px' },
        { key: 'sourceSystem', header: 'ที่มา', width: '110px' },
    ],
    fields: [],
    schema: z.object({}),
    defaultValues: {},
    toInput: () => ({}),
    emptyHint: 'ยังไม่มีตารางออกตรวจที่ซิงก์เข้ามาสำหรับรหัสแพทย์นี้',
};
export interface ScheduleOffRow {
    id: number;
    doctorCodeId: string;
    offDateFrom: string;
    offDateTo: string;
    reason: string | null;
    sourceSystem: string;
    syncedAt: string;
}
export const scheduleOffsChild: ChildTableDef<ScheduleOffRow, ReadOnlyInput> = {
    title: 'ตารางงดตรวจ',
    description: 'ข้อมูลจากระบบ SSB หรือ iMED — แก้ไขที่ระบบต้นทาง',
    api: {
        resource: 'doctor-schedule-offs',
        list: (params, signal?: AbortSignal) => tenantApi<Paged<ScheduleOffRow>>('/api/master-data/doctor-schedule-offs', { params, signal }),
        get: (id, signal?: AbortSignal) => tenantApi<ScheduleOffRow & { rowVersion: string; }>(`/api/master-data/doctor-schedule-offs/${id}`, { signal }),
        create: (input) => tenantApi<ScheduleOffRow & { rowVersion: string; }>('/api/master-data/doctor-schedule-offs', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => tenantApi<ScheduleOffRow & { rowVersion: string; }>(`/api/master-data/doctor-schedule-offs/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => tenantApi<void>(`/api/master-data/doctor-schedule-offs/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/doctor-schedule-offs/${id}/history`, { signal }),
        exportXlsx: (params) => tenantApiBlob('/api/master-data/doctor-schedule-offs/export', { params }),
    } satisfies CrudApi<ScheduleOffRow, ScheduleOffRow & { rowVersion: string; }, ReadOnlyInput>,
    parentKey: 'doctorCodeId',
    rowKey: (row) => String(row.id),
    readOnly: true,
    columns: [
        { key: 'offDateFrom', header: 'งดตั้งแต่', format: 'date', width: '150px' },
        { key: 'offDateTo', header: 'ถึงวันที่', format: 'date', width: '150px' },
        { key: 'reason', header: 'เหตุผล' },
        { key: 'sourceSystem', header: 'ที่มา', width: '110px' },
    ],
    fields: [],
    schema: z.object({}),
    defaultValues: {},
    toInput: () => ({}),
    emptyHint: 'ยังไม่มีการงดตรวจที่ซิงก์เข้ามาสำหรับรหัสแพทย์นี้',
};
