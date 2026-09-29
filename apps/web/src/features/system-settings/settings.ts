import { z } from 'zod';
import { createCrudApi } from '../../api/crud';
import type { RecordStatus } from '../../api/types';
import type { SelectOption } from '../../components/form/Select';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD, omitFields } from '../master-data/descriptor';
import type { ScreenDescriptor } from '../master-data/descriptor';
const BREADCRUMB = [{ label: 'ตั้งค่าระบบ' }] as const;
const formatDate = (iso: string) => {
    const date = new Date(iso);
    if (Number.isNaN(date.getTime()))
        return iso;
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}/${date.getFullYear()} ${pad(date.getHours())}:${pad(date.getMinutes())}`;
};
const days = (label: string) => z
    .number({ invalid_type_error: `โปรดระบุ${label}`, required_error: `โปรดระบุ${label}` })
    .int(`${label}ต้องเป็นจำนวนเต็ม`)
    .min(1, `${label}ต้องอยู่ระหว่าง 1 ถึง 999 วัน`)
    .max(999, `${label}ต้องอยู่ระหว่าง 1 ถึง 999 วัน`);
const emailPattern = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;
export interface EmailTemplateListItem {
    id: string;
    code: string;
    name: string;
    subject: string;
    updatedAt: string;
}
export interface EmailTemplateDetail extends EmailTemplateListItem {
    description: string | null;
    body: string;
    rowVersion: string;
}
export interface EmailTemplateInput {
    subject: string;
    body: string;
}
export const emailTemplateScreen: ScreenDescriptor<EmailTemplateListItem, EmailTemplateDetail, EmailTemplateInput> = {
    id: 'email-template',
    resource: 'email-templates',
    path: '/system-settings/email-template',
    titleTh: 'ตั้งค่ารูปแบบอีเมล',
    breadcrumb: BREADCRUMB,
    api: createCrudApi<EmailTemplateListItem, EmailTemplateDetail, EmailTemplateInput>('email-templates'),
    columns: [
        { key: 'name', header: 'Template', sortable: true, width: '260px' },
        { key: 'subject', header: 'หัวข้ออีเมล', sortable: true },
        {
            key: 'updatedAt',
            header: 'วันที่แก้ไขล่าสุด',
            sortable: true,
            width: '170px',
            value: (row) => formatDate(row.updatedAt),
        },
    ],
    filters: [],
    searchHint: 'ค้นหาชื่อ Template หรือหัวข้ออีเมล',
    defaultSort: 'name',
    rowKey: (row) => row.id,
    emptyHint: 'แม่แบบอีเมลที่ระบบใช้ส่งถึงแพทย์และผู้ใช้งาน — ระบบสร้างให้ แก้ได้เฉพาะหัวข้อและเนื้อหา',
    systemDefined: true,
    sections: [
        {
            title: 'รายละเอียด',
            description: 'ข้อความในวงเล็บปีกกา เช่น {doctorName} ถูกแทนด้วยข้อมูลจริงตอนส่ง — ดูรายการที่ใช้ได้ในคำอธิบายของแต่ละ Template',
            fields: [
                { kind: 'text', name: 'subject', label: 'หัวข้ออีเมล', required: true, maxLength: 500, width: 'full' },
                { kind: 'richtext', name: 'body', label: 'เนื้อหาอีเมล', required: true, maxLength: 4000 },
            ],
        },
    ],
    schema: z.object({
        subject: z.string().trim().min(1, 'โปรดระบุหัวข้ออีเมล').max(500, 'หัวข้ออีเมลต้องไม่เกิน 500 ตัวอักษร'),
        body: z
            .string()
            .min(1, 'โปรดระบุเนื้อหาอีเมล')
            .max(4000, 'เนื้อหาอีเมลต้องไม่เกิน 4,000 ตัวอักษร (นับรวมการจัดรูปแบบ)'),
    }) as never,
    defaultValues: { subject: '', body: '' },
    toInput: (detail) => ({ subject: detail.subject, body: detail.body }),
};
export interface HisNotifyEmailListItem {
    id: string;
    email: string;
    recipientName: string | null;
    status: RecordStatus;
    updatedAt: string;
}
export interface HisNotifyEmailDetail {
    id: string;
    email: string;
    recipientName: string | null;
    status: RecordStatus;
    remark: string | null;
    rowVersion: string;
}
export type HisNotifyEmailInput = Omit<HisNotifyEmailDetail, 'id' | 'rowVersion'>;
export const hisNotifyEmailScreen: ScreenDescriptor<HisNotifyEmailListItem, HisNotifyEmailDetail, HisNotifyEmailInput> = {
    id: 'his-notify-email',
    resource: 'his-notify-emails',
    path: '/system-settings/his-email',
    titleTh: 'ตั้งค่าอีเมล HIS',
    breadcrumb: BREADCRUMB,
    api: createCrudApi<HisNotifyEmailListItem, HisNotifyEmailDetail, HisNotifyEmailInput>('his-notify-emails'),
    columns: [
        { key: 'email', header: 'อีเมล', sortable: true },
        { key: 'recipientName', header: 'ชื่อผู้รับ', sortable: true },
        STATUS_COLUMN,
    ],
    filters: [{ kind: 'status', name: 'status', label: 'สถานะ' }],
    searchHint: 'ค้นหาอีเมล หรือชื่อผู้รับ',
    defaultSort: 'email',
    rowKey: (row) => row.id,
    emptyHint: 'อีเมลของทีม HIS ที่ได้รับแจ้งเมื่อมีการเพิ่มหรือแก้ไขข้อมูลแพทย์ เพื่อปรับข้อมูลใน HIS ให้ตรงกัน',
    sections: [
        {
            fields: [
                { kind: 'text', name: 'email', label: 'อีเมล', required: true, maxLength: 100 },
                { kind: 'text', name: 'recipientName', label: 'ชื่อผู้รับ', maxLength: 100 },
                STATUS_FIELD,
                REMARK_FIELD,
            ],
        },
    ],
    schema: z.object({
        email: z
            .string()
            .trim()
            .min(1, 'โปรดระบุอีเมล')
            .max(100, 'อีเมลต้องไม่เกิน 100 ตัวอักษร')
            .regex(emailPattern, 'รูปแบบอีเมลไม่ถูกต้อง'),
        recipientName: z.string().trim().max(100).nullable(),
        status: z.enum(['ACTIVE', 'INACTIVE']),
        remark: z.string().trim().max(1000).nullable(),
    }) as never,
    defaultValues: { email: '', recipientName: null, status: 'ACTIVE', remark: null },
    toInput: (detail) => omitFields(detail, 'id', 'rowVersion'),
};
type SendCycle = 'MONTHLY' | 'YEARLY';
type PayslipArDetail = 'HIDE' | 'CURRENT_MONTH' | 'ALL';
export interface IncomeDocSettingDetail {
    id: string;
    payslipCycle: SendCycle;
    payslipDay: number;
    payslipArDetail: PayslipArDetail;
    certificate406Cycle: SendCycle;
    certificate406Day: number;
    certificate50TawiCycle: SendCycle;
    certificate50TawiDay: number;
    updatedAt: string;
    rowVersion: string;
}
export type IncomeDocSettingInput = Omit<IncomeDocSettingDetail, 'id' | 'updatedAt' | 'rowVersion'>;
const CYCLE_OPTIONS: readonly SelectOption[] = [
    { value: 'MONTHLY', label: 'รายเดือน' },
    { value: 'YEARLY', label: 'รายปี' },
];
const AR_DETAIL_OPTIONS: readonly SelectOption[] = [
    { value: 'HIDE', label: 'ไม่แสดงรายการลูกหนี้คงค้าง' },
    { value: 'CURRENT_MONTH', label: 'แสดงลูกหนี้คงค้างเฉพาะเดือนปัจจุบัน' },
    { value: 'ALL', label: 'แสดงลูกหนี้คงค้างทั้งหมด' },
];
const sendDay = z
    .number({ invalid_type_error: 'โปรดระบุวันที่ส่งตามรอบ', required_error: 'โปรดระบุวันที่ส่งตามรอบ' })
    .int()
    .min(1, 'วันที่ส่งตามรอบต้องอยู่ระหว่าง 1 ถึง 31')
    .max(31, 'วันที่ส่งตามรอบต้องอยู่ระหว่าง 1 ถึง 31');
const cycle = z.enum(['MONTHLY', 'YEARLY'], { errorMap: () => ({ message: 'โปรดระบุรอบการส่ง' }) });
const DAY_HINT = 'วันที่ 29–31 ในเดือนที่สั้นกว่า ส่งวันสุดท้ายของเดือน';
export const incomeDocSettingScreen: ScreenDescriptor<IncomeDocSettingDetail, IncomeDocSettingDetail, IncomeDocSettingInput> = {
    id: 'income-doc-setting',
    resource: 'income-doc-settings',
    path: '/system-settings/income-document',
    titleTh: 'ตั้งค่าการส่งเอกสารรายได้',
    breadcrumb: BREADCRUMB,
    api: createCrudApi<IncomeDocSettingDetail, IncomeDocSettingDetail, IncomeDocSettingInput>('income-doc-settings'),
    singleton: {
        intro: 'รอบและวันที่ระบบส่งเอกสารรายได้ทางอีเมลให้แพทย์ ตามปลายทางในหน้าตั้งค่าการออกสลิป',
    },
    columns: [],
    filters: [],
    defaultSort: 'updatedAt',
    rowKey: (row) => row.id,
    emptyHint: '',
    sections: [
        {
            title: 'สลิปเงินเดือน',
            fields: [
                { kind: 'select', name: 'payslipCycle', label: 'รอบการส่ง', required: true, options: CYCLE_OPTIONS },
                { kind: 'number', name: 'payslipDay', label: 'วันที่ส่งตามรอบ', required: true, min: 1, max: 31, hint: DAY_HINT },
                {
                    kind: 'radio',
                    name: 'payslipArDetail',
                    label: 'รายละเอียดสลิปเงินเดือน',
                    required: true,
                    options: AR_DETAIL_OPTIONS,
                    width: 'full',
                },
            ],
        },
        {
            title: 'หนังสือรับรองภาษีเงินได้ 40(6)',
            fields: [
                { kind: 'select', name: 'certificate406Cycle', label: 'รอบการส่ง', required: true, options: CYCLE_OPTIONS },
                { kind: 'number', name: 'certificate406Day', label: 'วันที่ส่งตามรอบ', required: true, min: 1, max: 31, hint: DAY_HINT },
            ],
        },
        {
            title: 'หนังสือรับรอง 50 ทวิ (ภ.ง.ด.1ก)',
            fields: [
                { kind: 'select', name: 'certificate50TawiCycle', label: 'รอบการส่ง', required: true, options: CYCLE_OPTIONS },
                { kind: 'number', name: 'certificate50TawiDay', label: 'วันที่ส่งตามรอบ', required: true, min: 1, max: 31, hint: DAY_HINT },
            ],
        },
    ],
    schema: z.object({
        payslipCycle: cycle,
        payslipDay: sendDay,
        payslipArDetail: z.enum(['HIDE', 'CURRENT_MONTH', 'ALL'], {
            errorMap: () => ({ message: 'โปรดระบุรายละเอียดสลิปเงินเดือน' }),
        }),
        certificate406Cycle: cycle,
        certificate406Day: sendDay,
        certificate50TawiCycle: cycle,
        certificate50TawiDay: sendDay,
    }) as never,
    defaultValues: {
        payslipCycle: 'MONTHLY',
        payslipDay: 5,
        payslipArDetail: 'HIDE',
        certificate406Cycle: 'MONTHLY',
        certificate406Day: 5,
        certificate50TawiCycle: 'YEARLY',
        certificate50TawiDay: 15,
    },
    toInput: (detail) => omitFields(detail, 'id', 'updatedAt', 'rowVersion'),
};
export interface HisDoctorCodeMapListItem {
    id: string;
    hisDoctorCode: string;
    doctorCode: string | null;
    doctorName: string | null;
    departmentName: string | null;
    status: RecordStatus;
}
export interface HisDoctorCodeMapDetail {
    id: string;
    hisDoctorCode: string;
    doctorCodeId: string;
    departmentId: string | null;
    status: RecordStatus;
    remark: string | null;
    rowVersion: string;
}
export type HisDoctorCodeMapInput = Omit<HisDoctorCodeMapDetail, 'id' | 'rowVersion'>;
export const hisDoctorCodeMapScreen: ScreenDescriptor<HisDoctorCodeMapListItem, HisDoctorCodeMapDetail, HisDoctorCodeMapInput> = {
    id: 'his-doctor-code-map',
    resource: 'his-doctor-code-maps',
    path: '/system-settings/central-doctor-code',
    titleTh: 'ตั้งค่ารหัสแพทย์ไปเป็นแพทย์กลาง',
    breadcrumb: BREADCRUMB,
    api: createCrudApi<HisDoctorCodeMapListItem, HisDoctorCodeMapDetail, HisDoctorCodeMapInput>('his-doctor-code-maps'),
    columns: [
        { key: 'hisDoctorCode', header: 'รหัสแพทย์ (HIS)', sortable: true, width: '170px' },
        { key: 'doctorCode', header: 'รหัสแพทย์กลาง', sortable: true, width: '160px' },
        { key: 'doctorName', header: 'แพทย์' },
        {
            key: 'departmentName',
            header: 'แผนก',
            sortable: true,
            value: (row) => row.departmentName ?? 'ทุกแผนก',
        },
        STATUS_COLUMN,
    ],
    filters: [
        { kind: 'lookup', name: 'departmentId', label: 'แผนก', resource: 'departments' },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหารหัสแพทย์ HIS รหัสแพทย์กลาง หรือชื่อแพทย์',
    defaultSort: 'hisDoctorCode',
    rowKey: (row) => row.id,
    emptyHint: 'รหัสแพทย์ที่ส่งมาจาก HIS แต่ไม่มีในระบบ จะถูกแปลงเป็นรหัสแพทย์กลางตามตารางนี้ตอนนำเข้ารายการรักษา',
    sections: [
        {
            fields: [
                { kind: 'text', name: 'hisDoctorCode', label: 'รหัสแพทย์', required: true, maxLength: 20, hint: 'ตามที่ HIS ส่งมา' },
                {
                    kind: 'lookup',
                    name: 'doctorCodeId',
                    label: 'รหัสแพทย์กลาง',
                    resource: 'doctor-codes',
                    required: true,
                    width: 'lg',
                },
                {
                    kind: 'lookup',
                    name: 'departmentId',
                    label: 'แผนก',
                    resource: 'departments',
                    emptyLabel: 'ทุกแผนก',
                    hint: 'ไม่ระบุ = ใช้กับทุกแผนก · แถวที่ระบุแผนกถูกใช้ก่อนแถวทุกแผนก',
                },
                STATUS_FIELD,
                REMARK_FIELD,
            ],
        },
    ],
    schema: z.object({
        hisDoctorCode: z.string().trim().min(1, 'โปรดระบุรหัสแพทย์').max(20, 'รหัสแพทย์ต้องไม่เกิน 20 ตัวอักษร'),
        doctorCodeId: z.string().min(1, 'โปรดระบุรหัสแพทย์กลาง'),
        departmentId: z.string().nullable(),
        status: z.enum(['ACTIVE', 'INACTIVE']),
        remark: z.string().trim().max(1000).nullable(),
    }) as never,
    defaultValues: { hisDoctorCode: '', doctorCodeId: '', departmentId: null, status: 'ACTIVE', remark: null },
    toInput: (detail) => omitFields(detail, 'id', 'rowVersion'),
};
export interface ExpiryAlertSettingDetail {
    id: string;
    alertDaysBefore: number;
    updatedAt: string;
    rowVersion: string;
}
export type ExpiryAlertSettingInput = Pick<ExpiryAlertSettingDetail, 'alertDaysBefore'>;
export const expiryAlertSettingScreen: ScreenDescriptor<ExpiryAlertSettingDetail, ExpiryAlertSettingDetail, ExpiryAlertSettingInput> = {
    id: 'expiry-alert-setting',
    resource: 'expiry-alert-settings',
    path: '/system-settings/expiry-alert',
    titleTh: 'ตั้งค่าแจ้งเตือนรายการใกล้หมดอายุ',
    breadcrumb: BREADCRUMB,
    api: createCrudApi<ExpiryAlertSettingDetail, ExpiryAlertSettingDetail, ExpiryAlertSettingInput>('expiry-alert-settings'),
    singleton: {
        intro: 'ระบบแจ้งเตือนก่อนรายการที่มีวันสิ้นสุดหมดอายุ เช่น อัตราค่าเวร ประกันรายได้ สัญญาแพทย์',
    },
    columns: [],
    filters: [],
    defaultSort: 'updatedAt',
    rowKey: (row) => row.id,
    emptyHint: '',
    sections: [
        {
            fields: [
                {
                    kind: 'number',
                    name: 'alertDaysBefore',
                    label: 'จำนวนวันที่ระบบต้องแจ้งเตือนก่อนรายการหมดอายุ',
                    required: true,
                    min: 1,
                    max: 999,
                    width: 'md',
                },
            ],
        },
    ],
    schema: z.object({ alertDaysBefore: days('จำนวนวันที่ระบบต้องแจ้งเตือนก่อนรายการหมดอายุ') }) as never,
    defaultValues: { alertDaysBefore: 30 },
    toInput: (detail) => ({ alertDaysBefore: detail.alertDaysBefore }),
};
export interface PasswordPolicyDetail {
    id: string;
    resetDays: number;
    warnDaysBefore: number;
    updatedAt: string;
    rowVersion: string;
}
export type PasswordPolicyInput = Pick<PasswordPolicyDetail, 'resetDays' | 'warnDaysBefore'>;
export const passwordPolicyScreen: ScreenDescriptor<PasswordPolicyDetail, PasswordPolicyDetail, PasswordPolicyInput> = {
    id: 'password-policy',
    resource: 'password-policies',
    path: '/system-settings/password-reset',
    titleTh: 'ตั้งค่าการรีเซ็ตรหัสผ่าน',
    breadcrumb: BREADCRUMB,
    api: createCrudApi<PasswordPolicyDetail, PasswordPolicyDetail, PasswordPolicyInput>('password-policies'),
    singleton: {
        intro: 'อายุรหัสผ่านของบัญชีประเภท Local ทั้งเครือ — รหัสที่หมดอายุเข้าสู่ระบบไม่ได้จนกว่าผู้ดูแลจะตั้งรหัสใหม่',
    },
    columns: [],
    filters: [],
    defaultSort: 'updatedAt',
    rowKey: (row) => row.id,
    emptyHint: '',
    sections: [
        {
            fields: [
                { kind: 'number', name: 'resetDays', label: 'จำนวนวันที่ต้องรีเซ็ตรหัสผ่าน', required: true, min: 1, max: 999, width: 'md' },
                {
                    kind: 'number',
                    name: 'warnDaysBefore',
                    label: 'จำนวนวันที่ระบบต้องแจ้งเตือนก่อนรายการหมดอายุ',
                    required: true,
                    min: 1,
                    max: 999,
                    width: 'md',
                    hint: 'ต้องน้อยกว่าจำนวนวันที่ต้องรีเซ็ตรหัสผ่าน',
                },
            ],
        },
    ],
    schema: z
        .object({
        resetDays: days('จำนวนวันที่ต้องรีเซ็ตรหัสผ่าน'),
        warnDaysBefore: days('จำนวนวันที่ระบบต้องแจ้งเตือนก่อนรายการหมดอายุ'),
    })
        .refine((v) => v.warnDaysBefore < v.resetDays, {
        path: ['warnDaysBefore'],
        message: 'โปรดระบุจำนวนวันที่ระบบต้องแจ้งเตือนก่อนรายการหมดอายุ น้อยกว่าจำนวนวันที่ต้องรีเซ็ตรหัสผ่าน',
    }) as never,
    defaultValues: { resetDays: 90, warnDaysBefore: 7 },
    toInput: (detail) => ({ resetDays: detail.resetDays, warnDaysBefore: detail.warnDaysBefore }),
};
export interface CheckinAreaDetail {
    id: string;
    latitude: number;
    longitude: number;
    radiusMeters: number;
    allowOutside: boolean;
    updatedAt: string;
    rowVersion: string;
}
export interface CheckinAreaInput {
    latitude: string;
    longitude: string;
    radiusMeters: number;
    allowOutside: boolean;
}
const coordinate = (label: string, limit: number) => z
    .string()
    .trim()
    .min(1, `โปรดระบุ${label}`)
    .max(20, `${label} ต้องไม่เกิน 20 ตัวอักษร`)
    .refine((v) => /^-?\d+(\.\d+)?$/.test(v), `${label} ต้องเป็นตัวเลข เช่น 13.765432`)
    .refine((v) => Math.abs(Number(v)) <= limit, `${label} ต้องอยู่ระหว่าง -${limit} ถึง ${limit}`);
export const checkinAreaScreen: ScreenDescriptor<CheckinAreaDetail, CheckinAreaDetail, CheckinAreaInput> = {
    id: 'checkin-area',
    resource: 'checkin-areas',
    path: '/system-settings/checkin-area',
    titleTh: 'ตั้งค่าพื้นที่การลงชื่อเข้าเวร',
    breadcrumb: BREADCRUMB,
    api: createCrudApi<CheckinAreaDetail, CheckinAreaDetail, CheckinAreaInput>('checkin-areas'),
    singleton: {
        intro: 'จุดศูนย์กลางและรัศมีที่แพทย์ Check-in / Check-out เวรจากแอปพลิเคชันมือถือได้ ของโรงพยาบาลที่เปิดอยู่',
    },
    columns: [],
    filters: [],
    defaultSort: 'updatedAt',
    rowKey: (row) => row.id,
    emptyHint: '',
    sections: [
        {
            title: 'ตำแหน่งโรงพยาบาล',
            description: 'คัดลอกพิกัดจาก Google Maps ได้ (คลิกขวาที่ตำแหน่ง แล้วคลิกตัวเลขพิกัด)',
            fields: [
                { kind: 'text', name: 'latitude', label: 'Latitude', required: true, maxLength: 20 },
                { kind: 'text', name: 'longitude', label: 'Longitude', required: true, maxLength: 20 },
            ],
        },
        {
            title: 'เงื่อนไขการลงชื่อ',
            fields: [
                {
                    kind: 'number',
                    name: 'radiusMeters',
                    label: 'ระยะการลงชื่อเข้าเวร (เมตร)',
                    required: true,
                    min: 1,
                    max: 99999,
                },
                {
                    kind: 'checkbox',
                    name: 'allowOutside',
                    label: 'อนุญาตให้ลงชื่อเข้าเวรนอกพื้นที่',
                    hint: 'แอปยังให้ลงชื่อได้ และบันทึกไว้ว่าอยู่นอกพื้นที่เพื่อให้ตรวจภายหลัง',
                },
            ],
        },
    ],
    schema: z.object({
        latitude: coordinate('Latitude', 90),
        longitude: coordinate('Longitude', 180),
        radiusMeters: z
            .number({ invalid_type_error: 'โปรดระบุระยะการลงชื่อเข้าเวร', required_error: 'โปรดระบุระยะการลงชื่อเข้าเวร' })
            .int()
            .min(1, 'ระยะการลงชื่อเข้าเวรต้องอยู่ระหว่าง 1 ถึง 99,999 เมตร')
            .max(99999, 'ระยะการลงชื่อเข้าเวรต้องอยู่ระหว่าง 1 ถึง 99,999 เมตร'),
        allowOutside: z.boolean(),
    }) as never,
    defaultValues: { latitude: '', longitude: '', radiusMeters: 200, allowOutside: false },
    toInput: (detail) => ({
        latitude: String(detail.latitude),
        longitude: String(detail.longitude),
        radiusMeters: detail.radiusMeters,
        allowOutside: detail.allowOutside,
    }),
};
export interface TermsDetail {
    id: string;
    content: string;
    version: number;
    updatedAt: string;
    rowVersion: string;
}
export interface TermsInput {
    content: string;
}
export const termsScreen: ScreenDescriptor<TermsDetail, TermsDetail, TermsInput> = {
    id: 'terms',
    resource: 'terms',
    path: '/system-settings/terms',
    titleTh: 'ตั้งค่าข้อกำหนดและเงื่อนไข',
    breadcrumb: BREADCRUMB,
    api: createCrudApi<TermsDetail, TermsDetail, TermsInput>('terms'),
    singleton: {
        intro: 'ข้อกำหนดที่ผู้ใช้ต้องยอมรับก่อนใช้งานระบบ — แก้เนื้อหาแล้วนับเป็นฉบับใหม่ ผู้ใช้ทุกคนต้องยอมรับอีกครั้ง',
    },
    columns: [],
    filters: [],
    defaultSort: 'updatedAt',
    rowKey: (row) => row.id,
    emptyHint: '',
    sections: [
        {
            fields: [
                { kind: 'richtext', name: 'content', label: 'เนื้อหาข้อกำหนดและเงื่อนไข', required: true },
            ],
        },
    ],
    schema: z.object({ content: z.string().min(1, 'โปรดระบุเนื้อหาข้อกำหนดและเงื่อนไข') }) as never,
    defaultValues: { content: '' },
    toInput: (detail) => ({ content: detail.content }),
};
