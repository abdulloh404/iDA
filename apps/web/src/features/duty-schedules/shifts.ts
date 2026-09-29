import { z } from 'zod';
import { createCrudApi } from '../../api/crud';
import type { ChildTableDef } from '../master-data/descriptor';
export interface DutyShiftRow {
    id: string;
    scheduleId: string;
    shiftDate: string;
    departmentName: string | null;
    clinicName: string | null;
    roomLabel: string;
    payKind: 'NORMAL' | 'ON_TOP' | 'LUMP_SUM' | 'SURPLUS';
    startTime: string;
    endTime: string;
    hourlyAmount: number | null;
    requiredDoctors: number;
    doctorCount: number;
    noExamCount: number;
    completedCount: number;
    workHours: number;
}
export interface DutyShiftDetail extends DutyShiftRow {
    departmentId: string | null;
    clinicId: string | null;
    rowVersion: string;
}
export interface DutyShiftInput {
    scheduleId: string;
    startTime: string;
    endTime: string;
    requiredDoctors: number;
    hourlyAmount: number | null;
}
export const dutyShiftApi = createCrudApi<DutyShiftRow, DutyShiftDetail, DutyShiftInput>('duty-shifts');
export interface DutyShiftDoctorRow {
    id: string;
    shiftId: string;
    doctorCodeId: string;
    doctorCode: string | null;
    doctorName: string | null;
    noExam: boolean;
    workStart: string | null;
    workEnd: string | null;
    workHours: number | null;
    deductAmount: number | null;
    remark: string | null;
}
export interface DutyShiftDoctorDetail extends DutyShiftDoctorRow {
    rowVersion: string;
}
export interface DutyShiftDoctorInput {
    shiftId: string;
    doctorCodeId: string;
    noExam: boolean;
    workStart: string | null;
    workEnd: string | null;
    workHours: number | null;
    deductAmount: number | null;
    remark: string | null;
}
export const dutyShiftDoctorApi = createCrudApi<DutyShiftDoctorRow, DutyShiftDoctorDetail, DutyShiftDoctorInput>('duty-shift-doctors');
export type ShiftState = 'complete' | 'completeWithNoExam' | 'noExamOnly' | 'incomplete';
export function shiftState(shift: DutyShiftRow): ShiftState {
    const complete = shift.completedCount >= shift.requiredDoctors;
    if (!complete)
        return 'incomplete';
    if (shift.noExamCount === 0)
        return 'complete';
    return shift.noExamCount >= shift.doctorCount ? 'noExamOnly' : 'completeWithNoExam';
}
export const SHIFT_STATE_LABELS: Record<ShiftState, string> = {
    complete: 'ลงชื่อครบ',
    completeWithNoExam: 'ลงชื่อครบ มีงดตรวจ',
    noExamOnly: 'งดตรวจทั้งเวร',
    incomplete: 'ยังลงชื่อไม่ครบ',
};
export function clockLabel(iso: string | null): string {
    if (!iso)
        return '—';
    const at = new Date(iso);
    if (Number.isNaN(at.getTime()))
        return iso;
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${pad(at.getHours())}:${pad(at.getMinutes())}`;
}
export const dutyShiftDoctorTable: ChildTableDef<DutyShiftDoctorRow, DutyShiftDoctorInput> = {
    title: 'การลงเวลาแพทย์',
    description: 'งดตรวจให้ติ๊กไว้แล้วไม่ต้องกรอกเวลา ระบบคิดชั่วโมงทำงานให้เป็นศูนย์',
    api: dutyShiftDoctorApi,
    parentKey: 'shiftId',
    addLabel: 'เพิ่มแพทย์เข้าเวร',
    emptyHint: 'ยังไม่มีแพทย์ลงชื่อในเวรนี้',
    columns: [
        { key: 'doctorCode', header: 'รหัสแพทย์', width: '140px' },
        { key: 'doctorName', header: 'แพทย์' },
        {
            key: 'noExam',
            header: 'งดตรวจ',
            width: '110px',
            value: (row) => (row.noExam ? 'งดตรวจ' : '—'),
        },
        { key: 'workStart', header: 'เริ่มงาน', width: '130px', value: (row) => clockLabel(row.workStart) },
        { key: 'workEnd', header: 'เลิกงาน', width: '130px', value: (row) => clockLabel(row.workEnd) },
        { key: 'workHours', header: 'ชั่วโมง', align: 'right', width: '110px' },
        { key: 'deductAmount', header: 'หัก (บาท)', format: 'amount', align: 'right', width: '130px' },
    ],
    fields: [
        {
            kind: 'lookup',
            name: 'doctorCodeId',
            label: 'แพทย์เข้าทำงาน',
            resource: 'doctor-codes',
            required: true,
            width: 'lg',
        },
        { kind: 'checkbox', name: 'noExam', label: 'งดตรวจ' },
        { kind: 'datetime', name: 'workStart', label: 'ช่วงเวลาทำงานเริ่มต้น' },
        { kind: 'datetime', name: 'workEnd', label: 'ช่วงเวลาทำงานสิ้นสุด' },
        {
            kind: 'number',
            name: 'workHours',
            label: 'ชั่วโมงทำงาน',
            hint: 'เว้นว่างไว้ให้ระบบคำนวณจากช่วงเวลา',
        },
        { kind: 'amount', name: 'deductAmount', label: 'หัก (บาท)' },
        { kind: 'text', name: 'remark', label: 'หมายเหตุ', maxLength: 250, width: 'full' },
    ],
    schema: z
        .object({
        shiftId: z.string(),
        doctorCodeId: z.string().min(1, 'โปรดเลือกแพทย์เข้าทำงาน'),
        noExam: z.boolean(),
        workStart: z.string().nullable(),
        workEnd: z.string().nullable(),
        workHours: z.number().nullable(),
        deductAmount: z.number().nullable(),
        remark: z.string().trim().nullable(),
    })
        .refine((v) => !(v.deductAmount && v.deductAmount > 0) || !!v.remark, {
        path: ['remark'],
        message: 'โปรดระบุหมายเหตุเมื่อมีการหักเงิน',
    }),
    defaultValues: {
        shiftId: '',
        doctorCodeId: '',
        noExam: false,
        workStart: null,
        workEnd: null,
        workHours: null,
        deductAmount: null,
        remark: null,
    },
    toInput: (row) => ({
        shiftId: row.shiftId,
        doctorCodeId: row.doctorCodeId,
        noExam: row.noExam,
        workStart: row.workStart,
        workEnd: row.workEnd,
        workHours: row.workHours,
        deductAmount: row.deductAmount,
        remark: row.remark,
    }),
    rowKey: (row) => row.id,
    defaultSort: 'doctorCode',
};
