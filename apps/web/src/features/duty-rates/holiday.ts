import { z } from 'zod';
import { createCrudApi } from '../../api/crud';
import type { RecordStatus } from '../../api/types';
import type { SelectOption } from '../../components/form/Select';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD, omitFields, } from '../master-data/descriptor';
import type { ChildTableDef, ScreenDescriptor } from '../master-data/descriptor';
export const HOLIDAY_PAY_MODE_OPTIONS: readonly SelectOption[] = [
    { value: 'MULTIPLIER', label: 'อัตราจ่าย (เท่า)' },
    { value: 'HOURLY', label: 'อัตราจ่ายรายชั่วโมง (บาท/ชั่วโมง)' },
];
const PAY_MODE_LABELS = new Map(HOLIDAY_PAY_MODE_OPTIONS.map((o) => [o.value, o.label]));
interface HolidayRow {
    id: string;
    holidayName: string;
    startDate: string;
    endDate: string;
    specialHours: number | null;
    payMode: string;
    payMultiplier: number | null;
    boardHourlyAmount: number | null;
    nonBoardHourlyAmount: number | null;
    clinicName: string | null;
    status: RecordStatus;
}
interface HolidayDetail {
    id: string;
    holidayName: string;
    startDate: string;
    endDate: string;
    specialHours: number | null;
    payMode: string;
    payMultiplier: number | null;
    boardHourlyAmount: number | null;
    nonBoardHourlyAmount: number | null;
    clinicId: string | null;
    status: RecordStatus;
    remark: string | null;
    rowVersion: string;
}
type HolidayInput = Omit<HolidayDetail, 'id' | 'rowVersion'>;
interface HolidayExclusionRow {
    id: string;
    holidayRateId: string;
    doctorCode: string | null;
    doctorName: string | null;
}
interface HolidayExclusionInput {
    holidayRateId: string;
    doctorCodeId: string;
}
const exclusionsChild: ChildTableDef<HolidayExclusionRow, HolidayExclusionInput> = {
    title: 'ยกเว้นแพทย์',
    description: 'แพทย์ที่ไม่ได้รับอัตราวันหยุดนี้ — ยังได้ค่าเวรตามอัตราปกติ',
    api: createCrudApi<HolidayExclusionRow, HolidayExclusionRow & {
        rowVersion: string;
    }, HolidayExclusionInput>('holiday-duty-exclusions'),
    parentKey: 'holidayRateId',
    rowKey: (row) => row.id,
    addLabel: 'เพิ่มแพทย์ที่ยกเว้น',
    columns: [
        { key: 'doctorCode', header: 'รหัสแพทย์', width: '170px' },
        { key: 'doctorName', header: 'แพทย์' },
    ],
    fields: [
        {
            kind: 'lookup',
            name: 'doctorCodeId',
            label: 'แพทย์',
            resource: 'doctor-codes',
            required: true,
            width: 'lg',
        },
    ],
    schema: z.object({
        holidayRateId: z.string().min(1),
        doctorCodeId: z.string().min(1, 'โปรดเลือกแพทย์'),
    }),
    defaultValues: { holidayRateId: '', doctorCodeId: '' },
    toInput: (row) => ({
        holidayRateId: row.holidayRateId,
        doctorCodeId: (row as unknown as {
            doctorCodeId: string;
        }).doctorCodeId,
    }),
    defaultSort: 'doctorCode',
    emptyHint: 'ไม่มีการยกเว้น — อัตรานี้ใช้กับแพทย์ทุกท่านที่ลงเวรในช่วงวันหยุด',
};
export const holidayDutyRateScreen: ScreenDescriptor<HolidayRow, HolidayDetail, HolidayInput> = {
    id: 'holiday-duty-rate',
    resource: 'holiday-duty-rates',
    path: '/duty-rates/holiday',
    titleTh: 'อัตราค่าเวรวันหยุดเทศกาล',
    breadcrumb: [{ label: 'อัตราค่าเวรและประกันรายได้' }],
    api: createCrudApi<HolidayRow, HolidayDetail, HolidayInput>('holiday-duty-rates'),
    columns: [
        { key: 'holidayName', header: 'วันหยุด', sortable: true },
        { key: 'startDate', header: 'เริ่มต้น', sortable: true, format: 'date', width: '130px' },
        { key: 'endDate', header: 'สิ้นสุด', format: 'date', width: '130px' },
        {
            key: 'specialHours',
            header: 'ค่าเวรพิเศษ (ชม.)',
            align: 'right',
            format: 'amount',
            width: '160px',
        },
        {
            key: 'payMode',
            header: 'ประเภทการจ่าย',
            width: '230px',
            value: (row) => PAY_MODE_LABELS.get(row.payMode) ?? row.payMode,
        },
        {
            key: 'payMultiplier',
            header: 'อัตราจ่าย (เท่า)',
            align: 'right',
            format: 'amount',
            width: '150px',
        },
        {
            key: 'boardHourlyAmount',
            header: 'จบ Board (บาท/ชม.)',
            align: 'right',
            format: 'amount',
            width: '180px',
        },
        {
            key: 'nonBoardHourlyAmount',
            header: 'ไม่จบ Board (บาท/ชม.)',
            align: 'right',
            format: 'amount',
            width: '190px',
        },
        {
            key: 'clinicName',
            header: 'คลินิก',
            width: '180px',
            value: (row) => row.clinicName ?? 'ทุกคลินิก',
        },
        STATUS_COLUMN,
    ],
    filters: [
        {
            kind: 'select',
            name: 'payMode',
            label: 'ประเภทการจ่าย',
            options: HOLIDAY_PAY_MODE_OPTIONS,
        },
        { kind: 'lookup', name: 'clinicId', label: 'คลินิก', resource: 'clinics' },
        { kind: 'date', name: 'from', label: 'ตั้งแต่วันที่' },
        { kind: 'date', name: 'to', label: 'ถึงวันที่' },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหาชื่อวันหยุด',
    defaultSort: '-startDate',
    rowKey: (row) => row.id,
    emptyHint: 'อัตราค่าเวรที่ใช้แทนอัตราปกติในวันหยุดเทศกาล',
    sections: [
        {
            title: 'ช่วงวันหยุด',
            fields: [
                { kind: 'text', name: 'holidayName', label: 'ชื่อวันหยุด', required: true, width: 'lg' },
                { kind: 'date', name: 'startDate', label: 'วันที่เริ่มต้น', required: true },
                { kind: 'date', name: 'endDate', label: 'วันที่สิ้นสุด', required: true },
                {
                    kind: 'amount',
                    name: 'specialHours',
                    label: 'ค่าเวรพิเศษ (ชั่วโมง)',
                    hint: 'จำนวนชั่วโมงที่คิดอัตราวันหยุด — ไม่ระบุคือทั้งกะ',
                },
                {
                    kind: 'lookup',
                    name: 'clinicId',
                    label: 'คลินิก',
                    resource: 'clinics',
                    emptyLabel: 'ทุกคลินิก',
                    width: 'lg',
                },
            ],
        },
        {
            title: 'อัตราจ่าย',
            description: 'เลือกได้อย่างเดียว — ช่องของอีกแบบจะถูกล้างเมื่อบันทึก',
            fields: [
                {
                    kind: 'radio',
                    name: 'payMode',
                    label: 'ประเภทการจ่าย',
                    required: true,
                    options: HOLIDAY_PAY_MODE_OPTIONS,
                    width: 'lg',
                },
                { kind: 'amount', name: 'payMultiplier', label: 'อัตราจ่าย (เท่า)' },
                {
                    kind: 'amount',
                    name: 'boardHourlyAmount',
                    label: 'อัตราค่าแพทย์จบ Board (บาท/ชั่วโมง)',
                },
                {
                    kind: 'amount',
                    name: 'nonBoardHourlyAmount',
                    label: 'อัตราค่าแพทย์ไม่จบ Board (บาท/ชั่วโมง)',
                },
                STATUS_FIELD,
                REMARK_FIELD,
            ],
        },
    ],
    schema: z
        .object({
        holidayName: z.string().trim().min(1, 'โปรดระบุชื่อวันหยุด'),
        startDate: z.string().min(1, 'โปรดระบุวันที่เริ่มต้น'),
        endDate: z.string().min(1, 'โปรดระบุวันที่สิ้นสุด'),
        specialHours: z.number().min(0, 'ค่าเวรพิเศษต้องไม่ติดลบ').nullable(),
        payMode: z.string().min(1),
        payMultiplier: z.number().min(0, 'อัตราจ่ายต้องไม่ติดลบ').nullable(),
        boardHourlyAmount: z.number().min(0, 'อัตราต้องไม่ติดลบ').nullable(),
        nonBoardHourlyAmount: z.number().min(0, 'อัตราต้องไม่ติดลบ').nullable(),
        clinicId: z.string().nullable(),
        status: z.enum(['ACTIVE', 'INACTIVE']),
        remark: z.string().trim().nullable(),
    })
        .refine((v) => v.endDate >= v.startDate, {
        path: ['endDate'],
        message: 'วันที่สิ้นสุดต้องไม่ก่อนวันที่เริ่มต้น',
    })
        .refine((v) => v.payMode !== 'MULTIPLIER' || v.payMultiplier !== null, {
        path: ['payMultiplier'],
        message: 'โปรดระบุอัตราจ่าย (เท่า)',
    })
        .refine((v) => v.payMode !== 'HOURLY' ||
        v.boardHourlyAmount !== null ||
        v.nonBoardHourlyAmount !== null, { path: ['boardHourlyAmount'], message: 'โปรดระบุอัตรารายชั่วโมงอย่างน้อยหนึ่งช่อง' }),
    defaultValues: {
        holidayName: '',
        startDate: '',
        endDate: '',
        specialHours: null,
        payMode: 'MULTIPLIER',
        payMultiplier: null,
        boardHourlyAmount: null,
        nonBoardHourlyAmount: null,
        clinicId: null,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => omitFields(detail, 'id', 'rowVersion'),
    childTables: [exclusionsChild],
};
