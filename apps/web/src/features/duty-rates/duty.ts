import { z } from 'zod';
import { createCrudApi } from '../../api/crud';
import type { RecordStatus } from '../../api/types';
import type { SelectOption } from '../../components/form/Select';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD, omitFields, } from '../master-data/descriptor';
import type { ChildTableDef, ScreenDescriptor } from '../master-data/descriptor';
export const DUTY_ROOM_OPTIONS: readonly SelectOption[] = [
    { value: 'ROOM1', label: 'ห้อง/เวร 1' },
    { value: 'ROOM2', label: 'ห้อง/เวร 2' },
    { value: 'ROOM3', label: 'ห้อง/เวร 3' },
    { value: 'ROOM4', label: 'ห้อง/เวร 4' },
    { value: 'ROOM5', label: 'ห้อง/เวร 5' },
    { value: 'OTHER', label: 'อื่น ๆ' },
];
export const DUTY_PAY_KIND_OPTIONS: readonly SelectOption[] = [
    { value: 'NORMAL', label: 'ปกติ' },
    { value: 'ON_TOP', label: 'On Top' },
    { value: 'LUMP_SUM', label: 'เหมาจ่าย' },
    { value: 'SURPLUS', label: 'Surplus' },
];
export const DAY_OF_WEEK_OPTIONS: readonly SelectOption[] = [
    { value: '0', label: 'อาทิตย์' },
    { value: '1', label: 'จันทร์' },
    { value: '2', label: 'อังคาร' },
    { value: '3', label: 'พุธ' },
    { value: '4', label: 'พฤหัสบดี' },
    { value: '5', label: 'ศุกร์' },
    { value: '6', label: 'เสาร์' },
];
const ROOM_LABELS = new Map(DUTY_ROOM_OPTIONS.map((o) => [o.value, o.label]));
const PAY_KIND_LABELS = new Map(DUTY_PAY_KIND_OPTIONS.map((o) => [o.value, o.label]));
interface DutyRow {
    id: string;
    departmentName: string | null;
    room: string;
    roomOther: string | null;
    startTime: string;
    endTime: string;
    payKind: string;
    dayCount: number;
    status: RecordStatus;
}
interface DutyDetail {
    id: string;
    departmentId: string;
    room: string;
    roomOther: string | null;
    startTime: string;
    endTime: string;
    payKind: string;
    status: RecordStatus;
    remark: string | null;
    rowVersion: string;
}
type DutyInput = Omit<DutyDetail, 'id' | 'rowVersion'>;
interface DutyDayRow {
    id: string;
    dutyRateId: string;
    dayOfWeek: number;
    dayNameTh: string;
    nonBoardHourlyAmount: number | null;
    boardHourlyAmount: number | null;
}
interface DutyDayInput {
    dutyRateId: string;
    dayOfWeek: number;
    nonBoardHourlyAmount: number | null;
    boardHourlyAmount: number | null;
}
const daysChild: ChildTableDef<DutyDayRow, DutyDayInput> = {
    title: 'อัตราค่าเวรรายวัน',
    description: 'บาท/ชั่วโมง แยกตามวันในสัปดาห์ — วันที่ไม่ได้กำหนดจะใช้อัตราปกติของแผนก',
    api: createCrudApi<DutyDayRow, DutyDayRow & {
        rowVersion: string;
    }, DutyDayInput>('duty-rate-days'),
    parentKey: 'dutyRateId',
    rowKey: (row) => row.id,
    addLabel: 'เพิ่มวัน',
    columns: [
        { key: 'dayNameTh', header: 'วัน', width: '160px' },
        {
            key: 'nonBoardHourlyAmount',
            header: 'ไม่จบ Board (บาท/ชม.)',
            align: 'right',
            format: 'amount',
        },
        {
            key: 'boardHourlyAmount',
            header: 'จบ Board (บาท/ชม.)',
            align: 'right',
            format: 'amount',
        },
    ],
    fields: [
        {
            kind: 'select',
            name: 'dayOfWeek',
            label: 'วัน',
            required: true,
            options: DAY_OF_WEEK_OPTIONS,
            width: 'md',
        },
        {
            kind: 'amount',
            name: 'nonBoardHourlyAmount',
            label: 'อัตราค่าแพทย์ไม่จบ Board (บาท/ชั่วโมง)',
        },
        {
            kind: 'amount',
            name: 'boardHourlyAmount',
            label: 'อัตราค่าแพทย์จบ Board (บาท/ชั่วโมง)',
        },
    ],
    schema: z
        .object({
        dutyRateId: z.string().min(1),
        dayOfWeek: z.coerce.number().int().min(0).max(6),
        nonBoardHourlyAmount: z.number().min(0, 'อัตราต้องไม่ติดลบ').nullable(),
        boardHourlyAmount: z.number().min(0, 'อัตราต้องไม่ติดลบ').nullable(),
    })
        .refine((v) => v.nonBoardHourlyAmount !== null || v.boardHourlyAmount !== null, {
        path: ['nonBoardHourlyAmount'],
        message: 'โปรดระบุอัตราอย่างน้อยหนึ่งช่อง',
    }),
    defaultValues: {
        dutyRateId: '',
        dayOfWeek: 1,
        nonBoardHourlyAmount: null,
        boardHourlyAmount: null,
    },
    toInput: (row) => ({
        dutyRateId: row.dutyRateId,
        dayOfWeek: row.dayOfWeek,
        nonBoardHourlyAmount: row.nonBoardHourlyAmount,
        boardHourlyAmount: row.boardHourlyAmount,
    }),
    defaultSort: 'dayOfWeek',
    emptyHint: 'ยังไม่ได้กำหนดอัตรารายวัน',
};
export const dutyRateScreen: ScreenDescriptor<DutyRow, DutyDetail, DutyInput> = {
    id: 'duty-rate',
    resource: 'duty-rates',
    path: '/duty-rates/duty',
    titleTh: 'อัตราค่าแพทย์เวร',
    breadcrumb: [{ label: 'อัตราค่าเวรและประกันรายได้' }],
    api: createCrudApi<DutyRow, DutyDetail, DutyInput>('duty-rates'),
    columns: [
        { key: 'departmentName', header: 'แผนก', sortable: true },
        {
            key: 'room',
            header: 'ห้อง/เวร',
            width: '170px',
            value: (row) => row.room === 'OTHER'
                ? (row.roomOther ?? 'อื่น ๆ')
                : (ROOM_LABELS.get(row.room) ?? row.room),
        },
        { key: 'startTime', header: 'เริ่ม', format: 'time', sortable: true, width: '110px' },
        { key: 'endTime', header: 'สิ้นสุด', format: 'time', width: '110px' },
        {
            key: 'payKind',
            header: 'ประเภทการจ่าย',
            width: '160px',
            value: (row) => PAY_KIND_LABELS.get(row.payKind) ?? row.payKind,
        },
        {
            key: 'dayCount',
            header: 'วันที่กำหนดอัตรา',
            align: 'right',
            width: '160px',
            value: (row) => `${row.dayCount} วัน`,
        },
        STATUS_COLUMN,
    ],
    filters: [
        { kind: 'lookup', name: 'departmentId', label: 'แผนก', resource: 'departments' },
        { kind: 'select', name: 'room', label: 'ห้อง/เวร', options: DUTY_ROOM_OPTIONS },
        {
            kind: 'select',
            name: 'payKind',
            label: 'ประเภทการจ่ายค่าเวร',
            options: DUTY_PAY_KIND_OPTIONS,
        },
        { kind: 'status', name: 'status', label: 'สถานะ' },
    ],
    searchHint: 'ค้นหาชื่อแผนกหรือชื่อห้อง/เวร',
    defaultSort: 'startTime',
    rowKey: (row) => row.id,
    emptyHint: 'อัตราค่าเวรตามแผนก ห้อง/เวร และช่วงเวลา',
    sections: [
        {
            title: 'แผนกและช่วงเวลา',
            description: 'เวรข้ามเที่ยงคืนกรอกได้ตามปกติ เช่น 22:00 ถึง 06:00',
            fields: [
                {
                    kind: 'lookup',
                    name: 'departmentId',
                    label: 'แผนก',
                    resource: 'departments',
                    required: true,
                    width: 'lg',
                },
                {
                    kind: 'select',
                    name: 'room',
                    label: 'ห้อง/เวร',
                    required: true,
                    options: DUTY_ROOM_OPTIONS,
                },
                {
                    kind: 'text',
                    name: 'roomOther',
                    label: 'ชื่อห้อง/เวร (กรณีอื่น ๆ)',
                    width: 'lg',
                },
                { kind: 'time', name: 'startTime', label: 'เวลาเริ่มต้น', required: true },
                { kind: 'time', name: 'endTime', label: 'เวลาสิ้นสุด', required: true },
                {
                    kind: 'radio',
                    name: 'payKind',
                    label: 'ประเภทการจ่ายค่าเวร',
                    required: true,
                    options: DUTY_PAY_KIND_OPTIONS,
                    width: 'full',
                },
                STATUS_FIELD,
                REMARK_FIELD,
            ],
        },
    ],
    schema: z
        .object({
        departmentId: z.string().min(1, 'โปรดเลือกแผนก'),
        room: z.string().min(1),
        roomOther: z.string().trim().nullable(),
        startTime: z.string().min(1, 'โปรดระบุเวลาเริ่มต้น'),
        endTime: z.string().min(1, 'โปรดระบุเวลาสิ้นสุด'),
        payKind: z.string().min(1),
        status: z.enum(['ACTIVE', 'INACTIVE']),
        remark: z.string().trim().nullable(),
    })
        .refine((v) => v.room !== 'OTHER' || !!v.roomOther, {
        path: ['roomOther'],
        message: 'โปรดระบุชื่อห้อง/เวร',
    })
        .refine((v) => v.startTime !== v.endTime, {
        path: ['endTime'],
        message: 'เวลาสิ้นสุดต้องไม่เท่ากับเวลาเริ่มต้น',
    }),
    defaultValues: {
        departmentId: '',
        room: 'ROOM1',
        roomOther: null,
        startTime: '08:00',
        endTime: '16:00',
        payKind: 'NORMAL',
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => omitFields(detail, 'id', 'rowVersion'),
    childTables: [daysChild],
};
