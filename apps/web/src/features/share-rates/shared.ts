import { z } from 'zod';
import { tenantApi, tenantApiBlob } from '../../api/client';
import type { CrudApi } from '../../api/crud';
import type { AuditEntry, Paged, RecordStatus } from '../../api/types';
import type { SelectOption } from '../../components/form/Select';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD, omitFields, } from '../master-data/descriptor';
import type { ChildTableDef, FieldDef, FilterDef, ScreenDescriptor, } from '../master-data/descriptor';
import type { ColumnDef } from '../../components/data/DataTable';
export const SHARE_TAX_KIND_OPTIONS: readonly SelectOption[] = [
    { value: 'TAX406', label: 'ภาษี 40(6)' },
    { value: 'NO_TAX_BASE', label: 'ไม่กระทบฐานภาษี' },
];
export const SHARE_TAX_BASE_OPTIONS: readonly SelectOption[] = [
    { value: 'BEFORE_SHARE', label: 'ยอดก่อนแบ่งแพทย์' },
    { value: 'AFTER_SHARE', label: 'ยอดหลังแบ่งแพทย์' },
];
export const ADMISSION_TYPE_OPTIONS: readonly SelectOption[] = [
    { value: 'ALL', label: 'ทั้งหมด' },
    { value: 'IPD', label: 'IPD' },
    { value: 'OPD', label: 'OPD' },
];
export const SOCIAL_KIND_OPTIONS: readonly SelectOption[] = [
    { value: 'PURE', label: 'ประกันสังคมเพียว' },
    { value: 'SHARED', label: 'ประกันสังคมสิทธิ์ร่วม' },
];
export const SHARE_MODE_OPTIONS: readonly SelectOption[] = [
    { value: 'PERCENT', label: 'ส่วนแบ่งแบบเปอร์เซ็นต์' },
    { value: 'FIX_AMOUNT', label: 'ส่วนแบ่งแบบ Fix Amount' },
];
const TAX_KIND_LABELS = new Map(SHARE_TAX_KIND_OPTIONS.map((o) => [o.value, o.label]));
const ADMISSION_LABELS = new Map(ADMISSION_TYPE_OPTIONS.map((o) => [o.value, o.label]));
const SOCIAL_KIND_LABELS = new Map(SOCIAL_KIND_OPTIONS.map((o) => [o.value, o.label]));
export interface ShareRateRow {
    id: string;
    level: string;
    socialKind: string | null;
    privateCaseCode: string | null;
    packageCode: string | null;
    detail: string | null;
    location: string | null;
    arCode: string | null;
    patientRightName: string | null;
    doctorCode: string | null;
    treatmentCode: string | null;
    treatmentCategoryCode: string | null;
    departmentName: string | null;
    activityCode: string | null;
    taxKind: string;
    admissionType: string;
    shareMode: string;
    sharePercent: number | null;
    fixPriceFrom: number | null;
    fixPriceTo: number | null;
    doctorPayAmount: number | null;
    excludeXrayEkg: boolean;
    effectiveFrom: string;
    effectiveTo: string | null;
    status: RecordStatus;
}
export interface ShareRateDetail {
    id: string;
    level: string;
    socialKind: string | null;
    privateCaseCode: string | null;
    packageCode: string | null;
    detail: string | null;
    location: string | null;
    arCodeId: string | null;
    patientRightId: string | null;
    doctorCodeId: string | null;
    treatmentId: string | null;
    treatmentCategoryId: string | null;
    departmentId: string | null;
    activityCode: string | null;
    taxKind: string;
    taxBase: string;
    admissionType: string;
    shareMode: string;
    sharePercent: number | null;
    fixPriceFrom: number | null;
    fixPriceTo: number | null;
    doctorPayAmount: number | null;
    excludeXrayEkg: boolean;
    effectiveFrom: string;
    effectiveTo: string | null;
    noExpiry: boolean;
    status: RecordStatus;
    remark: string | null;
    rowVersion: string;
}
export type ShareRateInput = Omit<ShareRateDetail, 'id' | 'rowVersion'>;
export const shareRateApi = {
    resource: 'share-rates',
    list: (params, signal?: AbortSignal) => tenantApi<Paged<ShareRateRow>>('/api/master-data/share-rates', { params, signal }),
    get: (id, signal?: AbortSignal) => tenantApi<ShareRateDetail>(`/api/master-data/share-rates/${id}`, { signal }),
    create: (input) => tenantApi<ShareRateDetail>('/api/master-data/share-rates', { method: 'POST', body: input }),
    update: (id, input, rowVersion) => tenantApi<ShareRateDetail>(`/api/master-data/share-rates/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
    remove: (id) => tenantApi<void>(`/api/master-data/share-rates/${id}`, { method: 'DELETE' }),
    history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/share-rates/${id}/history`, { signal }),
    exportXlsx: (params) => tenantApiBlob('/api/master-data/share-rates/export', { params }),
} satisfies CrudApi<ShareRateRow, ShareRateDetail, ShareRateInput>;
const commonColumns: readonly ColumnDef<ShareRateRow>[] = [
    {
        key: 'taxKind',
        header: 'ประเภทภาษี',
        width: '160px',
        value: (row) => TAX_KIND_LABELS.get(row.taxKind) ?? row.taxKind,
    },
    {
        key: 'admissionType',
        header: 'Admission',
        width: '120px',
        value: (row) => ADMISSION_LABELS.get(row.admissionType) ?? row.admissionType,
    },
    {
        key: 'sharePercent',
        header: 'ส่วนแบ่ง (%)',
        align: 'right',
        format: 'amount',
        width: '140px',
    },
    {
        key: 'effectiveFrom',
        header: 'เริ่มใช้',
        sortable: true,
        format: 'date',
        width: '130px',
    },
    {
        key: 'effectiveTo',
        header: 'สิ้นสุด',
        format: 'date',
        width: '150px',
        value: (row) => row.effectiveTo ?? 'ไม่มีวันหมดอายุ',
    },
    STATUS_COLUMN,
];
const fixPriceColumns: readonly ColumnDef<ShareRateRow>[] = [
    {
        key: 'fixPriceFrom',
        header: 'Fix Price ตั้งแต่',
        align: 'right',
        format: 'amount',
        width: '160px',
    },
    {
        key: 'fixPriceTo',
        header: 'ถึง',
        align: 'right',
        format: 'amount',
        width: '140px',
    },
    {
        key: 'doctorPayAmount',
        header: 'ส่วนที่ทำจ่ายแพทย์',
        align: 'right',
        format: 'amount',
        width: '180px',
    },
];
const commonFilters: readonly FilterDef[] = [
    { kind: 'select', name: 'taxKind', label: 'ประเภทภาษี', options: SHARE_TAX_KIND_OPTIONS },
    {
        kind: 'select',
        name: 'admissionType',
        label: 'Admission Type',
        options: ADMISSION_TYPE_OPTIONS,
    },
    { kind: 'status', name: 'status', label: 'สถานะ' },
];
const conditionFields: readonly FieldDef[] = [
    {
        kind: 'select',
        name: 'taxKind',
        label: 'ประเภทภาษี',
        required: true,
        options: SHARE_TAX_KIND_OPTIONS,
    },
    {
        kind: 'select',
        name: 'taxBase',
        label: 'ฐานภาษี',
        required: true,
        options: SHARE_TAX_BASE_OPTIONS,
    },
    {
        kind: 'select',
        name: 'admissionType',
        label: 'Admission Type',
        required: true,
        options: ADMISSION_TYPE_OPTIONS,
    },
    { kind: 'date', name: 'effectiveFrom', label: 'วันที่เริ่มใช้', required: true },
    { kind: 'date', name: 'effectiveTo', label: 'วันที่สิ้นสุดการใช้' },
    {
        kind: 'bool',
        name: 'noExpiry',
        label: 'วันหมดอายุ',
        trueLabel: 'ไม่มีวันหมดอายุ',
        falseLabel: 'มีวันหมดอายุ',
    },
];
function rateFields(withFixAmount: boolean): readonly FieldDef[] {
    if (!withFixAmount) {
        return [{ kind: 'amount', name: 'sharePercent', label: 'ส่วนแบ่งแบบเปอร์เซ็นต์ (%)', required: true }];
    }
    return [
        {
            kind: 'radio',
            name: 'shareMode',
            label: 'รูปแบบส่วนแบ่ง',
            required: true,
            options: SHARE_MODE_OPTIONS,
            width: 'lg',
        },
        { kind: 'amount', name: 'sharePercent', label: 'ส่วนแบ่งแบบเปอร์เซ็นต์ (%)' },
        { kind: 'amount', name: 'fixPriceFrom', label: 'ส่วนแบ่งแบบ Fix Price ตั้งแต่ (บาท)' },
        { kind: 'amount', name: 'fixPriceTo', label: 'ถึง (บาท)' },
        { kind: 'amount', name: 'doctorPayAmount', label: 'ส่วนที่ทำจ่ายแพทย์ (บาท)' },
    ];
}
export interface ExclusionRow {
    id: string;
    rateId: string;
    doctorCode: string | null;
    doctorName: string | null;
    doctorGroupCode: string | null;
    doctorGroupName: string | null;
}
interface ExclusionInput {
    rateId: string;
    doctorCodeId: string | null;
    doctorGroupId: string | null;
}
export const exclusionsChild: ChildTableDef<ExclusionRow, ExclusionInput> = {
    title: 'ยกเว้นแพทย์',
    description: 'แพทย์หรือกลุ่มแพทย์ที่ไม่ใช้อัตรานี้ — หนึ่งแถวเลือกได้อย่างเดียว',
    api: {
        resource: 'share-rate-exclusions',
        list: (params, signal?: AbortSignal) => tenantApi<Paged<ExclusionRow>>('/api/master-data/share-rate-exclusions', { params, signal }),
        get: (id, signal?: AbortSignal) => tenantApi<ExclusionRow & { rowVersion: string; }>(`/api/master-data/share-rate-exclusions/${id}`, { signal }),
        create: (input) => tenantApi<ExclusionRow & { rowVersion: string; }>('/api/master-data/share-rate-exclusions', { method: 'POST', body: input }),
        update: (id, input, rowVersion) => tenantApi<ExclusionRow & { rowVersion: string; }>(`/api/master-data/share-rate-exclusions/${id}`, { method: 'PUT', body: input, params: { rowVersion } }),
        remove: (id) => tenantApi<void>(`/api/master-data/share-rate-exclusions/${id}`, { method: 'DELETE' }),
        history: (id, signal?: AbortSignal) => tenantApi<AuditEntry[]>(`/api/master-data/share-rate-exclusions/${id}/history`, { signal }),
        exportXlsx: (params) => tenantApiBlob('/api/master-data/share-rate-exclusions/export', { params }),
    } satisfies CrudApi<ExclusionRow, ExclusionRow & { rowVersion: string; }, ExclusionInput>,
    parentKey: 'rateId',
    rowKey: (row) => row.id,
    addLabel: 'เพิ่มรายการยกเว้น',
    columns: [
        { key: 'doctorCode', header: 'รหัสแพทย์', width: '160px' },
        { key: 'doctorName', header: 'แพทย์' },
        { key: 'doctorGroupCode', header: 'รหัสกลุ่มแพทย์', width: '170px' },
        { key: 'doctorGroupName', header: 'กลุ่มแพทย์' },
    ],
    fields: [
        {
            kind: 'lookup',
            name: 'doctorCodeId',
            label: 'ยกเว้นแพทย์รายคน',
            resource: 'doctor-codes',
            emptyLabel: 'ไม่ระบุ',
            width: 'lg',
        },
        {
            kind: 'lookup',
            name: 'doctorGroupId',
            label: 'ยกเว้นทั้งกลุ่มแพทย์',
            resource: 'doctor-groups',
            emptyLabel: 'ไม่ระบุ',
            width: 'lg',
        },
    ],
    schema: z
        .object({
        rateId: z.string().min(1),
        doctorCodeId: z.string().nullable(),
        doctorGroupId: z.string().nullable(),
    })
        .refine((v) => !!v.doctorCodeId !== !!v.doctorGroupId, {
        path: ['doctorCodeId'],
        message: 'เลือกอย่างใดอย่างหนึ่งระหว่างแพทย์รายคนกับกลุ่มแพทย์',
    }),
    defaultValues: { rateId: '', doctorCodeId: null, doctorGroupId: null },
    toInput: (row) => ({
        rateId: row.rateId,
        doctorCodeId: (row as unknown as {
            doctorCodeId: string | null;
        }).doctorCodeId,
        doctorGroupId: (row as unknown as {
            doctorGroupId: string | null;
        }).doctorGroupId,
    }),
    defaultSort: 'doctorCode',
    emptyHint: 'ไม่มีการยกเว้น — อัตรานี้ใช้กับแพทย์ทุกคนที่เข้าเงื่อนไข',
};
const socialKindColumn: readonly ColumnDef<ShareRateRow>[] = [
    {
        key: 'socialKind',
        header: 'รหัสประกันสังคม',
        width: '200px',
        value: (row) => row.socialKind === null ? '—' : (SOCIAL_KIND_LABELS.get(row.socialKind) ?? row.socialKind),
    },
];
const socialKindFilter: readonly FilterDef[] = [
    { kind: 'select', name: 'socialKind', label: 'รหัสประกันสังคม', options: SOCIAL_KIND_OPTIONS },
];
const socialKindField: readonly FieldDef[] = [
    {
        kind: 'radio',
        name: 'socialKind',
        label: 'รหัสประกันสังคม',
        required: true,
        options: SOCIAL_KIND_OPTIONS,
        width: 'lg',
    },
];
export interface ShareScreenOptions {
    id: string;
    level: string;
    group: 'Premium' | 'ประกันสังคม';
    path: string;
    titleTh: string;
    emptyHint: string;
    searchHint: string;
    keyColumns: readonly ColumnDef<ShareRateRow>[];
    keyFields: readonly FieldDef[];
    keyFilters?: readonly FilterDef[];
    withFixAmount?: boolean;
    withXrayExclusion?: boolean;
    withExclusions?: boolean;
    socialKind?: boolean;
}
export function shareRateScreen(options: ShareScreenOptions): ScreenDescriptor<ShareRateRow, ShareRateDetail, ShareRateInput> {
    const withFixAmount = options.withFixAmount === true;
    return {
        id: options.id,
        resource: 'share-rates',
        path: options.path,
        titleTh: options.titleTh,
        breadcrumb: [{ label: 'ส่วนแบ่งค่าแพทย์' }, { label: options.group }],
        api: shareRateApi,
        fixedFilters: { level: options.level },
        columns: [
            ...(options.socialKind === true ? socialKindColumn : []),
            ...options.keyColumns,
            ...(withFixAmount ? fixPriceColumns : []),
            ...commonColumns,
        ],
        filters: [
            ...(options.socialKind === true ? socialKindFilter : []),
            ...(options.keyFilters ?? []),
            ...commonFilters,
        ],
        searchHint: options.searchHint,
        defaultSort: '-effectiveFrom',
        rowKey: (row) => row.id,
        emptyHint: options.emptyHint,
        sections: [
            {
                title: 'เงื่อนไขของอัตรา',
                fields: [
                    ...(options.socialKind === true ? socialKindField : []),
                    ...options.keyFields,
                ],
            },
            {
                title: 'ภาษีและช่วงเวลาที่ใช้',
                description: 'อัตราที่เงื่อนไขเดียวกันและช่วงวันที่ทับกันจะถูกปฏิเสธตอนบันทึก',
                fields: [
                    ...conditionFields,
                    ...(options.withXrayExclusion
                        ? ([
                            {
                                kind: 'bool',
                                name: 'excludeXrayEkg',
                                label: 'รายการ X-ray และ EKG',
                                trueLabel: 'ยกเว้น',
                                falseLabel: 'คำนวณปกติ',
                            },
                        ] as FieldDef[])
                        : []),
                ],
            },
            {
                title: 'อัตราส่วนแบ่ง',
                fields: [...rateFields(withFixAmount), STATUS_FIELD, REMARK_FIELD],
            },
        ],
        schema: shareRateSchema,
        defaultValues: {
            level: options.level,
            socialKind: options.socialKind === true ? 'PURE' : null,
            privateCaseCode: null,
            packageCode: null,
            detail: null,
            location: null,
            arCodeId: null,
            patientRightId: null,
            doctorCodeId: null,
            treatmentId: null,
            treatmentCategoryId: null,
            departmentId: null,
            activityCode: null,
            taxKind: 'TAX406',
            taxBase: 'BEFORE_SHARE',
            admissionType: 'ALL',
            shareMode: 'PERCENT',
            sharePercent: null,
            fixPriceFrom: null,
            fixPriceTo: null,
            doctorPayAmount: null,
            excludeXrayEkg: false,
            effectiveFrom: '',
            effectiveTo: null,
            noExpiry: false,
            status: 'ACTIVE',
            remark: null,
        },
        toInput: (detail) => omitFields(detail, 'id', 'rowVersion'),
        ...(options.withExclusions ? { childTables: [exclusionsChild] } : {}),
    };
}
const shareRateSchema = z
    .object({
    level: z.string().min(1),
    socialKind: z.string().nullable(),
    privateCaseCode: z.string().trim().nullable(),
    packageCode: z.string().trim().nullable(),
    detail: z.string().trim().nullable(),
    location: z.string().trim().nullable(),
    arCodeId: z.string().nullable(),
    patientRightId: z.string().nullable(),
    doctorCodeId: z.string().nullable(),
    treatmentId: z.string().nullable(),
    treatmentCategoryId: z.string().nullable(),
    departmentId: z.string().nullable(),
    activityCode: z.string().trim().nullable(),
    taxKind: z.string().min(1, 'โปรดเลือกประเภทภาษี'),
    taxBase: z.string().min(1, 'โปรดเลือกฐานภาษี'),
    admissionType: z.string().min(1, 'โปรดเลือก Admission Type'),
    shareMode: z.string().min(1),
    sharePercent: z
        .number()
        .min(0, 'ส่วนแบ่งต้องอยู่ระหว่าง 0 ถึง 100')
        .max(100, 'ส่วนแบ่งต้องอยู่ระหว่าง 0 ถึง 100')
        .nullable(),
    fixPriceFrom: z.number().min(0, 'ราคาต้องไม่ติดลบ').nullable(),
    fixPriceTo: z.number().min(0, 'ราคาต้องไม่ติดลบ').nullable(),
    doctorPayAmount: z.number().min(0, 'ส่วนที่ทำจ่ายแพทย์ต้องไม่ติดลบ').nullable(),
    excludeXrayEkg: z.boolean(),
    effectiveFrom: z.string().min(1, 'โปรดระบุวันที่เริ่มใช้'),
    effectiveTo: z.string().nullable(),
    noExpiry: z.boolean(),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: z.string().trim().nullable(),
})
    .refine((v) => v.noExpiry || !!v.effectiveTo, {
    path: ['effectiveTo'],
    message: 'โปรดระบุวันที่สิ้นสุด หรือเลือกไม่มีวันหมดอายุ',
})
    .refine((v) => !v.effectiveTo || v.effectiveTo >= v.effectiveFrom, {
    path: ['effectiveTo'],
    message: 'วันที่สิ้นสุดต้องไม่ก่อนวันที่เริ่มใช้',
})
    .refine((v) => v.shareMode !== 'PERCENT' || v.sharePercent !== null, {
    path: ['sharePercent'],
    message: 'โปรดระบุส่วนแบ่งแบบเปอร์เซ็นต์',
})
    .refine((v) => v.shareMode !== 'FIX_AMOUNT' || v.doctorPayAmount !== null, {
    path: ['doctorPayAmount'],
    message: 'โปรดระบุส่วนที่ทำจ่ายแพทย์',
});
