import { z } from 'zod';
import { createCrudApi } from '../../api/crud';
import type { RecordStatus } from '../../api/types';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD, omitFields } from '../master-data/descriptor';
import type { ScreenDescriptor } from '../master-data/descriptor';
import { APPROVAL_STATUS_OPTIONS, GENDER_OPTIONS, ID_DOC_TYPE_OPTIONS, TAX_ENTITY_TYPE_OPTIONS, } from './options';
import { addressesChild, affiliationsChild, contactsChild, documentsChild, educationsChild, familiesChild, hospitalLinksChild, insurancesChild, licensesChild, professionalRecordsChild, trainingsChild, workHistoriesChild, } from './children';
export interface DoctorListItem {
    id: string;
    doctorGlobalCode: string;
    titleName: string | null;
    fullNameTh: string;
    fullNameEn: string | null;
    gender: string;
    nationalIdLast4: string | null;
    homeHospitalId: string | null;
    isCentralDoctor: boolean;
    approvalStatus: string;
    status: RecordStatus;
}
export interface DoctorDetail {
    id: string;
    doctorGlobalCode: string;
    epmsPersonId: string | null;
    titleId: string | null;
    firstNameTh: string;
    lastNameTh: string;
    firstNameEn: string | null;
    lastNameEn: string | null;
    gender: string;
    birthDate: string | null;
    nationality: string | null;
    idDocType: string;
    nationalIdLast4: string | null;
    idDocExpiryDate: string | null;
    idDocNoExpiry: boolean;
    taxId: string | null;
    taxEntityType: string;
    isCentralDoctor: boolean;
    homeHospitalId: string | null;
    photoUrl: string | null;
    spokenLanguages: string | null;
    treatForeignPatient: boolean;
    firstJoinDate: string | null;
    approvalStatus: string;
    status: RecordStatus;
    remark: string | null;
    sourceSystem: string;
    rowVersion: string;
}
export interface DoctorInput {
    doctorGlobalCode: string;
    titleId: string | null;
    firstNameTh: string;
    lastNameTh: string;
    firstNameEn: string | null;
    lastNameEn: string | null;
    gender: string;
    birthDate: string | null;
    nationality: string | null;
    idDocType: string;
    nationalId: string | null;
    passportNo: string | null;
    idDocExpiryDate: string | null;
    idDocNoExpiry: boolean;
    taxId: string | null;
    taxEntityType: string;
    isCentralDoctor: boolean;
    homeHospitalId: string | null;
    spokenLanguages: string | null;
    treatForeignPatient: boolean;
    firstJoinDate: string | null;
    status: RecordStatus;
    remark: string | null;
}
const thaiId = (label: string) => z
    .string()
    .trim()
    .nullable()
    .refine((v) => !v || /^\d{13}$/.test(v), `${label}ต้องเป็นตัวเลข 13 หลัก`);
const doctorSchema = z
    .object({
    doctorGlobalCode: z
        .string()
        .trim()
        .min(1, 'โปรดระบุรหัสแพทย์กลาง')
        .max(20, 'รหัสแพทย์กลางต้องไม่เกิน 20 ตัวอักษร'),
    titleId: z.string().nullable(),
    firstNameTh: z.string().trim().min(1, 'โปรดระบุชื่อ (ภาษาไทย)'),
    lastNameTh: z.string().trim().min(1, 'โปรดระบุนามสกุล (ภาษาไทย)'),
    firstNameEn: z.string().trim().nullable(),
    lastNameEn: z.string().trim().nullable(),
    gender: z.string().min(1, 'โปรดเลือกเพศ'),
    birthDate: z.string().nullable(),
    nationality: z.string().trim().nullable(),
    idDocType: z.string().min(1, 'โปรดเลือกชนิดเอกสารยืนยันตัวตน'),
    nationalId: thaiId('เลขบัตรประชาชน'),
    passportNo: z.string().trim().nullable(),
    idDocExpiryDate: z.string().nullable(),
    idDocNoExpiry: z.boolean(),
    taxId: thaiId('เลขประจำตัวผู้เสียภาษี'),
    taxEntityType: z.string().min(1),
    isCentralDoctor: z.boolean(),
    homeHospitalId: z.string().nullable(),
    spokenLanguages: z.string().trim().nullable(),
    treatForeignPatient: z.boolean(),
    firstJoinDate: z.string().nullable(),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().nullable(),
})
    .refine((v) => v.idDocNoExpiry || v.idDocType !== 'PASSPORT' || !!v.idDocExpiryDate, {
    path: ['idDocExpiryDate'],
    message: 'โปรดระบุวันหมดอายุ หรือเลือกไม่มีวันหมดอายุ',
});
const GENDER_LABELS = new Map(GENDER_OPTIONS.map((o) => [o.value, o.label]));
export const doctorScreen: ScreenDescriptor<DoctorListItem, DoctorDetail, DoctorInput> = {
    id: 'doctor',
    resource: 'doctors',
    path: '/doctors/profile',
    titleTh: 'ข้อมูลประวัติแพทย์',
    breadcrumb: [{ label: 'จัดการข้อมูลแพทย์' }],
    api: createCrudApi<DoctorListItem, DoctorDetail, DoctorInput>('doctors'),
    columns: [
        { key: 'doctorGlobalCode', header: 'รหัสแพทย์กลาง', sortable: true, width: '170px' },
        { key: 'titleName', header: 'คำนำหน้า', width: '120px' },
        { key: 'fullNameTh', header: 'ชื่อ-นามสกุล', sortable: true },
        { key: 'fullNameEn', header: 'ชื่อ-นามสกุล (อังกฤษ)' },
        {
            key: 'gender',
            header: 'เพศ',
            sortable: true,
            width: '110px',
            value: (row) => GENDER_LABELS.get(row.gender) ?? row.gender,
        },
        { key: 'nationalIdLast4', header: 'เลขบัตร (4 ตัวท้าย)', width: '170px' },
        { key: 'homeHospitalId', header: 'สังกัด', sortable: true, width: '130px' },
        {
            key: 'approvalStatus',
            header: 'สถานะการอนุมัติ',
            sortable: true,
            format: 'status',
            width: '170px',
        },
        STATUS_COLUMN,
    ],
    filters: [
        { kind: 'select', name: 'gender', label: 'เพศ', options: GENDER_OPTIONS },
        {
            kind: 'select',
            name: 'approvalStatus',
            label: 'สถานะการอนุมัติ',
            options: APPROVAL_STATUS_OPTIONS,
        },
        { kind: 'lookup', name: 'homeHospitalId', label: 'สังกัดโรงพยาบาล', resource: 'hospitals' },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหารหัสแพทย์กลางหรือชื่อ-นามสกุล',
    defaultSort: 'doctorGlobalCode',
    rowKey: (row) => row.id,
    emptyHint: 'ประวัติแพทย์เป็นข้อมูลกลางของเครือ แก้ที่นี่แล้วเปลี่ยนให้ทุกสาขาพร้อมกัน',
    sections: [
        {
            title: 'ประวัติส่วนตัว',
            fields: [
                {
                    kind: 'text',
                    name: 'doctorGlobalCode',
                    label: 'รหัสแพทย์กลาง',
                    required: true,
                    maxLength: 20,
                    immutableOnEdit: true,
                },
                {
                    kind: 'lookup',
                    name: 'titleId',
                    label: 'คำนำหน้าชื่อ',
                    resource: 'titles',
                    emptyLabel: 'ไม่ระบุ',
                },
                { kind: 'text', name: 'firstNameTh', label: 'ชื่อ (ภาษาไทย)', required: true },
                { kind: 'text', name: 'lastNameTh', label: 'นามสกุล (ภาษาไทย)', required: true },
                { kind: 'text', name: 'firstNameEn', label: 'ชื่อ (ภาษาอังกฤษ)' },
                { kind: 'text', name: 'lastNameEn', label: 'นามสกุล (ภาษาอังกฤษ)' },
                { kind: 'select', name: 'gender', label: 'เพศ', required: true, options: GENDER_OPTIONS },
                { kind: 'date', name: 'birthDate', label: 'วันเกิด' },
                { kind: 'text', name: 'nationality', label: 'สัญชาติ', width: 'sm' },
                {
                    kind: 'bool',
                    name: 'isCentralDoctor',
                    label: 'แพทย์กลาง',
                    trueLabel: 'ใช่',
                    falseLabel: 'ไม่ใช่',
                    hint: 'ใช้รหัสเดียวกันได้ทุกโรงพยาบาลในเครือ',
                },
                {
                    kind: 'lookup',
                    name: 'homeHospitalId',
                    label: 'สังกัดโรงพยาบาล',
                    resource: 'hospitals',
                    emptyLabel: 'ไม่ระบุ',
                },
                { kind: 'date', name: 'firstJoinDate', label: 'วันที่เริ่มร่วมงาน' },
                STATUS_FIELD,
            ],
        },
        {
            title: 'เอกสารยืนยันตัวตนและภาษี',
            description: 'เลขบัตรและหนังสือเดินทางเก็บแบบเข้ารหัส เว้นว่างไว้คือไม่แก้ของเดิม',
            fields: [
                {
                    kind: 'select',
                    name: 'idDocType',
                    label: 'ชนิดเอกสาร',
                    required: true,
                    options: ID_DOC_TYPE_OPTIONS,
                },
                {
                    kind: 'text',
                    name: 'nationalId',
                    label: 'เลขบัตรประชาชน',
                    maxLength: 13,
                    hint: 'อ่านกลับมาได้เฉพาะสี่ตัวท้าย',
                },
                { kind: 'text', name: 'passportNo', label: 'เลขที่หนังสือเดินทาง' },
                { kind: 'date', name: 'idDocExpiryDate', label: 'วันที่บัตรหมดอายุ' },
                {
                    kind: 'bool',
                    name: 'idDocNoExpiry',
                    label: 'วันหมดอายุ',
                    trueLabel: 'ไม่มีวันหมดอายุ',
                    falseLabel: 'มีวันหมดอายุ',
                },
                { kind: 'text', name: 'taxId', label: 'เลขประจำตัวผู้เสียภาษี', maxLength: 13 },
                {
                    kind: 'select',
                    name: 'taxEntityType',
                    label: 'เสียภาษีในนาม',
                    required: true,
                    options: TAX_ENTITY_TYPE_OPTIONS,
                },
            ],
        },
        {
            title: 'การให้บริการ',
            fields: [
                {
                    kind: 'text',
                    name: 'spokenLanguages',
                    label: 'ภาษาที่แพทย์พูดได้',
                    width: 'lg',
                    hint: 'คั่นแต่ละภาษาด้วยเครื่องหมายจุลภาค',
                },
                {
                    kind: 'bool',
                    name: 'treatForeignPatient',
                    label: 'ดูแลผู้ป่วยต่างประเทศ',
                    trueLabel: 'ดูแล',
                    falseLabel: 'ไม่ดูแล',
                },
                REMARK_FIELD,
            ],
        },
    ],
    schema: doctorSchema,
    defaultValues: {
        doctorGlobalCode: '',
        titleId: null,
        firstNameTh: '',
        lastNameTh: '',
        firstNameEn: null,
        lastNameEn: null,
        gender: 'U',
        birthDate: null,
        nationality: 'ไทย',
        idDocType: 'NATIONAL_ID',
        nationalId: null,
        passportNo: null,
        idDocExpiryDate: null,
        idDocNoExpiry: false,
        taxId: null,
        taxEntityType: 'INDIVIDUAL',
        isCentralDoctor: false,
        homeHospitalId: null,
        spokenLanguages: null,
        treatForeignPatient: false,
        firstJoinDate: null,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => ({
        ...omitFields(detail, 'id', 'rowVersion', 'nationalIdLast4', 'photoUrl', 'epmsPersonId', 'approvalStatus', 'sourceSystem'),
        nationalId: null,
        passportNo: null,
    }),
    childTables: [
        licensesChild,
        contactsChild,
        addressesChild,
        affiliationsChild,
        educationsChild,
        trainingsChild,
        workHistoriesChild,
        familiesChild,
        professionalRecordsChild,
        insurancesChild,
        documentsChild,
        hospitalLinksChild,
    ],
};
