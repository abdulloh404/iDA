import { z } from 'zod';
import { tenantApi, tenantApiBlob } from '../../api/client';
import type { CrudApi } from '../../api/crud';
import type { AuditEntry, Paged, RecordStatus } from '../../api/types';
import type { SelectOption } from '../../components/form/Select';
import type { ColumnDef } from '../../components/data/DataTable';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD, omitFields, } from '../master-data/descriptor';
import type { AnyChildTableDef, ChildTableDef, FieldDef, FilterDef, ScreenDescriptor, } from '../master-data/descriptor';
import { DAY_OF_WEEK_OPTIONS } from './duty';
export const WORK_TIME_RULE_OPTIONS: readonly SelectOption[] = [
    { value: 'BY_WORK_TIME', label: 'Transaction ตามเวลาการทำงาน' },
    { value: 'IGNORE_TIME', label: 'ไม่สนใจเวลา' },
];
export const INCOME_BASE_OPTIONS: readonly SelectOption[] = [
    { value: 'BEFORE_SHARE', label: 'ก่อนหักส่วนแบ่ง' },
    { value: 'AFTER_SHARE', label: 'หลังหักส่วนแบ่ง' },
];
export const GUARANTEE_BASIS_OPTIONS: readonly SelectOption[] = [
    { value: 'ACCRUAL_BASIC', label: 'Accrual Basic' },
    {
        value: 'ACCRUAL_NO_WAIT_PAYMENT',
        label: 'Accrual Basic ลูกหนี้ไม่รอรับชำระ จ่ายแพทย์ได้เลย',
    },
    { value: 'CASH_BASIC', label: 'Cash Basic' },
    { value: 'CASH_ALL_RECEIPTS_IN_MONTH', label: 'Cash Basic ลูกหนี้รับชำระในเดือนทั้งหมดเทียบ' },
];
export const CREDIT_CARD_FEE_OPTIONS: readonly SelectOption[] = [
    { value: 'BEFORE_FEE', label: 'ก่อนหักค่าธรรมเนียม' },
    { value: 'AFTER_FEE', label: 'หลังหักค่าธรรมเนียม' },
];
export const ADMISSION_OPTIONS: readonly SelectOption[] = [
    { value: 'ALL', label: 'ทั้งหมด' },
    { value: 'IPD', label: 'IPD' },
    { value: 'OPD', label: 'OPD' },
];
export const CALC_MODE_OPTIONS: readonly SelectOption[] = [
    { value: 'NORMAL', label: 'แบบปกติ' },
    { value: 'CUMULATIVE', label: 'แบบสะสม' },
];
export const TREATMENT_SCOPE_OPTIONS: readonly SelectOption[] = [
    { value: 'INCLUDE', label: 'ใช้เทียบ' },
    { value: 'EXCLUDE', label: 'ยกเว้น' },
];
const BASIS_LABELS = new Map(GUARANTEE_BASIS_OPTIONS.map((o) => [o.value, o.label]));
const CALC_MODE_LABELS = new Map(CALC_MODE_OPTIONS.map((o) => [o.value, o.label]));
const SCOPE_LABELS = new Map(TREATMENT_SCOPE_OPTIONS.map((o) => [o.value, o.label]));
export interface GuaranteeRow {
    id: string;
    kind: string;
    doctorCode: string | null;
    doctorName: string | null;
    departmentName: string | null;
    startDate: string;
    endDate: string | null;
    incomeAmount: number | null;
    surplusStartRate: number | null;
    calcMode: string | null;
    compareWholeMonth406: boolean;
    admissionType: string;
    basis: string;
    status: RecordStatus;
    approvalStatus: string;
}
export interface GuaranteeDetail {
    id: string;
    kind: string;
    doctorCodeId: string;
    departmentId: string | null;
    startDate: string;
    endDate: string | null;
    incomeAmount: number | null;
    surplusStartRate: number | null;
    calcMode: string | null;
    compareWholeMonth406: boolean;
    compareInvoiceFrom: string | null;
    workTimeRule: string;
    checkinToleranceMinutes: number | null;
    admissionType: string;
    incomeBase: string;
    basis: string;
    creditCardFeeBase: string;
    compareDoctorCodeId: string | null;
    status: RecordStatus;
    approvalStatus: string;
    remark: string | null;
    rowVersion: string;
}
export type GuaranteeInput = Omit<GuaranteeDetail, 'id' | 'rowVersion'>;
export const guaranteeApi = {
    resource: 'guarantee-rates',
    list: (params, signal?: AbortSignal) => tenantApi<Paged<GuaranteeRow>>('/api/master-data/guarantee-rates', { params, signal }),
    get: (id, signal?: AbortSignal) => tenantApi<GuaranteeDetail>(`/api/master-data/guarantee-rates/${id}`, { signal }),
    create: (input) => tenantApi<GuaranteeDetail>('/api/master-data/guarantee-rates', { method: 'POST', body: input }),
    update: (id, input, rowVersion) => tenantApi<GuaranteeDetail>(`/api/master-data/guarantee-rates/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
    remove: (id) => tenantApi<void>(`/api/master-data/guarantee-rates/${id}`, { method: 'DELETE' }),
    history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/guarantee-rates/${id}/history`, { signal }),
    exportXlsx: (params) => tenantApiBlob('/api/master-data/guarantee-rates/export', { params }),
} satisfies CrudApi<GuaranteeRow, GuaranteeDetail, GuaranteeInput>;
interface TreatmentRow {
    id: string;
    guaranteeRateId: string;
    treatmentId: string;
    treatmentCode: string | null;
    treatmentName: string | null;
    scope: string;
}
interface TreatmentInput {
    guaranteeRateId: string;
    treatmentId: string;
    scope: string;
}
const treatmentsChild: ChildTableDef<TreatmentRow, TreatmentInput> = {
    title: 'Treatment ที่ใช้เทียบ',
    description: 'ไม่ระบุเลย = เทียบทุก Treatment · แถวที่เป็น "ยกเว้น" จะถูกตัดออกจากการเทียบ',
    api: {
        resource: 'guarantee-rate-treatments',
        list: (params, signal?: AbortSignal) => tenantApi<Paged<TreatmentRow>>('/api/master-data/guarantee-rate-treatments', { params, signal }),
        get: (id, signal?: AbortSignal) => tenantApi<TreatmentRow & { rowVersion: string; }>(`/api/master-data/guarantee-rate-treatments/${id}`, { signal }),
        create: (input) => tenantApi<TreatmentRow & { rowVersion: string; }>('/api/master-data/guarantee-rate-treatments', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => tenantApi<TreatmentRow & { rowVersion: string; }>(`/api/master-data/guarantee-rate-treatments/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => tenantApi<void>(`/api/master-data/guarantee-rate-treatments/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/guarantee-rate-treatments/${id}/history`, { signal }),
        exportXlsx: (params) => tenantApiBlob('/api/master-data/guarantee-rate-treatments/export', { params }),
    } satisfies CrudApi<TreatmentRow, TreatmentRow & { rowVersion: string; }, TreatmentInput>,
    parentKey: 'guaranteeRateId',
    rowKey: (row) => row.id,
    addLabel: 'เพิ่ม Treatment',
    columns: [
        { key: 'treatmentCode', header: 'รหัส Treatment', width: '180px' },
        { key: 'treatmentName', header: 'Treatment' },
        {
            key: 'scope',
            header: 'บทบาท',
            width: '140px',
            value: (row) => SCOPE_LABELS.get(row.scope) ?? row.scope,
        },
    ],
    fields: [
        {
            kind: 'lookup',
            name: 'treatmentId',
            label: 'Treatment',
            resource: 'treatments',
            required: true,
            width: 'lg',
        },
        {
            kind: 'radio',
            name: 'scope',
            label: 'บทบาท',
            required: true,
            options: TREATMENT_SCOPE_OPTIONS,
            width: 'lg',
        },
    ],
    schema: z.object({
        guaranteeRateId: z.string().min(1),
        treatmentId: z.string().min(1, 'โปรดเลือก Treatment'),
        scope: z.string().min(1),
    }),
    defaultValues: { guaranteeRateId: '', treatmentId: '', scope: 'INCLUDE' },
    toInput: (row) => ({
        guaranteeRateId: row.guaranteeRateId,
        treatmentId: row.treatmentId,
        scope: row.scope,
    }),
    defaultSort: 'treatmentCode',
    emptyHint: 'ไม่ได้ระบุ — เทียบจากทุก Treatment ของแพทย์ท่านนี้',
};
interface DayRow {
    id: string;
    guaranteeRateId: string;
    dayOfWeek: number;
    dayNameTh: string;
    incomeAmount: number | null;
    startTime: string | null;
    endTime: string | null;
    isExcluded: boolean;
}
interface DayInput {
    guaranteeRateId: string;
    dayOfWeek: number;
    incomeAmount: number | null;
    startTime: string | null;
    endTime: string | null;
    isExcluded: boolean;
}
const hhmm = (raw: string | null) => (raw === null ? null : raw.slice(0, 5));
type DayVariant = 'income' | 'schedule' | 'exclusion';
function daysChild(variant: DayVariant): ChildTableDef<DayRow, DayInput> {
    const income = variant === 'income';
    const exclusion = variant === 'exclusion';
    const timeColumns: readonly ColumnDef<DayRow>[] = [
        { key: 'startTime', header: 'เวลาเริ่มต้น', format: 'time', width: '150px' },
        { key: 'endTime', header: 'เวลาสิ้นสุด', format: 'time', width: '150px' },
    ];
    const timeFields: readonly FieldDef[] = [
        {
            kind: 'time',
            name: 'startTime',
            label: 'เวลาเริ่มต้น',
            hint: exclusion ? 'เว้นว่างทั้งคู่ = ยกเว้นทั้งวัน' : 'เว้นว่างทั้งคู่ = ใช้ Transaction ทั้งวัน',
        },
        { kind: 'time', name: 'endTime', label: 'เวลาสิ้นสุด' },
    ];
    return {
        title: income
            ? 'รายได้ตามวัน'
            : exclusion
                ? 'วันและเวลาที่ยกเว้น'
                : 'ช่วงเวลาคิดประกันรายได้',
        description: income
            ? 'ค่าเวรเหมาจ่ายของแต่ละวันในสัปดาห์'
            : exclusion
                ? 'วันที่ไม่นำมาคิดประกันรายได้ — ระบุช่วงเวลาได้ถ้ายกเว้นเฉพาะบางช่วง'
                : 'ช่วงเวลาที่นับ Transaction เข้ามาเทียบ แยกตามวันในสัปดาห์',
        api: {
            resource: 'guarantee-rate-days',
            list: (params, signal?: AbortSignal) => tenantApi<Paged<DayRow>>('/api/master-data/guarantee-rate-days', { params, signal }),
            get: (id, signal?: AbortSignal) => tenantApi<DayRow & { rowVersion: string; }>(`/api/master-data/guarantee-rate-days/${id}`, { signal }),
            create: (input) => tenantApi<DayRow & { rowVersion: string; }>('/api/master-data/guarantee-rate-days', { method: 'POST', body: input }),
            update: (id, input, rowVersion) => tenantApi<DayRow & { rowVersion: string; }>(`/api/master-data/guarantee-rate-days/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
            remove: (id) => tenantApi<void>(`/api/master-data/guarantee-rate-days/${id}`, { method: 'DELETE' }),
            history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/guarantee-rate-days/${id}/history`, { signal }),
            exportXlsx: (params) => tenantApiBlob('/api/master-data/guarantee-rate-days/export', { params }),
        } satisfies CrudApi<DayRow, DayRow & { rowVersion: string; }, DayInput>,
        parentKey: 'guaranteeRateId',
        rowKey: (row) => row.id,
        addLabel: 'เพิ่มวัน',
        columns: [
            { key: 'dayNameTh', header: 'วัน', width: '150px' },
            ...(income
                ? ([
                    {
                        key: 'incomeAmount',
                        header: 'รายได้ (บาท)',
                        align: 'right',
                        format: 'amount',
                    },
                ] as ColumnDef<DayRow>[])
                : timeColumns),
            ...(exclusion
                ? ([
                    {
                        key: 'isExcluded',
                        header: 'การยกเว้น',
                        width: '160px',
                        value: (row) => (row.isExcluded ? 'ยกเว้นทั้งวัน' : 'ยกเว้นเฉพาะช่วงเวลา'),
                    },
                ] as ColumnDef<DayRow>[])
                : []),
        ],
        fields: [
            {
                kind: 'select',
                name: 'dayOfWeek',
                label: 'วัน',
                required: true,
                options: DAY_OF_WEEK_OPTIONS,
            },
            ...(income
                ? ([{ kind: 'amount', name: 'incomeAmount', label: 'รายได้ (บาท)', required: true }] as FieldDef[])
                : timeFields),
            ...(exclusion
                ? ([
                    {
                        kind: 'bool',
                        name: 'isExcluded',
                        label: 'ขอบเขตการยกเว้น',
                        trueLabel: 'ยกเว้นทั้งวัน',
                        falseLabel: 'ยกเว้นเฉพาะช่วงเวลา',
                        width: 'lg',
                    },
                ] as FieldDef[])
                : []),
        ],
        schema: z
            .object({
            guaranteeRateId: z.string().min(1),
            dayOfWeek: z.coerce.number().int().min(0).max(6),
            incomeAmount: z.number().min(0, 'รายได้ต้องไม่ติดลบ').nullable(),
            startTime: z.string().nullable(),
            endTime: z.string().nullable(),
            isExcluded: z.boolean(),
        })
            .refine((v) => !income || v.incomeAmount !== null, {
            path: ['incomeAmount'],
            message: 'โปรดระบุรายได้',
        })
            .refine((v) => !v.startTime === !v.endTime, {
            path: ['endTime'],
            message: 'โปรดระบุทั้งเวลาเริ่มต้นและเวลาสิ้นสุด',
        })
            .refine((v) => !v.startTime || !v.endTime || v.endTime > v.startTime, {
            path: ['endTime'],
            message: 'เวลาสิ้นสุดต้องหลังเวลาเริ่มต้น',
        }),
        defaultValues: {
            guaranteeRateId: '',
            dayOfWeek: 1,
            incomeAmount: null,
            startTime: null,
            endTime: null,
            isExcluded: exclusion,
        },
        toInput: (row) => ({
            guaranteeRateId: row.guaranteeRateId,
            dayOfWeek: row.dayOfWeek,
            incomeAmount: row.incomeAmount,
            startTime: hhmm(row.startTime),
            endTime: hhmm(row.endTime),
            isExcluded: row.isExcluded,
        }),
        defaultSort: 'dayOfWeek',
        emptyHint: income
            ? 'ยังไม่ได้กำหนดรายได้รายวัน'
            : exclusion
                ? 'ไม่มีวันที่ยกเว้น — คิดประกันรายได้ทุกวัน'
                : 'ไม่ได้ระบุช่วงเวลา — ใช้ Transaction ทั้งวันเทียบ',
    };
}
const compareFields: readonly FieldDef[] = [
    { kind: 'date', name: 'compareInvoiceFrom', label: 'Invoice ในการเทียบตั้งแต่วันที่' },
    {
        kind: 'radio',
        name: 'workTimeRule',
        label: 'เวลาการทำงาน',
        required: true,
        options: WORK_TIME_RULE_OPTIONS,
        width: 'full',
    },
    {
        kind: 'number',
        name: 'checkinToleranceMinutes',
        label: 'ตรวจสอบ Transaction เข้าเวร (นาที)',
        hint: 'ติดลบ = ก่อนเข้าเวร · บวก = หลังเข้าเวร',
        min: -1440,
        max: 1440,
    },
    {
        kind: 'radio',
        name: 'admissionType',
        label: 'ประเภทผู้เข้ารับการรักษา',
        required: true,
        options: ADMISSION_OPTIONS,
        width: 'lg',
    },
    {
        kind: 'radio',
        name: 'incomeBase',
        label: 'ฐานรายได้',
        required: true,
        options: INCOME_BASE_OPTIONS,
        width: 'lg',
    },
    {
        kind: 'select',
        name: 'basis',
        label: 'รูปแบบการคำนวณ',
        required: true,
        options: GUARANTEE_BASIS_OPTIONS,
        width: 'full',
    },
    {
        kind: 'radio',
        name: 'creditCardFeeBase',
        label: 'ค่าธรรมเนียมบัตรเครดิต',
        required: true,
        options: CREDIT_CARD_FEE_OPTIONS,
        width: 'lg',
    },
    {
        kind: 'lookup',
        name: 'compareDoctorCodeId',
        label: 'รหัสแพทย์เทียบประกันรายได้',
        resource: 'doctor-codes',
        emptyLabel: 'ไม่ระบุ',
        hint: 'แพทย์อีกท่านที่รายได้ถูกนับรวมเข้ามาเทียบด้วย',
        width: 'lg',
    },
];
const commonColumns: readonly ColumnDef<GuaranteeRow>[] = [
    { key: 'doctorCode', header: 'รหัสแพทย์', sortable: true, width: '150px' },
    { key: 'doctorName', header: 'แพทย์' },
    { key: 'departmentName', header: 'แผนก', width: '170px' },
    { key: 'startDate', header: 'เริ่มต้น', sortable: true, format: 'date', width: '130px' },
    {
        key: 'endDate',
        header: 'สิ้นสุด',
        format: 'date',
        width: '140px',
        value: (row) => row.endDate ?? 'ไม่มีกำหนด',
    },
];
const commonFilters: readonly FilterDef[] = [
    { kind: 'lookup', name: 'doctorCodeId', label: 'แพทย์', resource: 'doctor-codes' },
    { kind: 'lookup', name: 'departmentId', label: 'แผนก', resource: 'departments' },
    { kind: 'date', name: 'from', label: 'ตั้งแต่วันที่' },
    { kind: 'date', name: 'to', label: 'ถึงวันที่' },
    { kind: 'status', name: 'status', label: 'สถานะ' },
];
export interface GuaranteeScreenOptions {
    id: string;
    kind: string;
    path: string;
    titleTh: string;
    emptyHint: string;
    incomeLabel?: string;
    incomeInDays?: boolean;
    withCompareBlock?: boolean;
    withSurplusRate?: boolean;
    withWholeMonth406?: boolean;
    withCalcMode?: boolean;
    dayVariant?: DayVariant;
    withTreatments?: boolean;
}
export function guaranteeScreen(options: GuaranteeScreenOptions): ScreenDescriptor<GuaranteeRow, GuaranteeDetail, GuaranteeInput> {
    const children: AnyChildTableDef[] = [];
    if (options.withTreatments)
        children.push(treatmentsChild);
    if (options.dayVariant)
        children.push(daysChild(options.dayVariant));
    return {
        id: options.id,
        resource: 'guarantee-rates',
        path: options.path,
        titleTh: options.titleTh,
        breadcrumb: [{ label: 'อัตราค่าเวรและประกันรายได้' }],
        api: guaranteeApi,
        fixedFilters: { kind: options.kind },
        columns: [
            ...commonColumns,
            ...(options.incomeInDays
                ? []
                : ([
                    {
                        key: 'incomeAmount',
                        header: options.incomeLabel ?? 'รายได้',
                        align: 'right',
                        format: 'amount',
                        sortable: true,
                        width: '180px',
                    },
                ] as ColumnDef<GuaranteeRow>[])),
            ...(options.withSurplusRate
                ? ([
                    {
                        key: 'surplusStartRate',
                        header: 'เรทเริ่มต้น Surplus',
                        align: 'right',
                        format: 'amount',
                        width: '180px',
                    },
                ] as ColumnDef<GuaranteeRow>[])
                : []),
            ...(options.withCalcMode
                ? ([
                    {
                        key: 'calcMode',
                        header: 'การคำนวณ',
                        width: '140px',
                        value: (row) => row.calcMode === null
                            ? '—'
                            : (CALC_MODE_LABELS.get(row.calcMode) ?? row.calcMode),
                    },
                ] as ColumnDef<GuaranteeRow>[])
                : []),
            ...(options.withWholeMonth406
                ? ([
                    {
                        key: 'compareWholeMonth406',
                        header: 'เทียบฐาน 40(6) ทั้งเดือน',
                        width: '210px',
                        value: (row) => (row.compareWholeMonth406 ? 'เทียบ' : 'ไม่เทียบ'),
                    },
                ] as ColumnDef<GuaranteeRow>[])
                : []),
            ...(options.withCompareBlock
                ? ([
                    {
                        key: 'basis',
                        header: 'รูปแบบการคำนวณ',
                        width: '260px',
                        value: (row) => BASIS_LABELS.get(row.basis) ?? row.basis,
                    },
                ] as ColumnDef<GuaranteeRow>[])
                : []),
            STATUS_COLUMN,
        ],
        filters: [
            ...(options.withCalcMode
                ? ([
                    {
                        kind: 'select',
                        name: 'calcMode',
                        label: 'การคำนวณ',
                        options: CALC_MODE_OPTIONS,
                    },
                ] as FilterDef[])
                : []),
            ...commonFilters,
        ],
        searchHint: 'ค้นหารหัสแพทย์หรือชื่อแพทย์',
        defaultSort: '-startDate',
        rowKey: (row) => row.id,
        emptyHint: options.emptyHint,
        sections: [
            {
                title: 'แพทย์และช่วงเวลา',
                fields: [
                    {
                        kind: 'lookup',
                        name: 'doctorCodeId',
                        label: 'แพทย์',
                        resource: 'doctor-codes',
                        required: true,
                        width: 'lg',
                    },
                    {
                        kind: 'lookup',
                        name: 'departmentId',
                        label: 'แผนก',
                        resource: 'departments',
                        emptyLabel: 'ไม่ระบุ',
                        width: 'lg',
                    },
                    { kind: 'date', name: 'startDate', label: 'วันที่เริ่มต้น', required: true },
                    {
                        kind: 'date',
                        name: 'endDate',
                        label: 'วันที่สิ้นสุด',
                        hint: 'เว้นว่างคือยังไม่มีกำหนดสิ้นสุด',
                    },
                    ...(options.incomeInDays
                        ? []
                        : ([
                            {
                                kind: 'amount',
                                name: 'incomeAmount',
                                label: options.incomeLabel ?? 'รายได้',
                                required: true,
                            },
                        ] as FieldDef[])),
                    ...(options.withSurplusRate
                        ? ([
                            {
                                kind: 'amount',
                                name: 'surplusStartRate',
                                label: 'เรทเริ่มต้น Surplus',
                            },
                        ] as FieldDef[])
                        : []),
                    ...(options.withCalcMode
                        ? ([
                            {
                                kind: 'radio',
                                name: 'calcMode',
                                label: 'การคำนวณ',
                                required: true,
                                options: CALC_MODE_OPTIONS,
                                width: 'lg',
                            },
                        ] as FieldDef[])
                        : []),
                    ...(options.withWholeMonth406
                        ? ([
                            {
                                kind: 'bool',
                                name: 'compareWholeMonth406',
                                label: 'ฐานรายได้ 40(6) ทั้งเดือน',
                                trueLabel: 'เทียบทั้งเดือน',
                                falseLabel: 'ไม่เทียบ',
                                width: 'lg',
                            },
                        ] as FieldDef[])
                        : []),
                ],
            },
            ...(options.withCompareBlock
                ? [
                    {
                        title: 'เงื่อนไขการเทียบรายได้',
                        description: 'กำหนดว่า Transaction ไหนถูกนับเข้ามาเทียบกับยอดประกัน',
                        fields: compareFields,
                    },
                ]
                : []),
            { title: 'สถานะ', fields: [STATUS_FIELD, REMARK_FIELD] },
        ],
        schema: guaranteeSchema,
        defaultValues: {
            kind: options.kind,
            doctorCodeId: '',
            departmentId: null,
            startDate: '',
            endDate: null,
            incomeAmount: null,
            surplusStartRate: null,
            calcMode: options.withCalcMode ? 'NORMAL' : null,
            compareWholeMonth406: false,
            compareInvoiceFrom: null,
            workTimeRule: 'BY_WORK_TIME',
            checkinToleranceMinutes: null,
            admissionType: 'ALL',
            incomeBase: 'BEFORE_SHARE',
            basis: 'ACCRUAL_BASIC',
            creditCardFeeBase: 'BEFORE_FEE',
            compareDoctorCodeId: null,
            status: 'ACTIVE',
            approvalStatus: 'DRAFT',
            remark: null,
        },
        toInput: (detail) => omitFields(detail, 'id', 'rowVersion'),
        ...(children.length > 0 ? { childTables: children } : {}),
    };
}
const guaranteeSchema = z
    .object({
    kind: z.string().min(1),
    doctorCodeId: z.string().min(1, 'โปรดเลือกแพทย์'),
    departmentId: z.string().nullable(),
    startDate: z.string().min(1, 'โปรดระบุวันที่เริ่มต้น'),
    endDate: z.string().nullable(),
    incomeAmount: z.number().min(0, 'รายได้ต้องไม่ติดลบ').nullable(),
    surplusStartRate: z.number().min(0, 'เรทเริ่มต้นต้องไม่ติดลบ').nullable(),
    calcMode: z.string().nullable(),
    compareWholeMonth406: z.boolean(),
    compareInvoiceFrom: z.string().nullable(),
    workTimeRule: z.string().min(1),
    checkinToleranceMinutes: z
        .number()
        .int()
        .min(-1440, 'ค่าคลาดเคลื่อนต้องไม่เกิน 1440 นาที')
        .max(1440, 'ค่าคลาดเคลื่อนต้องไม่เกิน 1440 นาที')
        .nullable(),
    admissionType: z.string().min(1),
    incomeBase: z.string().min(1),
    basis: z.string().min(1),
    creditCardFeeBase: z.string().min(1),
    compareDoctorCodeId: z.string().nullable(),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    approvalStatus: z.string().min(1),
    remark: z.string().trim().nullable(),
})
    .refine((v) => !v.endDate || v.endDate >= v.startDate, {
    path: ['endDate'],
    message: 'วันที่สิ้นสุดต้องไม่ก่อนวันที่เริ่มต้น',
})
    .refine((v) => v.kind === 'LUMP_SUM' || v.incomeAmount !== null, {
    path: ['incomeAmount'],
    message: 'โปรดระบุรายได้',
})
    .refine((v) => !v.compareDoctorCodeId || v.compareDoctorCodeId !== v.doctorCodeId, {
    path: ['compareDoctorCodeId'],
    message: 'รหัสแพทย์เทียบต้องไม่ใช่แพทย์เจ้าของอัตรา',
});
