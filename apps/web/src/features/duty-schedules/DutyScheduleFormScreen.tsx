import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { PageHeader } from '../../components/layout/PageHeader';
import { ApiErrorAlert } from '../../components/feedback/ApiErrorAlert';
import { ConfirmDialog } from '../../components/feedback/ConfirmDialog';
import { useToast } from '../../components/feedback/toastContext';
import { Icon } from '../../components/Icon';
import { Select } from '../../components/form/Select';
import { invalidateAfterWrite } from '../../app/queryClient';
import { usePermission } from '../auth/authState';
import { ChildTablePanel } from '../master-data/screens/ChildTablePanel';
import type { AnyScreenDescriptor, FormMode } from '../master-data/descriptor';
import { DUTY_SCHEDULE_STATUS_LABELS, dutyScheduleApi, generateDutySchedule, monthLabel, rejectDutySchedule, selectableYears, submitDutySchedule, } from './schedules';
import type { DutyScheduleDetail, DutyScheduleKind, DutyScheduleStatus } from './schedules';
import { SHIFT_STATE_LABELS, dutyShiftApi, dutyShiftDoctorTable, shiftState, } from './shifts';
import type { DutyShiftRow } from './shifts';
export function DutyScheduleFormScreen({ descriptor, mode, }: {
    descriptor: AnyScreenDescriptor;
    mode: FormMode;
}) {
    return mode === 'create' ? (<CreateSchedule descriptor={descriptor}/>) : (<ScheduleCalendar descriptor={descriptor} mode={mode}/>);
}
function CreateSchedule({ descriptor }: {
    descriptor: AnyScreenDescriptor;
}) {
    const navigate = useNavigate();
    const toast = useToast();
    const queryClient = useQueryClient();
    const today = new Date();
    const [year, setYear] = useState(String(today.getFullYear()));
    const [month, setMonth] = useState(String(today.getMonth() + 1));
    const kind = (descriptor.fixedFilters?.kind ?? 'DUTY') as DutyScheduleKind;
    const generate = useMutation({
        mutationFn: () => generateDutySchedule(kind, Number(year), Number(month)),
        onSuccess: (schedule) => {
            invalidateAfterWrite(queryClient);
            toast.success(`สร้างตารางเวรเดือน${monthLabel(schedule.month)} ${schedule.year} แล้ว`);
            navigate(`${descriptor.path}/${schedule.id}`);
        },
    });
    return (<>
      <PageHeader title={`สร้าง${descriptor.titleTh}`} breadcrumbs={[...descriptor.breadcrumb, { label: descriptor.titleTh, to: descriptor.path }]}/>

      <section className="ida-form-section">
        {generate.error != null && (<div style={{ marginBottom: 'var(--ida-space-5)' }}>
            <ApiErrorAlert error={generate.error}/>
          </div>)}

        <div className="ida-form-grid">
          <div className="ida-field ida-field--sm">
            <label className="ida-label" htmlFor="schedule-month">
              เดือน <span className="ida-field__required" aria-hidden="true">*</span>
            </label>
            <Select id="schedule-month" value={month} onChange={setMonth} options={Array.from({ length: 12 }, (_, i) => ({
            value: String(i + 1),
            label: monthLabel(i + 1),
        }))}/>
          </div>

          <div className="ida-field ida-field--sm">
            <label className="ida-label" htmlFor="schedule-year">
              ปี (ค.ศ.) <span className="ida-field__required" aria-hidden="true">*</span>
            </label>
            <Select id="schedule-year" value={year} onChange={setYear} options={selectableYears(today).map((y) => ({ value: String(y), label: String(y) }))}/>
          </div>
        </div>

        <p className="ida-field-hint" style={{ marginTop: 'var(--ida-space-4)' }}>
          ระบบจะดึงเวรของทั้งเดือนมาจากอัตราที่ใช้งานอยู่ให้เอง จากนั้นจึงลงเวลาทำงานรายเวรได้
        </p>
      </section>

      <div className="ida-form-actions">
        <Link className="ida-btn ida-btn--secondary" to={descriptor.path}>
          ยกเลิก
        </Link>
        <button type="button" className="ida-btn ida-btn--primary" onClick={() => generate.mutate()} disabled={generate.isPending}>
          {generate.isPending ? (<span className="ida-spinner" aria-hidden="true"/>) : (<Icon name="check" size={18}/>)}
          สร้างตารางเวร
        </button>
      </div>
    </>);
}
function ScheduleCalendar({ descriptor, mode, }: {
    descriptor: AnyScreenDescriptor;
    mode: FormMode;
}) {
    const { id } = useParams<{
        id: string;
    }>();
    const toast = useToast();
    const queryClient = useQueryClient();
    const canSubmit = usePermission('duty-schedules.submit');
    const canReject = usePermission('duty-schedules.reject');
    const canWrite = usePermission('duty-schedules.write');
    const [selected, setSelected] = useState<DutyShiftRow | null>(null);
    const [rejecting, setRejecting] = useState(false);
    const [reason, setReason] = useState('');
    const schedule = useQuery({
        queryKey: ['master', 'duty-schedules', id],
        queryFn: ({ signal }) => dutyScheduleApi.get(id!, signal),
        enabled: !!id,
    });
    const shifts = useQuery({
        queryKey: ['master', 'duty-shifts', 'list', { scheduleId: id }],
        queryFn: ({ signal }) => dutyShiftApi.list({ scheduleId: id, pageSize: 500 }, signal),
        enabled: !!id,
    });
    const submit = useMutation({
        mutationFn: () => submitDutySchedule(id!),
        onSuccess: () => {
            invalidateAfterWrite(queryClient);
            toast.success('ส่งตารางเวรให้ฝ่ายบัญชีแล้ว');
        },
        onError: (error) => toast.error(error instanceof Error ? error.message : 'ส่งตารางเวรไม่สำเร็จ'),
    });
    const reject = useMutation({
        mutationFn: () => rejectDutySchedule(id!, reason),
        onSuccess: () => {
            invalidateAfterWrite(queryClient);
            toast.success('ตีกลับตารางเวรแล้ว');
            setRejecting(false);
            setReason('');
        },
        onError: (error) => toast.error(error instanceof Error ? error.message : 'ตีกลับตารางเวรไม่สำเร็จ'),
    });
    if (schedule.isPending) {
        return <span className="ida-skeleton" style={{ height: '320px', display: 'block' }}/>;
    }
    if (schedule.error)
        return <ApiErrorAlert error={schedule.error}/>;
    const detail = schedule.data as DutyScheduleDetail;
    const rows = shifts.data?.items ?? [];
    const locked = detail.status === 'CALCULATED' || mode === 'view' || !canWrite;
    const incomplete = rows.filter((row) => shiftState(row) === 'incomplete').length;
    return (<>
      <PageHeader title={`${descriptor.titleTh} เดือน${monthLabel(detail.month)} ${detail.year}`} breadcrumbs={[...descriptor.breadcrumb, { label: descriptor.titleTh, to: descriptor.path }]} actions={<>
            <ScheduleStatusBadge status={detail.status}/>
            {canSubmit && (detail.status === 'DRAFT' || detail.status === 'REJECTED') && (<button type="button" className="ida-btn ida-btn--primary" onClick={() => submit.mutate()} disabled={submit.isPending || rows.length === 0}>
                <Icon name="check" size={18}/>
                ส่งให้บัญชี
              </button>)}
            {canReject && detail.status === 'SUBMITTED' && (<button type="button" className="ida-btn ida-btn--secondary" onClick={() => setRejecting(true)}>
                <Icon name="undo" size={18}/>
                ตีกลับ
              </button>)}
          </>}/>

      {detail.status === 'REJECTED' && detail.rejectReason && (<div className="ida-alert ida-alert--warning" role="alert">
          <div className="ida-alert__body">
            <Icon name="alert" size={18}/>
            <span>
              ฝ่ายบัญชีตีกลับให้แก้: {detail.rejectReason}
              {detail.rejectedBy && ` (โดย ${detail.rejectedBy})`}
            </span>
          </div>
        </div>)}

      {detail.status === 'CALCULATED' && (<div className="ida-alert ida-alert--info" role="status">
          <div className="ida-alert__body">
            <Icon name="info" size={18}/>
            <span>ตารางเวรนี้ถูกคำนวณรายเดือนแล้ว แก้ไขไม่ได้ หากต้องการแก้ต้องถอนการคำนวณก่อน</span>
          </div>
        </div>)}

      <section className="ida-card">
        <h2 className="ida-card__title">ปฏิทินเวร</h2>

        {shifts.error != null && <ApiErrorAlert error={shifts.error}/>}

        {rows.length === 0 ? (<p className="ida-text-secondary">
            เดือนนี้ยังไม่มีเวร — ตรวจสอบว่าตั้งอัตราค่าเวรของแผนกไว้แล้วหรือยัง แล้วสร้างตารางใหม่
          </p>) : (<>
            <CalendarGrid year={detail.year} month={detail.month} shifts={rows} selectedId={selected?.id ?? null} onSelect={setSelected}/>
            <p className="ida-field-hint" style={{ marginTop: 'var(--ida-space-4)' }}>
              {incomplete === 0
                ? 'ลงเวลาครบทุกเวรแล้ว'
                : `ยังมี ${incomplete} เวรที่ลงเวลาไม่ครบ — ส่งให้บัญชีได้เมื่อครบทุกเวร`}
            </p>
          </>)}
      </section>

      {selected && (<>
          <section className="ida-card">
            <h2 className="ida-card__title">
              เวรวันที่ {new Date(selected.shiftDate).toLocaleDateString('th-TH')} ·{' '}
              {selected.roomLabel}
            </h2>
            <dl className="ida-detail-grid">
              <Detail label="แผนก" value={selected.departmentName ?? '—'}/>
              <Detail label="คลินิก" value={selected.clinicName ?? '—'}/>
              <Detail label="ช่วงเวลาเวร" value={`${selected.startTime.slice(0, 5)}–${selected.endTime.slice(0, 5)}`}/>
              <Detail label="รายได้ (บาท/ชั่วโมง)" value={selected.hourlyAmount == null ? '—' : String(selected.hourlyAmount)}/>
              <Detail label="จำนวนแพทย์ที่ต้องมี" value={String(selected.requiredDoctors)}/>
              <Detail label="สถานะเวร" value={SHIFT_STATE_LABELS[shiftState(selected)]}/>
            </dl>
          </section>

          <ChildTablePanel key={selected.id} child={dutyShiftDoctorTable} parentId={selected.id} readOnly={locked}/>
        </>)}

      <ConfirmDialog open={rejecting} title="ตีกลับตารางเวร" confirmLabel="ตีกลับ" tone="danger" busy={reject.isPending} onCancel={() => setRejecting(false)} onConfirm={() => reject.mutate()}>
        <div className="ida-field">
          <label className="ida-label" htmlFor="reject-reason">
            เหตุผล <span className="ida-field__required" aria-hidden="true">*</span>
          </label>
          
          <textarea id="reject-reason" className="ida-input" rows={3} value={reason} onChange={(e) => setReason(e.target.value)}/>
        </div>
      </ConfirmDialog>
    </>);
}
function CalendarGrid({ year, month, shifts, selectedId, onSelect, }: {
    year: number;
    month: number;
    shifts: readonly DutyShiftRow[];
    selectedId: string | null;
    onSelect: (shift: DutyShiftRow) => void;
}) {
    const byDate = new Map<string, DutyShiftRow[]>();
    for (const shift of shifts) {
        const key = shift.shiftDate.slice(0, 10);
        byDate.set(key, [...(byDate.get(key) ?? []), shift]);
    }
    const first = new Date(year, month - 1, 1);
    const daysInMonth = new Date(year, month, 0).getDate();
    const leading = first.getDay();
    const cells: (number | null)[] = [
        ...Array.from({ length: leading }, () => null),
        ...Array.from({ length: daysInMonth }, (_, i) => i + 1),
    ];
    return (<div className="ida-duty-calendar" role="grid" aria-label="ปฏิทินเวร">
      {['อา', 'จ', 'อ', 'พ', 'พฤ', 'ศ', 'ส'].map((day) => (<div key={day} className="ida-duty-calendar__head" role="columnheader">
          {day}
        </div>))}

      {cells.map((day, index) => {
            if (day === null) {
                return <div key={`pad-${index}`} className="ida-duty-calendar__pad" aria-hidden="true"/>;
            }
            const key = `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
            const dayShifts = byDate.get(key) ?? [];
            return (<div key={key} className="ida-duty-calendar__day" role="gridcell">
            <span className="ida-duty-calendar__date">{day}</span>
            {dayShifts.map((shift) => {
                    const state = shiftState(shift);
                    return (<button key={shift.id} type="button" className={`ida-duty-shift ida-duty-shift--${state}` +
                            (shift.id === selectedId ? ' ida-duty-shift--selected' : '')} onClick={() => onSelect(shift)} title={`${shift.roomLabel} · ${SHIFT_STATE_LABELS[state]}`}>
                  <span className="ida-duty-shift__label">{shift.roomLabel}</span>
                  <span className="ida-duty-shift__hours">({shift.workHours})</span>
                  <span className="ida-visually-hidden">{SHIFT_STATE_LABELS[state]}</span>
                </button>);
                })}
          </div>);
        })}
    </div>);
}
function ScheduleStatusBadge({ status }: {
    status: DutyScheduleStatus;
}) {
    const variant: Record<DutyScheduleStatus, {
        className: string;
        icon: 'check' | 'clock' | 'pencil' | 'undo';
    }> = {
        DRAFT: { className: 'ida-badge--info', icon: 'pencil' },
        SUBMITTED: { className: 'ida-badge--pending', icon: 'clock' },
        CALCULATED: { className: 'ida-badge--success', icon: 'check' },
        REJECTED: { className: 'ida-badge--error', icon: 'undo' },
    };
    const badge = variant[status];
    return (<span className={`ida-badge ${badge.className}`} data-status={status}>
      <Icon name={badge.icon} size={14}/>
      {DUTY_SCHEDULE_STATUS_LABELS[status]}
    </span>);
}
function Detail({ label, value }: {
    label: string;
    value: string;
}) {
    return (<div className="ida-detail-grid__item">
      <dt className="ida-label">{label}</dt>
      <dd className="ida-detail-grid__value">{value}</dd>
    </div>);
}
