import { z } from 'zod';
import { createCrudApi } from '../../api/crud';
import type { RecordStatus } from '../../api/types';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD, omitFields } from '../master-data/descriptor';
import type { ScreenDescriptor } from '../master-data/descriptor';
import { APPROVAL_STATUS_OPTIONS, BOARD_STATUS_OPTIONS, EMPLOYMENT_STATUS_OPTIONS, WHT_FORM_OPTIONS, } from './options';
import { bankAccountsChild, codeSpecialtiesChild, contractsChild, scheduleOffsChild, schedulesChild, } from './code-children';
export interface DoctorCodeListItem {
    id: string;
    doctorId: string;
    code: string;
    oldDoctorCode: string | null;
    displayNameTh: string;
    isDutyDoctor: boolean;
    departmentCode: string | null;
    doctorTypeCode: string | null;
    employmentStatus: string;
    approvalStatus: string;
    status: RecordStatus;
}
export interface DoctorCodeDetail extends DoctorCodeListItem {
    isCentralCode: boolean;
    displayNameEn: string | null;
    taxInvoiceNameTh: string | null;
    taxInvoiceNameEn: string | null;
    boardStatus: string | null;
    defaultDfDoctorCode: string | null;
    whtFormType: string | null;
    canCheckinAnywhere: boolean;
    arNoWaitPayment: boolean;
    calcToHospitalNoPay: boolean;
    doctorTypeId: string | null;
    doctorGroupId: string | null;
    departmentId: string | null;
    clinicId: string | null;
    privilegeTypeId: string | null;
    statusPrivilegeId: string | null;
    employeeCode: string | null;
    paymentTypeId: string | null;
    startWorkDate: string | null;
    resignDate: string | null;
    paymentCondition: string | null;
    hospitalAbsorbCcFee: boolean;
    ccFeePercent: number | null;
    taxId: string | null;
    useHomeTaxAddress: boolean;
    taxAddrNo: string | null;
    taxAddrBuilding: string | null;
    taxAddrSoi: string | null;
    taxAddrRoad: string | null;
    taxAddrSubdistrict: string | null;
    taxAddrDistrict: string | null;
    taxAddrProvince: string | null;
    taxAddrPostcode: string | null;
    publishChannels: string[];
    remark: string | null;
    rowVersion: string;
}
export type DoctorCodeInput = Omit<DoctorCodeDetail, 'id' | 'rowVersion' | 'approvalStatus' | 'departmentCode' | 'doctorTypeCode'>;
const doctorCodeSchema = z
    .object({
    doctorId: z.string().min(1, 'โปรดเลือกประวัติแพทย์'),
    code: z
        .string()
        .trim()
        .min(1, 'โปรดระบุรหัสแพทย์')
        .max(20, 'รหัสแพทย์ต้องไม่เกิน 20 ตัวอักษร'),
    oldDoctorCode: z.string().trim().nullable(),
    isCentralCode: z.boolean(),
    displayNameTh: z.string().trim().min(1, 'โปรดระบุชื่อกลุ่ม/แพทย์ (ภาษาไทย)'),
    displayNameEn: z.string().trim().nullable(),
    taxInvoiceNameTh: z.string().trim().nullable(),
    taxInvoiceNameEn: z.string().trim().nullable(),
    boardStatus: z.string().nullable(),
    defaultDfDoctorCode: z.string().trim().nullable(),
    isDutyDoctor: z.boolean(),
    whtFormType: z.string().nullable(),
    canCheckinAnywhere: z.boolean(),
    arNoWaitPayment: z.boolean(),
    calcToHospitalNoPay: z.boolean(),
    doctorTypeId: z.string().nullable(),
    doctorGroupId: z.string().nullable(),
    departmentId: z.string().nullable(),
    clinicId: z.string().nullable(),
    privilegeTypeId: z.string().nullable(),
    statusPrivilegeId: z.string().nullable(),
    employeeCode: z.string().trim().nullable(),
    paymentTypeId: z.string().nullable(),
    startWorkDate: z.string().nullable(),
    employmentStatus: z.string().min(1),
    resignDate: z.string().nullable(),
    paymentCondition: z.string().trim().nullable(),
    hospitalAbsorbCcFee: z.boolean(),
    ccFeePercent: z
        .number()
        .min(0, 'ค่าธรรมเนียมต้องอยู่ระหว่าง 0 ถึง 100')
        .max(100, 'ค่าธรรมเนียมต้องอยู่ระหว่าง 0 ถึง 100')
        .nullable(),
    taxId: z
        .string()
        .trim()
        .nullable()
        .refine((v) => !v || /^\d{13}$/.test(v), 'เลขประจำตัวผู้เสียภาษีต้องเป็นตัวเลข 13 หลัก'),
    useHomeTaxAddress: z.boolean(),
    taxAddrNo: z.string().trim().nullable(),
    taxAddrBuilding: z.string().trim().nullable(),
    taxAddrSoi: z.string().trim().nullable(),
    taxAddrRoad: z.string().trim().nullable(),
    taxAddrSubdistrict: z.string().trim().nullable(),
    taxAddrDistrict: z.string().trim().nullable(),
    taxAddrProvince: z.string().trim().nullable(),
    taxAddrPostcode: z
        .string()
        .trim()
        .nullable()
        .refine((v) => !v || /^\d{5}$/.test(v), 'รหัสไปรษณีย์ต้องเป็นตัวเลข 5 หลัก'),
    publishChannels: z.array(z.string()),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().nullable(),
})
    .refine((v) => v.employmentStatus === 'WORKING' || !!v.resignDate, {
    path: ['resignDate'],
    message: 'แพทย์ที่ไม่ได้ปฏิบัติงานแล้วต้องระบุวันที่ลาออก',
})
    .refine((v) => !v.hospitalAbsorbCcFee || v.ccFeePercent !== null, {
    path: ['ccFeePercent'],
    message: 'โปรดระบุค่าธรรมเนียมบัตรเครดิตที่โรงพยาบาลออกให้',
});
const EMPLOYMENT_LABELS = new Map(EMPLOYMENT_STATUS_OPTIONS.map((o) => [o.value, o.label]));
export const doctorCodeScreen: ScreenDescriptor<DoctorCodeListItem, DoctorCodeDetail, DoctorCodeInput> = {
    id: 'doctor-code',
    resource: 'doctor-codes',
    path: '/doctors/code',
    titleTh: 'ข้อมูลรหัสแพทย์',
    breadcrumb: [{ label: 'จัดการข้อมูลแพทย์' }],
    api: createCrudApi<DoctorCodeListItem, DoctorCodeDetail, DoctorCodeInput>('doctor-codes'),
    columns: [
        { key: 'code', header: 'รหัสแพทย์', sortable: true, width: '150px' },
        { key: 'oldDoctorCode', header: 'รหัสแพทย์เก่า', sortable: true, width: '150px' },
        { key: 'displayNameTh', header: 'ชื่อกลุ่ม/แพทย์', sortable: true },
        {
            key: 'isDutyDoctor',
            header: 'แพทย์เวร',
            width: '130px',
            value: (row) => (row.isDutyDoctor ? 'อยู่เวร' : 'ไม่อยู่เวร'),
        },
        { key: 'departmentCode', header: 'แผนก', sortable: true, width: '140px' },
        { key: 'doctorTypeCode', header: 'ประเภทแพทย์', width: '140px' },
        {
            key: 'employmentStatus',
            header: 'สถานะปฏิบัติงาน',
            sortable: true,
            width: '170px',
            value: (row) => EMPLOYMENT_LABELS.get(row.employmentStatus) ?? row.employmentStatus,
        },
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
        { kind: 'lookup', name: 'doctorId', label: 'แพทย์', resource: 'doctors' },
        { kind: 'lookup', name: 'departmentId', label: 'แผนก', resource: 'departments' },
        {
            kind: 'select',
            name: 'approvalStatus',
            label: 'สถานะการอนุมัติ',
            options: APPROVAL_STATUS_OPTIONS,
        },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหารหัสแพทย์ รหัสเก่า หรือชื่อกลุ่ม/แพทย์',
    defaultSort: 'code',
    rowKey: (row) => row.id,
    emptyHint: 'รหัสแพทย์เป็นของแต่ละโรงพยาบาล แพทย์คนเดียวมีได้หลายรหัสในสาขาเดียวกัน',
    sections: [
        {
            title: 'รหัสและชื่อที่ใช้แสดง',
            fields: [
                {
                    kind: 'lookup',
                    name: 'doctorId',
                    label: 'ประวัติแพทย์',
                    resource: 'doctors',
                    required: true,
                    width: 'lg',
                },
                {
                    kind: 'text',
                    name: 'code',
                    label: 'รหัสแพทย์',
                    required: true,
                    maxLength: 20,
                    immutableOnEdit: true,
                },
                { kind: 'text', name: 'oldDoctorCode', label: 'รหัสแพทย์เก่า' },
                {
                    kind: 'bool',
                    name: 'isCentralCode',
                    label: 'ใช้รหัสกลางของเครือ',
                    trueLabel: 'ใช่',
                    falseLabel: 'ไม่ใช่',
                },
                { kind: 'text', name: 'displayNameTh', label: 'ชื่อกลุ่ม/แพทย์ (ภาษาไทย)', required: true },
                { kind: 'text', name: 'displayNameEn', label: 'ชื่อกลุ่ม/แพทย์ (ภาษาอังกฤษ)' },
                { kind: 'text', name: 'taxInvoiceNameTh', label: 'ชื่อออกใบกำกับภาษี (ภาษาไทย)' },
                { kind: 'text', name: 'taxInvoiceNameEn', label: 'ชื่อออกใบกำกับภาษี (ภาษาอังกฤษ)' },
                STATUS_FIELD,
            ],
        },
        {
            title: 'เงื่อนไขการทำงาน',
            fields: [
                {
                    kind: 'select',
                    name: 'boardStatus',
                    label: 'Status Board',
                    options: BOARD_STATUS_OPTIONS,
                    emptyLabel: 'ไม่ระบุ',
                },
                { kind: 'text', name: 'defaultDfDoctorCode', label: 'Default DF Doctor Code' },
                {
                    kind: 'bool',
                    name: 'isDutyDoctor',
                    label: 'แพทย์เวร',
                    trueLabel: 'อยู่เวร',
                    falseLabel: 'ไม่อยู่เวร',
                },
                {
                    kind: 'bool',
                    name: 'canCheckinAnywhere',
                    label: 'เช็คอินได้ทุกที่',
                    trueLabel: 'ได้',
                    falseLabel: 'ไม่ได้',
                },
                {
                    kind: 'bool',
                    name: 'arNoWaitPayment',
                    label: 'AR ไม่ต้องรอรับชำระ',
                    trueLabel: 'ไม่ต้องรอ',
                    falseLabel: 'ต้องรอ',
                },
                {
                    kind: 'bool',
                    name: 'calcToHospitalNoPay',
                    label: 'คำนวณเข้าโรงพยาบาลไม่จ่ายแพทย์',
                    trueLabel: 'ใช่',
                    falseLabel: 'ไม่ใช่',
                },
                { kind: 'text', name: 'employeeCode', label: 'รหัสพนักงาน', width: 'sm' },
                { kind: 'date', name: 'startWorkDate', label: 'วันที่เริ่มงาน' },
                {
                    kind: 'select',
                    name: 'employmentStatus',
                    label: 'สถานะการปฏิบัติงาน',
                    required: true,
                    options: EMPLOYMENT_STATUS_OPTIONS,
                },
                { kind: 'date', name: 'resignDate', label: 'วันที่ลาออก' },
            ],
        },
        {
            title: 'สังกัดและการจ่ายเงิน',
            fields: [
                {
                    kind: 'lookup',
                    name: 'doctorTypeId',
                    label: 'ประเภทแพทย์',
                    resource: 'doctor-types',
                    emptyLabel: 'ไม่ระบุ',
                },
                {
                    kind: 'lookup',
                    name: 'doctorGroupId',
                    label: 'กลุ่มแพทย์',
                    resource: 'doctor-groups',
                    emptyLabel: 'ไม่ระบุ',
                },
                {
                    kind: 'lookup',
                    name: 'departmentId',
                    label: 'แผนก (ตามศูนย์รายได้)',
                    resource: 'departments',
                    emptyLabel: 'ไม่ระบุ',
                },
                {
                    kind: 'lookup',
                    name: 'clinicId',
                    label: 'คลินิก',
                    resource: 'clinics',
                    emptyLabel: 'ไม่ระบุ',
                },
                {
                    kind: 'lookup',
                    name: 'privilegeTypeId',
                    label: 'Privilege Type',
                    resource: 'privilege-types',
                    emptyLabel: 'ไม่ระบุ',
                },
                {
                    kind: 'lookup',
                    name: 'statusPrivilegeId',
                    label: 'Status Privilege',
                    resource: 'status-privileges',
                    emptyLabel: 'ไม่ระบุ',
                },
                {
                    kind: 'lookup',
                    name: 'paymentTypeId',
                    label: 'ประเภทการจ่ายเงิน',
                    resource: 'payment-types',
                    emptyLabel: 'ไม่ระบุ',
                },
                { kind: 'text', name: 'paymentCondition', label: 'เงื่อนไขการจ่ายค่าแพทย์', width: 'lg' },
                {
                    kind: 'bool',
                    name: 'hospitalAbsorbCcFee',
                    label: 'โรงพยาบาลออกค่าธรรมเนียมบัตรเครดิต',
                    trueLabel: 'ออกให้',
                    falseLabel: 'ไม่ออกให้',
                },
                { kind: 'amount', name: 'ccFeePercent', label: 'ค่าธรรมเนียมบัตรเครดิต (%)' },
            ],
        },
        {
            title: 'ภาษีและที่อยู่ที่เสียภาษี',
            description: 'เลือกใช้ที่อยู่เดียวกับที่อยู่บ้านของแพทย์ หรือกรอกที่อยู่เฉพาะของรหัสนี้',
            fields: [
                {
                    kind: 'select',
                    name: 'whtFormType',
                    label: 'ประเภทหักภาษี ณ ที่จ่าย 3%',
                    options: WHT_FORM_OPTIONS,
                    emptyLabel: 'ไม่ระบุ',
                    width: 'lg',
                },
                { kind: 'text', name: 'taxId', label: 'เลขประจำตัวผู้เสียภาษี', maxLength: 13 },
                {
                    kind: 'bool',
                    name: 'useHomeTaxAddress',
                    label: 'ที่อยู่ที่เสียภาษี',
                    trueLabel: 'ใช้ที่อยู่บ้าน',
                    falseLabel: 'กรอกเอง',
                },
                { kind: 'text', name: 'taxAddrNo', label: 'เลขที่บ้าน', width: 'sm' },
                { kind: 'text', name: 'taxAddrBuilding', label: 'อาคาร/หมู่บ้าน' },
                { kind: 'text', name: 'taxAddrSoi', label: 'ซอย/ตรอก', width: 'sm' },
                { kind: 'text', name: 'taxAddrRoad', label: 'ถนน', width: 'sm' },
                { kind: 'text', name: 'taxAddrSubdistrict', label: 'ตำบล/แขวง', width: 'sm' },
                { kind: 'text', name: 'taxAddrDistrict', label: 'อำเภอ/เขต', width: 'sm' },
                { kind: 'text', name: 'taxAddrProvince', label: 'จังหวัด', width: 'sm' },
                { kind: 'text', name: 'taxAddrPostcode', label: 'รหัสไปรษณีย์', width: 'sm' },
                REMARK_FIELD,
            ],
        },
    ],
    schema: doctorCodeSchema,
    defaultValues: {
        doctorId: '',
        code: '',
        oldDoctorCode: null,
        isCentralCode: false,
        displayNameTh: '',
        displayNameEn: null,
        taxInvoiceNameTh: null,
        taxInvoiceNameEn: null,
        boardStatus: null,
        defaultDfDoctorCode: null,
        isDutyDoctor: false,
        whtFormType: null,
        canCheckinAnywhere: false,
        arNoWaitPayment: false,
        calcToHospitalNoPay: false,
        doctorTypeId: null,
        doctorGroupId: null,
        departmentId: null,
        clinicId: null,
        privilegeTypeId: null,
        statusPrivilegeId: null,
        employeeCode: null,
        paymentTypeId: null,
        startWorkDate: null,
        employmentStatus: 'WORKING',
        resignDate: null,
        paymentCondition: null,
        hospitalAbsorbCcFee: false,
        ccFeePercent: null,
        taxId: null,
        useHomeTaxAddress: true,
        taxAddrNo: null,
        taxAddrBuilding: null,
        taxAddrSoi: null,
        taxAddrRoad: null,
        taxAddrSubdistrict: null,
        taxAddrDistrict: null,
        taxAddrProvince: null,
        taxAddrPostcode: null,
        publishChannels: [],
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => omitFields(detail, 'id', 'rowVersion', 'approvalStatus', 'departmentCode', 'doctorTypeCode'),
    childTables: [
        bankAccountsChild,
        codeSpecialtiesChild,
        contractsChild,
        schedulesChild,
        scheduleOffsChild,
    ],
};
