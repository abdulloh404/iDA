import { z } from 'zod';
import { coreApi, coreApiBlob } from '../../api/client';
import type { CrudApi } from '../../api/crud';
import type { AuditEntry, Paged, RecordStatus } from '../../api/types';
import { STATUS_COLUMN, STATUS_FIELD } from '../master-data/descriptor';
import type { ChildTableDef } from '../master-data/descriptor';
import { ADDRESS_TYPE_OPTIONS, CONTACT_TYPE_OPTIONS, LICENSE_TYPE_OPTIONS, RELATION_GROUP_OPTIONS, } from './options';
const doctorIdField = z.string().min(1);
function doctorChild<TRow extends {
    id: string;
}, TInput>(api: CrudApi<TRow, TRow & {
    rowVersion: string;
}, TInput>, def: Omit<ChildTableDef<TRow, TInput>, 'api' | 'parentKey' | 'rowKey'>): ChildTableDef<TRow, TInput> {
    return {
        ...def,
        api,
        parentKey: 'doctorId',
        rowKey: (row) => row.id,
    };
}
interface ChildRow {
    id: string;
    doctorId: string;
    status: RecordStatus;
}
export interface LicenseRow extends ChildRow {
    licenseType: string;
    licenseNo: string;
    issuedPlace: string | null;
    issuedDate: string | null;
    expiryDate: string | null;
    isNoExpiry: boolean;
    detail: string | null;
    approvedDate: string | null;
}
export const licensesChild = doctorChild<LicenseRow, Omit<LicenseRow, 'id'>>({
    resource: 'doctor-licenses',
    list: (params, signal?: AbortSignal) => coreApi<Paged<LicenseRow>>('/api/master-data/doctor-licenses', { params, signal }),
    get: (id, signal?: AbortSignal) => coreApi<LicenseRow & { rowVersion: string; }>(`/api/master-data/doctor-licenses/${id}`, { signal }),
    create: (input) => coreApi<LicenseRow & { rowVersion: string; }>('/api/master-data/doctor-licenses', { method: 'POST', body: input }),
    update: (id, input, rowVersion) => coreApi<LicenseRow & { rowVersion: string; }>(`/api/master-data/doctor-licenses/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
    remove: (id) => coreApi<void>(`/api/master-data/doctor-licenses/${id}`, { method: 'DELETE' }),
    history: (id, signal?: AbortSignal) => coreApi<AuditEntry[]>(`/api/master-data/doctor-licenses/${id}/history`, { signal }),
    exportXlsx: (params) => coreApiBlob('/api/master-data/doctor-licenses/export', { params }),
}, {
    title: 'ใบประกอบวิชาชีพและใบอนุญาต',
    description: 'ใบอนุญาตติดตัวแพทย์ ใช้ร่วมกันทุกโรงพยาบาลในเครือ',
    addLabel: 'เพิ่มใบอนุญาต',
    columns: [
        {
            key: 'licenseType',
            header: 'ชนิด',
            width: '220px',
            value: (row) => label(LICENSE_TYPE_OPTIONS, row.licenseType),
        },
        { key: 'licenseNo', header: 'เลขที่', width: '160px' },
        { key: 'issuedPlace', header: 'ออก ณ สถานที่' },
        { key: 'issuedDate', header: 'วันที่ออก', format: 'date', width: '130px' },
        {
            key: 'expiryDate',
            header: 'วันหมดอายุ',
            format: 'date',
            width: '140px',
            value: (row) => (row.isNoExpiry ? 'ไม่มีวันหมดอายุ' : row.expiryDate),
        },
        STATUS_COLUMN,
    ],
    fields: [
        {
            kind: 'select',
            name: 'licenseType',
            label: 'ชนิดใบอนุญาต',
            required: true,
            options: LICENSE_TYPE_OPTIONS,
            width: 'lg',
        },
        { kind: 'text', name: 'licenseNo', label: 'เลขที่', required: true },
        { kind: 'text', name: 'issuedPlace', label: 'ออก ณ สถานที่' },
        { kind: 'date', name: 'issuedDate', label: 'วันที่ออกเอกสาร' },
        { kind: 'date', name: 'expiryDate', label: 'วันหมดอายุ' },
        {
            kind: 'bool',
            name: 'isNoExpiry',
            label: 'วันหมดอายุ',
            trueLabel: 'ไม่มีวันหมดอายุ',
            falseLabel: 'มีวันหมดอายุ',
        },
        { kind: 'date', name: 'approvedDate', label: 'วันอนุมัติ' },
        { kind: 'textarea', name: 'detail', label: 'รายละเอียด', rows: 2 },
        STATUS_FIELD,
    ],
    schema: z
        .object({
        doctorId: doctorIdField,
        licenseType: z.string().min(1, 'โปรดเลือกชนิดใบอนุญาต'),
        licenseNo: z.string().trim().min(1, 'โปรดระบุเลขที่ใบอนุญาต'),
        issuedPlace: z.string().trim().nullable(),
        issuedDate: z.string().nullable(),
        expiryDate: z.string().nullable(),
        isNoExpiry: z.boolean(),
        detail: z.string().trim().nullable(),
        approvedDate: z.string().nullable(),
        status: z.enum(['ACTIVE', 'INACTIVE']),
    })
        .refine((v) => v.isNoExpiry || !!v.expiryDate, {
        path: ['expiryDate'],
        message: 'โปรดระบุวันหมดอายุ หรือเลือกไม่มีวันหมดอายุ',
    }),
    defaultValues: {
        doctorId: '',
        licenseType: 'MEDICAL',
        licenseNo: '',
        issuedPlace: null,
        issuedDate: null,
        expiryDate: null,
        isNoExpiry: false,
        detail: null,
        approvedDate: null,
        status: 'ACTIVE',
    },
    toInput: (row) => ({
        doctorId: row.doctorId,
        licenseType: row.licenseType,
        licenseNo: row.licenseNo,
        issuedPlace: row.issuedPlace,
        issuedDate: row.issuedDate,
        expiryDate: row.expiryDate,
        isNoExpiry: row.isNoExpiry,
        detail: row.detail,
        approvedDate: row.approvedDate,
        status: row.status,
    }),
    defaultSort: 'licenseNo',
    emptyHint: 'ใบประกอบวิชาชีพเป็นข้อมูลบังคับก่อนส่งประวัติแพทย์ขออนุมัติ',
});
function label(options: readonly {
    value: string;
    label: string;
}[], value: string): string {
    return options.find((o) => o.value === value)?.label ?? value;
}
export interface ContactRow extends ChildRow {
    contactType: string;
    contactValue: string;
    isPrimary: boolean;
    isVerified: boolean;
}
export const contactsChild = doctorChild<ContactRow, Omit<ContactRow, 'id' | 'isVerified'>>({
    resource: 'doctor-contacts',
    list: (params, signal?: AbortSignal) => coreApi<Paged<ContactRow>>('/api/master-data/doctor-contacts', { params, signal }),
    get: (id, signal?: AbortSignal) => coreApi<ContactRow & { rowVersion: string; }>(`/api/master-data/doctor-contacts/${id}`, { signal }),
    create: (input) => coreApi<ContactRow & { rowVersion: string; }>('/api/master-data/doctor-contacts', { method: 'POST', body: input }),
    update: (id, input, rowVersion) => coreApi<ContactRow & { rowVersion: string; }>(`/api/master-data/doctor-contacts/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
    remove: (id) => coreApi<void>(`/api/master-data/doctor-contacts/${id}`, { method: 'DELETE' }),
    history: (id, signal?: AbortSignal) => coreApi<AuditEntry[]>(`/api/master-data/doctor-contacts/${id}/history`, { signal }),
    exportXlsx: (params) => coreApiBlob('/api/master-data/doctor-contacts/export', { params }),
}, {
    title: 'ช่องทางติดต่อ',
    description: 'เบอร์โทรและอีเมลห้ามซ้ำกับแพทย์รายอื่นในระบบ',
    addLabel: 'เพิ่มช่องทางติดต่อ',
    columns: [
        {
            key: 'contactType',
            header: 'ชนิด',
            width: '180px',
            value: (row) => label(CONTACT_TYPE_OPTIONS, row.contactType),
        },
        { key: 'contactValue', header: 'ข้อมูลติดต่อ' },
        {
            key: 'isPrimary',
            header: 'ช่องทางหลัก',
            width: '140px',
            value: (row) => (row.isPrimary ? 'ใช่' : 'ไม่ใช่'),
        },
        STATUS_COLUMN,
    ],
    fields: [
        {
            kind: 'select',
            name: 'contactType',
            label: 'ชนิดช่องทาง',
            required: true,
            options: CONTACT_TYPE_OPTIONS,
        },
        { kind: 'text', name: 'contactValue', label: 'ข้อมูลติดต่อ', required: true, width: 'lg' },
        {
            kind: 'bool',
            name: 'isPrimary',
            label: 'ช่องทางหลัก',
            trueLabel: 'ใช่',
            falseLabel: 'ไม่ใช่',
        },
        STATUS_FIELD,
    ],
    schema: z.object({
        doctorId: doctorIdField,
        contactType: z.string().min(1, 'โปรดเลือกชนิดช่องทาง'),
        contactValue: z.string().trim().min(1, 'โปรดระบุข้อมูลติดต่อ'),
        isPrimary: z.boolean(),
        status: z.enum(['ACTIVE', 'INACTIVE']),
    }),
    defaultValues: {
        doctorId: '',
        contactType: 'MOBILE',
        contactValue: '',
        isPrimary: false,
        status: 'ACTIVE',
    },
    toInput: (row) => ({
        doctorId: row.doctorId,
        contactType: row.contactType,
        contactValue: row.contactValue,
        isPrimary: row.isPrimary,
        status: row.status,
    }),
    defaultSort: 'contactType',
    emptyHint: 'อย่างน้อยหนึ่งช่องทางที่ติดต่อแพทย์ได้จริง',
});
export interface AffiliationRow extends ChildRow {
    affiliationName: string;
    positionName: string | null;
    isPrimary: boolean;
}
export const affiliationsChild = doctorChild<AffiliationRow, Omit<AffiliationRow, 'id'>>({
    resource: 'doctor-affiliations',
    list: (params, signal?: AbortSignal) => coreApi<Paged<AffiliationRow>>('/api/master-data/doctor-affiliations', { params, signal }),
    get: (id, signal?: AbortSignal) => coreApi<AffiliationRow & { rowVersion: string; }>(`/api/master-data/doctor-affiliations/${id}`, { signal }),
    create: (input) => coreApi<AffiliationRow & { rowVersion: string; }>('/api/master-data/doctor-affiliations', { method: 'POST', body: input }),
    update: (id, input, rowVersion) => coreApi<AffiliationRow & { rowVersion: string; }>(`/api/master-data/doctor-affiliations/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
    remove: (id) => coreApi<void>(`/api/master-data/doctor-affiliations/${id}`, { method: 'DELETE' }),
    history: (id, signal?: AbortSignal) => coreApi<AuditEntry[]>(`/api/master-data/doctor-affiliations/${id}/history`, { signal }),
    exportXlsx: (params) => coreApiBlob('/api/master-data/doctor-affiliations/export', { params }),
}, {
    title: 'ต้นสังกัดและตำแหน่ง',
    addLabel: 'เพิ่มต้นสังกัด',
    columns: [
        { key: 'affiliationName', header: 'ต้นสังกัด' },
        { key: 'positionName', header: 'ตำแหน่งที่ต้นสังกัด', width: '240px' },
        {
            key: 'isPrimary',
            header: 'ต้นสังกัดหลัก',
            width: '140px',
            value: (row) => (row.isPrimary ? 'ใช่' : 'ไม่ใช่'),
        },
        STATUS_COLUMN,
    ],
    fields: [
        { kind: 'text', name: 'affiliationName', label: 'ต้นสังกัด', required: true, width: 'lg' },
        { kind: 'text', name: 'positionName', label: 'ตำแหน่งที่ต้นสังกัด' },
        {
            kind: 'bool',
            name: 'isPrimary',
            label: 'ต้นสังกัดหลัก',
            trueLabel: 'ใช่',
            falseLabel: 'ไม่ใช่',
        },
        STATUS_FIELD,
    ],
    schema: z.object({
        doctorId: doctorIdField,
        affiliationName: z.string().trim().min(1, 'โปรดระบุต้นสังกัด'),
        positionName: z.string().trim().nullable(),
        isPrimary: z.boolean(),
        status: z.enum(['ACTIVE', 'INACTIVE']),
    }),
    defaultValues: {
        doctorId: '',
        affiliationName: '',
        positionName: null,
        isPrimary: false,
        status: 'ACTIVE',
    },
    toInput: (row) => ({
        doctorId: row.doctorId,
        affiliationName: row.affiliationName,
        positionName: row.positionName,
        isPrimary: row.isPrimary,
        status: row.status,
    }),
    defaultSort: 'affiliationName',
    emptyHint: 'ต้นสังกัดของแพทย์ เช่น โรงเรียนแพทย์หรือหน่วยงานต้นสังกัดเดิม',
});
export interface AddressRow extends ChildRow {
    addressType: string;
    addrNo: string | null;
    building: string | null;
    soi: string | null;
    road: string | null;
    subdistrict: string | null;
    district: string | null;
    province: string | null;
    postcode: string | null;
    country: string | null;
    sameAsHome: boolean;
}
export const addressesChild = doctorChild<AddressRow, Omit<AddressRow, 'id'>>({
    resource: 'doctor-addresses',
    list: (params, signal?: AbortSignal) => coreApi<Paged<AddressRow>>('/api/master-data/doctor-addresses', { params, signal }),
    get: (id, signal?: AbortSignal) => coreApi<AddressRow & { rowVersion: string; }>(`/api/master-data/doctor-addresses/${id}`, { signal }),
    create: (input) => coreApi<AddressRow & { rowVersion: string; }>('/api/master-data/doctor-addresses', { method: 'POST', body: input }),
    update: (id, input, rowVersion) => coreApi<AddressRow & { rowVersion: string; }>(`/api/master-data/doctor-addresses/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
    remove: (id) => coreApi<void>(`/api/master-data/doctor-addresses/${id}`, { method: 'DELETE' }),
    history: (id, signal?: AbortSignal) => coreApi<AuditEntry[]>(`/api/master-data/doctor-addresses/${id}/history`, { signal }),
    exportXlsx: (params) => coreApiBlob('/api/master-data/doctor-addresses/export', { params }),
}, {
    title: 'ที่อยู่',
    description: 'ที่อยู่สำหรับยื่นภาษีคือที่อยู่ที่พิมพ์ลงหนังสือรับรองการหักภาษี ณ ที่จ่าย',
    addLabel: 'เพิ่มที่อยู่',
    columns: [
        {
            key: 'addressType',
            header: 'ชนิดที่อยู่',
            width: '200px',
            value: (row) => label(ADDRESS_TYPE_OPTIONS, row.addressType),
        },
        { key: 'addrNo', header: 'เลขที่', width: '110px' },
        { key: 'road', header: 'ถนน' },
        { key: 'district', header: 'อำเภอ/เขต', width: '160px' },
        { key: 'province', header: 'จังหวัด', width: '160px' },
        { key: 'postcode', header: 'รหัสไปรษณีย์', width: '140px' },
        STATUS_COLUMN,
    ],
    fields: [
        {
            kind: 'select',
            name: 'addressType',
            label: 'ชนิดที่อยู่',
            required: true,
            options: ADDRESS_TYPE_OPTIONS,
            width: 'lg',
        },
        {
            kind: 'bool',
            name: 'sameAsHome',
            label: 'ใช้ที่อยู่เดียวกับที่อยู่บ้าน',
            trueLabel: 'ใช่',
            falseLabel: 'ไม่ใช่',
        },
        { kind: 'text', name: 'addrNo', label: 'เลขที่บ้าน', width: 'sm' },
        { kind: 'text', name: 'building', label: 'อาคาร/หมู่บ้าน' },
        { kind: 'text', name: 'soi', label: 'ซอย/ตรอก', width: 'sm' },
        { kind: 'text', name: 'road', label: 'ถนน', width: 'sm' },
        { kind: 'text', name: 'subdistrict', label: 'ตำบล/แขวง', width: 'sm' },
        { kind: 'text', name: 'district', label: 'อำเภอ/เขต', width: 'sm' },
        { kind: 'text', name: 'province', label: 'จังหวัด', width: 'sm' },
        { kind: 'text', name: 'postcode', label: 'รหัสไปรษณีย์', width: 'sm' },
        { kind: 'text', name: 'country', label: 'ประเทศ', width: 'sm' },
        STATUS_FIELD,
    ],
    schema: z.object({
        doctorId: doctorIdField,
        addressType: z.string().min(1, 'โปรดเลือกชนิดที่อยู่'),
        addrNo: z.string().trim().nullable(),
        building: z.string().trim().nullable(),
        soi: z.string().trim().nullable(),
        road: z.string().trim().nullable(),
        subdistrict: z.string().trim().nullable(),
        district: z.string().trim().nullable(),
        province: z.string().trim().nullable(),
        postcode: z
            .string()
            .trim()
            .nullable()
            .refine((v) => !v || /^\d{5}$/.test(v), 'รหัสไปรษณีย์ต้องเป็นตัวเลข 5 หลัก'),
        country: z.string().trim().nullable(),
        sameAsHome: z.boolean(),
        status: z.enum(['ACTIVE', 'INACTIVE']),
    }),
    defaultValues: {
        doctorId: '',
        addressType: 'HOME',
        addrNo: null,
        building: null,
        soi: null,
        road: null,
        subdistrict: null,
        district: null,
        province: null,
        postcode: null,
        country: 'ไทย',
        sameAsHome: false,
        status: 'ACTIVE',
    },
    toInput: (row) => ({
        doctorId: row.doctorId,
        addressType: row.addressType,
        addrNo: row.addrNo,
        building: row.building,
        soi: row.soi,
        road: row.road,
        subdistrict: row.subdistrict,
        district: row.district,
        province: row.province,
        postcode: row.postcode,
        country: row.country,
        sameAsHome: row.sameAsHome,
        status: row.status,
    }),
    defaultSort: 'addressType',
    emptyHint: 'แพทย์หนึ่งคนมีที่อยู่ได้ชนิดละหนึ่งแห่ง',
});
export interface EducationRow extends ChildRow {
    startYear: number | null;
    endYear: number | null;
    degreeName: string;
    instituteName: string | null;
    country: string | null;
    remark: string | null;
}
export const educationsChild = doctorChild<EducationRow, Omit<EducationRow, 'id'>>({
    resource: 'doctor-educations',
    list: (params, signal?: AbortSignal) => coreApi<Paged<EducationRow>>('/api/master-data/doctor-educations', { params, signal }),
    get: (id, signal?: AbortSignal) => coreApi<EducationRow & { rowVersion: string; }>(`/api/master-data/doctor-educations/${id}`, { signal }),
    create: (input) => coreApi<EducationRow & { rowVersion: string; }>('/api/master-data/doctor-educations', { method: 'POST', body: input }),
    update: (id, input, rowVersion) => coreApi<EducationRow & { rowVersion: string; }>(`/api/master-data/doctor-educations/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
    remove: (id) => coreApi<void>(`/api/master-data/doctor-educations/${id}`, { method: 'DELETE' }),
    history: (id, signal?: AbortSignal) => coreApi<AuditEntry[]>(`/api/master-data/doctor-educations/${id}/history`, { signal }),
    exportXlsx: (params) => coreApiBlob('/api/master-data/doctor-educations/export', { params }),
}, {
    title: 'ประวัติการศึกษา',
    addLabel: 'เพิ่มประวัติการศึกษา',
    columns: [
        { key: 'startYear', header: 'ปีที่เริ่ม', align: 'right', width: '110px' },
        { key: 'endYear', header: 'ปีที่จบ', align: 'right', width: '110px' },
        { key: 'degreeName', header: 'วุฒิการศึกษา' },
        { key: 'instituteName', header: 'สถาบัน' },
        { key: 'country', header: 'ประเทศ', width: '140px' },
        STATUS_COLUMN,
    ],
    fields: [
        { kind: 'number', name: 'startYear', label: 'ปีที่เริ่ม (ค.ศ.)', min: 1900, max: 2100 },
        { kind: 'number', name: 'endYear', label: 'ปีที่จบ (ค.ศ.)', min: 1900, max: 2100 },
        { kind: 'text', name: 'degreeName', label: 'วุฒิการศึกษา', required: true, width: 'lg' },
        { kind: 'text', name: 'instituteName', label: 'สถาบัน', width: 'lg' },
        { kind: 'text', name: 'country', label: 'ประเทศ', width: 'sm' },
        { kind: 'textarea', name: 'remark', label: 'หมายเหตุ', rows: 2 },
        STATUS_FIELD,
    ],
    schema: z
        .object({
        doctorId: doctorIdField,
        startYear: z.number().int().min(1900).max(2100).nullable(),
        endYear: z.number().int().min(1900).max(2100).nullable(),
        degreeName: z.string().trim().min(1, 'โปรดระบุวุฒิการศึกษา'),
        instituteName: z.string().trim().nullable(),
        country: z.string().trim().nullable(),
        remark: z.string().trim().nullable(),
        status: z.enum(['ACTIVE', 'INACTIVE']),
    })
        .refine((v) => !v.startYear || !v.endYear || v.endYear >= v.startYear, {
        path: ['endYear'],
        message: 'ปีที่จบต้องไม่ก่อนปีที่เริ่ม',
    }),
    defaultValues: {
        doctorId: '',
        startYear: null,
        endYear: null,
        degreeName: '',
        instituteName: null,
        country: null,
        remark: null,
        status: 'ACTIVE',
    },
    toInput: (row) => ({
        doctorId: row.doctorId,
        startYear: row.startYear,
        endYear: row.endYear,
        degreeName: row.degreeName,
        instituteName: row.instituteName,
        country: row.country,
        remark: row.remark,
        status: row.status,
    }),
    defaultSort: '-endYear',
    emptyHint: 'ปีที่กรอกเป็น ค.ศ. เพื่อให้เรียงลำดับตรงกับข้อมูลส่วนอื่นของระบบ',
});
export interface WorkHistoryRow extends ChildRow {
    startYear: number | null;
    endYear: number | null;
    positionName: string | null;
    workplace: string | null;
    remark: string | null;
}
export const workHistoriesChild = doctorChild<WorkHistoryRow, Omit<WorkHistoryRow, 'id'>>({
    resource: 'doctor-work-histories',
    list: (params, signal?: AbortSignal) => coreApi<Paged<WorkHistoryRow>>('/api/master-data/doctor-work-histories', { params, signal }),
    get: (id, signal?: AbortSignal) => coreApi<WorkHistoryRow & { rowVersion: string; }>(`/api/master-data/doctor-work-histories/${id}`, { signal }),
    create: (input) => coreApi<WorkHistoryRow & { rowVersion: string; }>('/api/master-data/doctor-work-histories', { method: 'POST', body: input }),
    update: (id, input, rowVersion) => coreApi<WorkHistoryRow & { rowVersion: string; }>(`/api/master-data/doctor-work-histories/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
    remove: (id) => coreApi<void>(`/api/master-data/doctor-work-histories/${id}`, { method: 'DELETE' }),
    history: (id, signal?: AbortSignal) => coreApi<AuditEntry[]>(`/api/master-data/doctor-work-histories/${id}/history`, { signal }),
    exportXlsx: (params) => coreApiBlob('/api/master-data/doctor-work-histories/export', { params }),
}, {
    title: 'ประวัติการทำงาน',
    description: 'ประวัติการทำงานก่อนหรือนอกเครือโรงพยาบาล',
    addLabel: 'เพิ่มประวัติการทำงาน',
    columns: [
        { key: 'startYear', header: 'ปีที่เริ่ม', align: 'right', width: '110px' },
        { key: 'endYear', header: 'ปีที่สิ้นสุด', align: 'right', width: '120px' },
        { key: 'positionName', header: 'ตำแหน่ง', width: '220px' },
        { key: 'workplace', header: 'สถานที่ทำงาน' },
        STATUS_COLUMN,
    ],
    fields: [
        { kind: 'number', name: 'startYear', label: 'ปีที่เริ่ม (ค.ศ.)', min: 1900, max: 2100 },
        { kind: 'number', name: 'endYear', label: 'ปีที่สิ้นสุด (ค.ศ.)', min: 1900, max: 2100 },
        { kind: 'text', name: 'positionName', label: 'ตำแหน่ง' },
        { kind: 'text', name: 'workplace', label: 'สถานที่ทำงาน', required: true, width: 'lg' },
        { kind: 'textarea', name: 'remark', label: 'หมายเหตุ', rows: 2 },
        STATUS_FIELD,
    ],
    schema: z
        .object({
        doctorId: doctorIdField,
        startYear: z.number().int().min(1900).max(2100).nullable(),
        endYear: z.number().int().min(1900).max(2100).nullable(),
        positionName: z.string().trim().nullable(),
        workplace: z.string().trim().min(1, 'โปรดระบุสถานที่ทำงาน'),
        remark: z.string().trim().nullable(),
        status: z.enum(['ACTIVE', 'INACTIVE']),
    })
        .refine((v) => !v.startYear || !v.endYear || v.endYear >= v.startYear, {
        path: ['endYear'],
        message: 'ปีที่สิ้นสุดต้องไม่ก่อนปีที่เริ่ม',
    }),
    defaultValues: {
        doctorId: '',
        startYear: null,
        endYear: null,
        positionName: null,
        workplace: '',
        remark: null,
        status: 'ACTIVE',
    },
    toInput: (row) => ({
        doctorId: row.doctorId,
        startYear: row.startYear,
        endYear: row.endYear,
        positionName: row.positionName,
        workplace: row.workplace,
        remark: row.remark,
        status: row.status,
    }),
    defaultSort: '-endYear',
    emptyHint: 'ใช้บันทึกที่ทำงานก่อนเข้าเครือ หรือที่ทำงานอื่นที่ยังทำอยู่',
});
export interface DoctorHospitalLinkRow extends ChildRow {
    hospitalId: string;
    hospitalName: string | null;
    isHome: boolean;
    effectiveFrom: string;
    effectiveTo: string | null;
}
export const hospitalLinksChild = doctorChild<DoctorHospitalLinkRow, Omit<DoctorHospitalLinkRow, 'id' | 'hospitalName'>>({
    resource: 'doctor-hospital-links',
    list: (params, signal?: AbortSignal) => coreApi<Paged<DoctorHospitalLinkRow>>('/api/master-data/doctor-hospital-links', { params, signal }),
    get: (id, signal?: AbortSignal) => coreApi<DoctorHospitalLinkRow & { rowVersion: string; }>(`/api/master-data/doctor-hospital-links/${id}`, { signal }),
    create: (input) => coreApi<DoctorHospitalLinkRow & { rowVersion: string; }>('/api/master-data/doctor-hospital-links', { method: 'POST', body: input }),
    update: (id, input, rowVersion) => coreApi<DoctorHospitalLinkRow & { rowVersion: string; }>(`/api/master-data/doctor-hospital-links/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
    remove: (id) => coreApi<void>(`/api/master-data/doctor-hospital-links/${id}`, { method: 'DELETE' }),
    history: (id, signal?: AbortSignal) => coreApi<AuditEntry[]>(`/api/master-data/doctor-hospital-links/${id}/history`, { signal }),
    exportXlsx: (params) => coreApiBlob('/api/master-data/doctor-hospital-links/export', { params }),
}, {
    title: 'โรงพยาบาลที่แพทย์มีข้อมูล',
    description: 'บอกว่าแพทย์รายนี้มีข้อมูลอยู่ที่สาขาใดบ้าง และสาขาไหนเป็นต้นสังกัด',
    addLabel: 'เพิ่มโรงพยาบาล',
    columns: [
        { key: 'hospitalId', header: 'รหัสโรงพยาบาล', width: '170px' },
        { key: 'hospitalName', header: 'โรงพยาบาล' },
        {
            key: 'isHome',
            header: 'ต้นสังกัด',
            width: '130px',
            value: (row) => (row.isHome ? 'ใช่' : 'ไม่ใช่'),
        },
        { key: 'effectiveFrom', header: 'ตั้งแต่', format: 'date', width: '140px' },
        {
            key: 'effectiveTo',
            header: 'ถึง',
            format: 'date',
            width: '140px',
            value: (row) => row.effectiveTo ?? 'ไม่กำหนด',
        },
        STATUS_COLUMN,
    ],
    fields: [
        {
            kind: 'lookup',
            name: 'hospitalId',
            label: 'โรงพยาบาล',
            resource: 'hospitals',
            required: true,
            width: 'lg',
        },
        {
            kind: 'bool',
            name: 'isHome',
            label: 'โรงพยาบาลต้นสังกัด',
            trueLabel: 'ใช่',
            falseLabel: 'ไม่ใช่',
        },
        { kind: 'date', name: 'effectiveFrom', label: 'มีข้อมูลตั้งแต่', required: true },
        { kind: 'date', name: 'effectiveTo', label: 'ถึงวันที่' },
        STATUS_FIELD,
    ],
    schema: z
        .object({
        doctorId: doctorIdField,
        hospitalId: z.string().min(1, 'โปรดเลือกโรงพยาบาล'),
        isHome: z.boolean(),
        effectiveFrom: z.string().min(1, 'โปรดระบุวันที่เริ่มมีข้อมูล'),
        effectiveTo: z.string().nullable(),
        status: z.enum(['ACTIVE', 'INACTIVE']),
    })
        .refine((v) => !v.effectiveTo || v.effectiveTo >= v.effectiveFrom, {
        path: ['effectiveTo'],
        message: 'วันที่สิ้นสุดต้องไม่ก่อนวันที่เริ่ม',
    }),
    defaultValues: {
        doctorId: '',
        hospitalId: '',
        isHome: false,
        effectiveFrom: '',
        effectiveTo: null,
        status: 'ACTIVE',
    },
    toInput: (row) => ({
        doctorId: row.doctorId,
        hospitalId: row.hospitalId,
        isHome: row.isHome,
        effectiveFrom: row.effectiveFrom,
        effectiveTo: row.effectiveTo,
        status: row.status,
    }),
    defaultSort: 'hospitalId',
    emptyHint: 'แพทย์มีต้นสังกัดได้แห่งเดียว แต่มีข้อมูลอยู่ได้หลายสาขา',
});
export interface FamilyRow extends ChildRow {
    fullNameTh: string;
    fullNameEn: string | null;
    relationGroup: string;
    relationName: string | null;
    nationalIdLast4: string | null;
    hn: string | null;
    isWelfareEligible: boolean;
}
export const familiesChild = doctorChild<FamilyRow, Omit<FamilyRow, 'id' | 'nationalIdLast4'> & {
    nationalId: string | null;
}>({
    resource: 'doctor-families',
    list: (params, signal?: AbortSignal) => coreApi<Paged<FamilyRow>>('/api/master-data/doctor-families', { params, signal }),
    get: (id, signal?: AbortSignal) => coreApi<FamilyRow & { rowVersion: string; }>(`/api/master-data/doctor-families/${id}`, { signal }),
    create: (input) => coreApi<FamilyRow & { rowVersion: string; }>('/api/master-data/doctor-families', { method: 'POST', body: input }),
    update: (id, input, rowVersion) => coreApi<FamilyRow & { rowVersion: string; }>(`/api/master-data/doctor-families/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
    remove: (id) => coreApi<void>(`/api/master-data/doctor-families/${id}`, { method: 'DELETE' }),
    history: (id, signal?: AbortSignal) => coreApi<AuditEntry[]>(`/api/master-data/doctor-families/${id}/history`, { signal }),
    exportXlsx: (params) => coreApiBlob('/api/master-data/doctor-families/export', { params }),
}, {
    title: 'ครอบครัวและบุคคลอ้างอิง',
    description: 'ผู้ที่มีสิทธิ์ใช้สวัสดิการของแพทย์ต้องระบุความสัมพันธ์',
    addLabel: 'เพิ่มบุคคล',
    columns: [
        { key: 'fullNameTh', header: 'ชื่อ-นามสกุล' },
        {
            key: 'relationGroup',
            header: 'กลุ่ม',
            width: '150px',
            value: (row) => label(RELATION_GROUP_OPTIONS, row.relationGroup),
        },
        { key: 'relationName', header: 'ความสัมพันธ์', width: '160px' },
        { key: 'nationalIdLast4', header: 'เลขบัตร (4 ตัวท้าย)', width: '170px' },
        { key: 'hn', header: 'H.N.', width: '130px' },
        {
            key: 'isWelfareEligible',
            header: 'สิทธิ์สวัสดิการ',
            width: '150px',
            value: (row) => (row.isWelfareEligible ? 'มีสิทธิ์' : 'ไม่มีสิทธิ์'),
        },
        STATUS_COLUMN,
    ],
    fields: [
        { kind: 'text', name: 'fullNameTh', label: 'ชื่อ-นามสกุล (ภาษาไทย)', required: true },
        { kind: 'text', name: 'fullNameEn', label: 'ชื่อ-นามสกุล (ภาษาอังกฤษ)' },
        {
            kind: 'select',
            name: 'relationGroup',
            label: 'กลุ่มความสัมพันธ์',
            required: true,
            options: RELATION_GROUP_OPTIONS,
        },
        { kind: 'text', name: 'relationName', label: 'ความสัมพันธ์', width: 'sm' },
        {
            kind: 'text',
            name: 'nationalId',
            label: 'เลขบัตรประชาชน',
            maxLength: 13,
            hint: 'เก็บแบบเข้ารหัส เว้นว่างไว้คือไม่แก้ของเดิม',
        },
        { kind: 'text', name: 'hn', label: 'H.N.', width: 'sm' },
        {
            kind: 'bool',
            name: 'isWelfareEligible',
            label: 'สิทธิ์สวัสดิการ',
            trueLabel: 'มีสิทธิ์',
            falseLabel: 'ไม่มีสิทธิ์',
        },
        STATUS_FIELD,
    ],
    schema: z
        .object({
        doctorId: doctorIdField,
        fullNameTh: z.string().trim().min(1, 'โปรดระบุชื่อ-นามสกุล'),
        fullNameEn: z.string().trim().nullable(),
        relationGroup: z.string().min(1, 'โปรดเลือกกลุ่มความสัมพันธ์'),
        relationName: z.string().trim().nullable(),
        nationalId: z
            .string()
            .trim()
            .nullable()
            .refine((v) => !v || /^\d{13}$/.test(v), 'เลขบัตรประชาชนต้องเป็นตัวเลข 13 หลัก'),
        hn: z.string().trim().nullable(),
        isWelfareEligible: z.boolean(),
        status: z.enum(['ACTIVE', 'INACTIVE']),
    })
        .refine((v) => !v.isWelfareEligible || !!v.relationName, {
        path: ['relationName'],
        message: 'ผู้มีสิทธิ์สวัสดิการต้องระบุความสัมพันธ์',
    }),
    defaultValues: {
        doctorId: '',
        fullNameTh: '',
        fullNameEn: null,
        relationGroup: 'FAMILY',
        relationName: null,
        nationalId: null,
        hn: null,
        isWelfareEligible: false,
        status: 'ACTIVE',
    },
    toInput: (row) => ({
        doctorId: row.doctorId,
        fullNameTh: row.fullNameTh,
        fullNameEn: row.fullNameEn,
        relationGroup: row.relationGroup,
        relationName: row.relationName,
        nationalId: null,
        hn: row.hn,
        isWelfareEligible: row.isWelfareEligible,
        status: row.status,
    }),
    defaultSort: 'fullNameTh',
    emptyHint: 'ใช้ผูกสิทธิ์สวัสดิการครอบครัวแพทย์และเป็นบุคคลอ้างอิง',
});
export interface TrainingRow extends ChildRow {
    trainingName: string;
    instituteName: string | null;
    budgetSource: string | null;
    bondContractNo: string | null;
    startDate: string | null;
    endDate: string | null;
}
export const trainingsChild = doctorChild<TrainingRow, Omit<TrainingRow, 'id'>>({
    resource: 'doctor-trainings',
    list: (params, signal?: AbortSignal) => coreApi<Paged<TrainingRow>>('/api/master-data/doctor-trainings', { params, signal }),
    get: (id, signal?: AbortSignal) => coreApi<TrainingRow & { rowVersion: string; }>(`/api/master-data/doctor-trainings/${id}`, { signal }),
    create: (input) => coreApi<TrainingRow & { rowVersion: string; }>('/api/master-data/doctor-trainings', { method: 'POST', body: input }),
    update: (id, input, rowVersion) => coreApi<TrainingRow & { rowVersion: string; }>(`/api/master-data/doctor-trainings/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
    remove: (id) => coreApi<void>(`/api/master-data/doctor-trainings/${id}`, { method: 'DELETE' }),
    history: (id, signal?: AbortSignal) => coreApi<AuditEntry[]>(`/api/master-data/doctor-trainings/${id}/history`, { signal }),
    exportXlsx: (params) => coreApiBlob('/api/master-data/doctor-trainings/export', { params }),
}, {
    title: 'ประวัติการฝึกอบรมและสัญญาใช้ทุน',
    addLabel: 'เพิ่มการฝึกอบรม',
    columns: [
        { key: 'trainingName', header: 'หลักสูตร' },
        { key: 'instituteName', header: 'สถาบัน', width: '220px' },
        { key: 'budgetSource', header: 'แหล่งทุน', width: '180px' },
        { key: 'bondContractNo', header: 'เลขที่สัญญาใช้ทุน', width: '180px' },
        { key: 'startDate', header: 'เริ่ม', format: 'date', width: '130px' },
        { key: 'endDate', header: 'สิ้นสุด', format: 'date', width: '130px' },
        STATUS_COLUMN,
    ],
    fields: [
        { kind: 'text', name: 'trainingName', label: 'หลักสูตรที่อบรม', required: true, width: 'lg' },
        { kind: 'text', name: 'instituteName', label: 'สถาบัน', width: 'lg' },
        { kind: 'text', name: 'budgetSource', label: 'แหล่งทุน' },
        { kind: 'text', name: 'bondContractNo', label: 'เลขที่สัญญาใช้ทุน' },
        { kind: 'date', name: 'startDate', label: 'วันที่เริ่ม' },
        { kind: 'date', name: 'endDate', label: 'วันที่สิ้นสุด' },
        STATUS_FIELD,
    ],
    schema: z
        .object({
        doctorId: doctorIdField,
        trainingName: z.string().trim().min(1, 'โปรดระบุหลักสูตรที่อบรม'),
        instituteName: z.string().trim().nullable(),
        budgetSource: z.string().trim().nullable(),
        bondContractNo: z.string().trim().nullable(),
        startDate: z.string().nullable(),
        endDate: z.string().nullable(),
        status: z.enum(['ACTIVE', 'INACTIVE']),
    })
        .refine((v) => !v.startDate || !v.endDate || v.endDate >= v.startDate, {
        path: ['endDate'],
        message: 'วันที่สิ้นสุดต้องไม่ก่อนวันที่เริ่ม',
    }),
    defaultValues: {
        doctorId: '',
        trainingName: '',
        instituteName: null,
        budgetSource: null,
        bondContractNo: null,
        startDate: null,
        endDate: null,
        status: 'ACTIVE',
    },
    toInput: (row) => ({
        doctorId: row.doctorId,
        trainingName: row.trainingName,
        instituteName: row.instituteName,
        budgetSource: row.budgetSource,
        bondContractNo: row.bondContractNo,
        startDate: row.startDate,
        endDate: row.endDate,
        status: row.status,
    }),
    defaultSort: '-startDate',
    emptyHint: 'สัญญาใช้ทุนที่ยังไม่ครบกำหนดมีผลต่อเงื่อนไขการจ่ายค่าแพทย์',
});
export interface ProfessionalRecordRow extends ChildRow {
    recordNo: string | null;
    recordDate: string | null;
    hospitalName: string | null;
    subject: string | null;
    recordType: string | null;
    conclusion: string | null;
}
export const professionalRecordsChild = doctorChild<ProfessionalRecordRow, Omit<ProfessionalRecordRow, 'id'>>({
    resource: 'doctor-professional-records',
    list: (params, signal?: AbortSignal) => coreApi<Paged<ProfessionalRecordRow>>('/api/master-data/doctor-professional-records', { params, signal }),
    get: (id, signal?: AbortSignal) => coreApi<ProfessionalRecordRow & { rowVersion: string; }>(`/api/master-data/doctor-professional-records/${id}`, { signal }),
    create: (input) => coreApi<ProfessionalRecordRow & { rowVersion: string; }>('/api/master-data/doctor-professional-records', { method: 'POST', body: input }),
    update: (id, input, rowVersion) => coreApi<ProfessionalRecordRow & { rowVersion: string; }>(`/api/master-data/doctor-professional-records/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
    remove: (id) => coreApi<void>(`/api/master-data/doctor-professional-records/${id}`, { method: 'DELETE' }),
    history: (id, signal?: AbortSignal) => coreApi<AuditEntry[]>(`/api/master-data/doctor-professional-records/${id}/history`, { signal }),
    exportXlsx: (params) => coreApiBlob('/api/master-data/doctor-professional-records/export', { params }),
}, {
    title: 'ประวัติวิชาชีพ',
    addLabel: 'เพิ่มประวัติวิชาชีพ',
    columns: [
        { key: 'recordNo', header: 'เลขที่', width: '150px' },
        { key: 'recordDate', header: 'วันที่', format: 'date', width: '130px' },
        { key: 'hospitalName', header: 'โรงพยาบาล', width: '220px' },
        { key: 'subject', header: 'เรื่อง' },
        { key: 'recordType', header: 'ประเภท', width: '160px' },
        STATUS_COLUMN,
    ],
    fields: [
        { kind: 'text', name: 'recordNo', label: 'เลขที่บันทึก', width: 'sm' },
        { kind: 'date', name: 'recordDate', label: 'วันที่บันทึก' },
        { kind: 'text', name: 'hospitalName', label: 'โรงพยาบาล', width: 'lg' },
        { kind: 'text', name: 'subject', label: 'เรื่อง', required: true, width: 'lg' },
        { kind: 'text', name: 'recordType', label: 'ประเภท' },
        { kind: 'textarea', name: 'conclusion', label: 'ข้อสรุป', rows: 3 },
        STATUS_FIELD,
    ],
    schema: z.object({
        doctorId: doctorIdField,
        recordNo: z.string().trim().nullable(),
        recordDate: z.string().nullable(),
        hospitalName: z.string().trim().nullable(),
        subject: z.string().trim().min(1, 'โปรดระบุเรื่อง'),
        recordType: z.string().trim().nullable(),
        conclusion: z.string().trim().nullable(),
        status: z.enum(['ACTIVE', 'INACTIVE']),
    }),
    defaultValues: {
        doctorId: '',
        recordNo: null,
        recordDate: null,
        hospitalName: null,
        subject: '',
        recordType: null,
        conclusion: null,
        status: 'ACTIVE',
    },
    toInput: (row) => ({
        doctorId: row.doctorId,
        recordNo: row.recordNo,
        recordDate: row.recordDate,
        hospitalName: row.hospitalName,
        subject: row.subject,
        recordType: row.recordType,
        conclusion: row.conclusion,
        status: row.status,
    }),
    defaultSort: '-recordDate',
    emptyHint: 'บันทึกเหตุการณ์ทางวิชาชีพที่เกี่ยวข้องกับแพทย์รายนี้',
});
export interface InsuranceRow extends ChildRow {
    insurerName: string;
    policyNo: string | null;
    policyYear: number | null;
    coverageAmount: number | null;
    startDate: string | null;
    endDate: string | null;
    documentUrl: string | null;
}
export const insurancesChild = doctorChild<InsuranceRow, Omit<InsuranceRow, 'id'>>({
    resource: 'doctor-insurances',
    list: (params, signal?: AbortSignal) => coreApi<Paged<InsuranceRow>>('/api/master-data/doctor-insurances', { params, signal }),
    get: (id, signal?: AbortSignal) => coreApi<InsuranceRow & { rowVersion: string; }>(`/api/master-data/doctor-insurances/${id}`, { signal }),
    create: (input) => coreApi<InsuranceRow & { rowVersion: string; }>('/api/master-data/doctor-insurances', { method: 'POST', body: input }),
    update: (id, input, rowVersion) => coreApi<InsuranceRow & { rowVersion: string; }>(`/api/master-data/doctor-insurances/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
    remove: (id) => coreApi<void>(`/api/master-data/doctor-insurances/${id}`, { method: 'DELETE' }),
    history: (id, signal?: AbortSignal) => coreApi<AuditEntry[]>(`/api/master-data/doctor-insurances/${id}/history`, { signal }),
    exportXlsx: (params) => coreApiBlob('/api/master-data/doctor-insurances/export', { params }),
}, {
    title: 'ประกันความรับผิดทางวิชาชีพ',
    addLabel: 'เพิ่มกรมธรรม์',
    columns: [
        { key: 'policyYear', header: 'ปีกรมธรรม์', align: 'right', width: '130px' },
        { key: 'insurerName', header: 'บริษัทประกัน' },
        { key: 'policyNo', header: 'เลขที่กรมธรรม์', width: '180px' },
        {
            key: 'coverageAmount',
            header: 'วงเงินคุ้มครอง (บาท)',
            align: 'right',
            format: 'amount',
            width: '190px',
        },
        { key: 'endDate', header: 'คุ้มครองถึง', format: 'date', width: '140px' },
        STATUS_COLUMN,
    ],
    fields: [
        { kind: 'text', name: 'insurerName', label: 'บริษัทประกัน', required: true, width: 'lg' },
        { kind: 'text', name: 'policyNo', label: 'เลขที่กรมธรรม์' },
        { kind: 'number', name: 'policyYear', label: 'ปีกรมธรรม์ (ค.ศ.)', min: 1900, max: 2100 },
        { kind: 'amount', name: 'coverageAmount', label: 'วงเงินคุ้มครอง (บาท)' },
        { kind: 'date', name: 'startDate', label: 'คุ้มครองตั้งแต่' },
        { kind: 'date', name: 'endDate', label: 'คุ้มครองถึง' },
        { kind: 'text', name: 'documentUrl', label: 'ไฟล์กรมธรรม์', width: 'lg' },
        STATUS_FIELD,
    ],
    schema: z
        .object({
        doctorId: doctorIdField,
        insurerName: z.string().trim().min(1, 'โปรดระบุบริษัทประกัน'),
        policyNo: z.string().trim().nullable(),
        policyYear: z.number().int().min(1900).max(2100).nullable(),
        coverageAmount: z.number().min(0, 'วงเงินคุ้มครองต้องไม่ติดลบ').nullable(),
        startDate: z.string().nullable(),
        endDate: z.string().nullable(),
        documentUrl: z.string().trim().nullable(),
        status: z.enum(['ACTIVE', 'INACTIVE']),
    })
        .refine((v) => !v.startDate || !v.endDate || v.endDate >= v.startDate, {
        path: ['endDate'],
        message: 'วันที่สิ้นสุดความคุ้มครองต้องไม่ก่อนวันที่เริ่ม',
    }),
    defaultValues: {
        doctorId: '',
        insurerName: '',
        policyNo: null,
        policyYear: null,
        coverageAmount: null,
        startDate: null,
        endDate: null,
        documentUrl: null,
        status: 'ACTIVE',
    },
    toInput: (row) => ({
        doctorId: row.doctorId,
        insurerName: row.insurerName,
        policyNo: row.policyNo,
        policyYear: row.policyYear,
        coverageAmount: row.coverageAmount,
        startDate: row.startDate,
        endDate: row.endDate,
        documentUrl: row.documentUrl,
        status: row.status,
    }),
    defaultSort: '-policyYear',
    emptyHint: 'กรมธรรม์ที่หมดอายุแล้วมีผลต่อเงื่อนไขการทำหัตถการบางประเภท',
});
export interface DocumentRow extends ChildRow {
    docTypeName: string | null;
    documentName: string;
    hasExpiry: boolean;
    expiryDate: string | null;
    uploadedBy: string | null;
    uploadedAt: string;
}
export const documentsChild = doctorChild<DocumentRow, {
    doctorId: string;
    docTypeId: string;
    documentName: string;
    fileUrl: string;
    hasExpiry: boolean;
    expiryDate: string | null;
    status: RecordStatus;
}>({
    resource: 'doctor-documents',
    list: (params, signal?: AbortSignal) => coreApi<Paged<DocumentRow>>('/api/master-data/doctor-documents', { params, signal }),
    get: (id, signal?: AbortSignal) => coreApi<DocumentRow & { rowVersion: string; }>(`/api/master-data/doctor-documents/${id}`, { signal }),
    create: (input) => coreApi<DocumentRow & { rowVersion: string; }>('/api/master-data/doctor-documents', { method: 'POST', body: input }),
    update: (id, input, rowVersion) => coreApi<DocumentRow & { rowVersion: string; }>(`/api/master-data/doctor-documents/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
    remove: (id) => coreApi<void>(`/api/master-data/doctor-documents/${id}`, { method: 'DELETE' }),
    history: (id, signal?: AbortSignal) => coreApi<AuditEntry[]>(`/api/master-data/doctor-documents/${id}/history`, { signal }),
    exportXlsx: (params) => coreApiBlob('/api/master-data/doctor-documents/export', { params }),
}, {
    title: 'เอกสารแนบ',
    description: 'สถานะใกล้หมดอายุคำนวณจากวันหมดอายุตอนแสดงผล ไม่ได้เก็บเป็นคอลัมน์',
    addLabel: 'เพิ่มเอกสาร',
    columns: [
        { key: 'docTypeName', header: 'ประเภทเอกสาร', width: '220px' },
        { key: 'documentName', header: 'ชื่อเอกสาร' },
        {
            key: 'expiryDate',
            header: 'วันหมดอายุ',
            format: 'date',
            width: '150px',
            value: (row) => (row.hasExpiry ? row.expiryDate : 'ไม่มีวันหมดอายุ'),
        },
        { key: 'uploadedBy', header: 'ผู้อัปโหลด', width: '160px' },
        { key: 'uploadedAt', header: 'วันที่อัปโหลด', format: 'date', width: '150px' },
        STATUS_COLUMN,
    ],
    fields: [
        {
            kind: 'lookup',
            name: 'docTypeId',
            label: 'ประเภทเอกสาร',
            resource: 'document-types',
            required: true,
            width: 'lg',
        },
        { kind: 'text', name: 'documentName', label: 'ชื่อเอกสาร', required: true, width: 'lg' },
        { kind: 'text', name: 'fileUrl', label: 'ไฟล์เอกสาร', required: true, width: 'full' },
        {
            kind: 'bool',
            name: 'hasExpiry',
            label: 'วันหมดอายุ',
            trueLabel: 'มีวันหมดอายุ',
            falseLabel: 'ไม่มีวันหมดอายุ',
        },
        { kind: 'date', name: 'expiryDate', label: 'วันที่หมดอายุ' },
        STATUS_FIELD,
    ],
    schema: z
        .object({
        doctorId: doctorIdField,
        docTypeId: z.string().min(1, 'โปรดเลือกประเภทเอกสาร'),
        documentName: z.string().trim().min(1, 'โปรดระบุชื่อเอกสาร'),
        fileUrl: z.string().trim().min(1, 'โปรดระบุไฟล์เอกสาร'),
        hasExpiry: z.boolean(),
        expiryDate: z.string().nullable(),
        status: z.enum(['ACTIVE', 'INACTIVE']),
    })
        .refine((v) => !v.hasExpiry || !!v.expiryDate, {
        path: ['expiryDate'],
        message: 'เอกสารที่มีวันหมดอายุต้องระบุวันที่หมดอายุ',
    }),
    defaultValues: {
        doctorId: '',
        docTypeId: '',
        documentName: '',
        fileUrl: '',
        hasExpiry: false,
        expiryDate: null,
        status: 'ACTIVE',
    },
    toInput: (row) => ({
        doctorId: row.doctorId,
        docTypeId: (row as unknown as {
            docTypeId: string;
        }).docTypeId,
        documentName: row.documentName,
        fileUrl: (row as unknown as {
            fileUrl: string;
        }).fileUrl,
        hasExpiry: row.hasExpiry,
        expiryDate: row.expiryDate,
        status: row.status,
    }),
    defaultSort: '-uploadedAt',
    emptyHint: 'สำเนาบัตร ใบอนุญาต และเอกสารประกอบอื่นของแพทย์',
});
