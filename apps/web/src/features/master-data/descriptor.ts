import type { ComponentType } from 'react';
import type { RouteObject } from 'react-router-dom';
import type { ZodType } from 'zod';
import type { CrudApi } from '../../api/crud';
import type { LookupResource } from '../../api/lookup';
import type { ColumnDef } from '../../components/data/DataTable';
import type { SelectOption } from '../../components/form/fields';
import type { PostcodeFill } from '../../components/form/PostcodeField';
import type { Crumb } from '../../components/layout/PageHeader';
export const FORM_MODES = ['create', 'edit', 'view'] as const;
export type FormMode = (typeof FORM_MODES)[number];
export type FilterDef = {
    kind: 'text';
    name: string;
    label: string;
    placeholder?: string;
} | {
    kind: 'select';
    name: string;
    label: string;
    options: readonly SelectOption[];
} | {
    kind: 'lookup';
    name: string;
    label: string;
    resource: LookupResource;
} | {
    kind: 'date';
    name: string;
    label: string;
} | {
    kind: 'status';
    name: 'status';
    label: string;
};
export type ApprovalAction = 'APPROVE' | 'REJECT' | 'RETURN';
export const FIELD_WIDTHS = ['sm', 'md', 'lg', 'full'] as const;
export type FieldWidth = (typeof FIELD_WIDTHS)[number];
interface FieldBase {
    name: string;
    label: string;
    hint?: string;
    width?: FieldWidth;
}
export type FieldDef = (FieldBase & {
    kind: 'text';
    required?: boolean;
    maxLength?: number;
    immutableOnEdit?: boolean;
    autoFilled?: boolean;
}) | (FieldBase & {
    kind: 'textarea';
    rows?: number;
}) | (FieldBase & {
    kind: 'password';
    maxLength?: number;
    placeholder?: string;
}) | (FieldBase & {
    kind: 'richtext';
    required?: boolean;
    maxLength?: number;
}) | (FieldBase & {
    kind: 'number';
    required?: boolean;
    min?: number;
    max?: number;
}) | (FieldBase & {
    kind: 'amount';
    required?: boolean;
}) | (FieldBase & {
    kind: 'select';
    required?: boolean;
    options: readonly SelectOption[];
    emptyLabel?: string;
}) | (FieldBase & {
    kind: 'radio';
    required?: boolean;
    options: readonly SelectOption[];
}) | (FieldBase & {
    kind: 'lookup';
    resource: LookupResource;
    required?: boolean;
    emptyLabel?: string;
}) | (FieldBase & {
    kind: 'checkbox';
}) | (FieldBase & {
    kind: 'bool';
    trueLabel: string;
    falseLabel: string;
}) | (FieldBase & {
    kind: 'switch';
    onValue: string;
    offValue: string;
    onLabel: string;
    offLabel: string;
}) | (FieldBase & {
    kind: 'date';
    required?: boolean;
}) | (FieldBase & {
    kind: 'time';
    required?: boolean;
}) | (FieldBase & {
    kind: 'datetime';
    required?: boolean;
}) | (FieldBase & {
    kind: 'postcode';
    required?: boolean;
    fill: PostcodeFill;
});
const DEFAULT_WIDTH: Record<FieldDef['kind'], FieldWidth> = {
    text: 'md',
    textarea: 'full',
    password: 'md',
    richtext: 'full',
    number: 'sm',
    amount: 'sm',
    select: 'md',
    lookup: 'md',
    radio: 'md',
    checkbox: 'full',
    bool: 'md',
    switch: 'md',
    date: 'sm',
    time: 'sm',
    datetime: 'md',
    postcode: 'sm',
};
export function fieldWidth(field: FieldDef): FieldWidth {
    return field.width ?? DEFAULT_WIDTH[field.kind];
}
export interface FormSectionDef {
    title?: string;
    description?: string;
    fields: readonly FieldDef[];
}
export interface ScreenDescriptor<TList, TDetail, TInput> {
    id: string;
    resource: string;
    path: string;
    titleTh: string;
    titleEn?: string;
    breadcrumb: readonly Crumb[];
    api: CrudApi<TList, TDetail, TInput>;
    columns: readonly ColumnDef<TList>[];
    filters: readonly FilterDef[];
    fixedFilters?: Readonly<Record<string, string>>;
    searchHint?: string;
    defaultSort: string;
    rowKey: (row: TList) => string;
    emptyHint: string;
    sections: readonly FormSectionDef[];
    schema: ZodType<TInput>;
    defaultValues: TInput;
    toInput: (detail: TDetail) => TInput;
    childTables?: readonly AnyChildTableDef[];
    approval?: {
        decide: (ids: readonly string[], action: ApprovalAction, comment: string | null) => Promise<unknown>;
        permission: string;
        statusFilter: string;
    };
    isLocked?: (detail: TDetail) => string | null;
    systemDefined?: true;
    singleton?: {
        intro: string;
    };
    formPanels?: readonly ComponentType<{
        id: string;
        detail: TDetail;
        readOnly: boolean;
    }>[];
    ListActions?: ComponentType;
    ListScreen?: ComponentType<{
        descriptor: ScreenDescriptor<TList, TDetail, TInput>;
    }>;
    FormScreen?: ComponentType<{
        descriptor: ScreenDescriptor<TList, TDetail, TInput>;
        mode: FormMode;
    }>;
    extraRoutes?: RouteObject[];
}
export interface ChildTableDef<TRow, TInput> {
    title: string;
    description?: string;
    api: CrudApi<TRow, TRow & {
        rowVersion: string;
    }, TInput>;
    parentKey: string;
    parentValueFrom?: string;
    columns: readonly ColumnDef<TRow>[];
    fields: readonly FieldDef[];
    schema: ZodType<TInput>;
    defaultValues: TInput;
    toInput: (row: TRow & {
        rowVersion: string;
    }) => TInput;
    rowKey: (row: TRow) => string;
    defaultSort?: string;
    emptyHint: string;
    addLabel?: string;
    readOnly?: boolean;
}
export type AnyChildTableDef = ChildTableDef<any, any>;
export type AnyScreenDescriptor = ScreenDescriptor<any, any, any>;
export function omitFields<T extends object, K extends keyof T>(source: T, ...keys: K[]): Omit<T, K> {
    const copy = { ...source };
    for (const key of keys)
        delete copy[key];
    return copy;
}
export const RECORD_STATUS_OPTIONS: readonly SelectOption[] = [
    { value: 'ACTIVE', label: 'ใช้งาน' },
    { value: 'INACTIVE', label: 'ไม่ใช้งาน' },
];
export const STATUS_FIELD = {
    kind: 'switch',
    name: 'status',
    label: 'สถานะ',
    onValue: 'ACTIVE',
    offValue: 'INACTIVE',
    onLabel: 'ใช้งาน',
    offLabel: 'ไม่ใช้งาน',
} as const satisfies FieldDef;
export const REMARK_FIELD = {
    kind: 'textarea',
    name: 'remark',
    label: 'หมายเหตุ',
    rows: 3,
} as const satisfies FieldDef;
export const STATUS_COLUMN = {
    key: 'status',
    header: 'สถานะ',
    sortable: true,
    format: 'status',
    width: '140px',
} as const;
export const ITEM_DIRECTION_OPTIONS: readonly SelectOption[] = [
    { value: 'ADD', label: 'เพิ่ม' },
    { value: 'DEDUCT', label: 'หัก' },
];
export const RECEIPT_PAYMENT_FORM_OPTIONS: readonly SelectOption[] = [
    { value: 'AR', label: 'AR (ตั้งหนี้)' },
    { value: 'CASH', label: 'CASH (เงินสด)' },
    { value: 'CHEQUE', label: 'CQ (เช็ค)' },
    { value: 'CREDIT_CARD', label: 'CREDIT CARD' },
    { value: 'DP', label: 'DP (เงินมัดจำ)' },
    { value: 'DPC', label: 'DPC (ตัดเงินมัดจำ)' },
    { value: 'INVOICE', label: 'INVOICE' },
];
export const GL_POSTING_DATE_RULE_OPTIONS: readonly SelectOption[] = [
    { value: 'BATCH_DATE', label: 'วันที่ Batch Date' },
    { value: 'MONTH_END', label: 'วันสิ้นสุดเดือน' },
    { value: 'PAYMENT_DATE', label: 'วันที่จ่ายเงิน' },
];
export const INVOICE_CALC_MODE_OPTIONS: readonly SelectOption[] = [
    { value: 'NORMAL_SHARE', label: 'คำนวณส่งแบ่งปกติ' },
    { value: 'TO_HOSPITAL', label: 'คำนวณเข้าโรงพยาบาล' },
];
