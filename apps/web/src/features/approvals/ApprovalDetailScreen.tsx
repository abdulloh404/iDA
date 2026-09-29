import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ApiErrorAlert } from '../../components/feedback/ApiErrorAlert';
import { useToast } from '../../components/feedback/toastContext';
import { StatusBadge } from '../../components/data/StatusBadge';
import { PageHeader } from '../../components/layout/PageHeader';
import { invalidateAfterWrite } from '../../app/queryClient';
import { Icon } from '../../components/Icon';
import { approvalsApi } from './api';
import type { ApprovalStep } from './api';
const ACTION_LABELS: Record<NonNullable<ApprovalStep['action']>, string> = {
    APPROVE: 'อนุมัติ',
    REJECT: 'ไม่อนุมัติ',
    RETURN: 'ส่งกลับให้แก้ไข',
};
export function ApprovalDetailScreen() {
    const { id } = useParams<{
        id: string;
    }>();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const toast = useToast();
    const [comment, setComment] = useState('');
    const [pending, setPending] = useState<NonNullable<ApprovalStep['action']> | null>(null);
    const request = useQuery({
        queryKey: ['approvals', 'detail', id],
        queryFn: ({ signal }) => approvalsApi.get(id!, signal),
        enabled: !!id,
    });
    const decide = useMutation({
        mutationFn: (action: NonNullable<ApprovalStep['action']>) => approvalsApi.decide(id!, action, comment.trim() === '' ? null : comment.trim()),
        onSuccess: (data) => {
            invalidateAfterWrite(queryClient);
            toast.success(`บันทึกการ${ACTION_LABELS[pending ?? 'APPROVE']}เรียบร้อยแล้ว`);
            setPending(null);
            setComment('');
            if (data.status !== 'PENDING')
                navigate('/approvals/pending');
        },
    });
    if (request.isPending) {
        return <span className="ida-skeleton" style={{ height: '320px', display: 'block' }}/>;
    }
    if (request.error)
        return <ApiErrorAlert error={request.error}/>;
    const data = request.data!;
    const fields = readPayload(data.payload);
    return (<>
      <PageHeader title={`คำขอเลขที่ ${data.requestNo}`} description={data.summary} breadcrumbs={[
            { label: 'คำขอและการอนุมัติ' },
            { label: 'รายการรอดำเนินการ', to: '/approvals/pending' },
        ]} actions={<StatusBadge status={data.status}/>}/>

      <section className="ida-card">
        <h2 className="ida-card__title">ข้อมูลคำขอ</h2>
        <dl className="ida-detail-grid">
          <Detail label="ประเภทคำขอ" value={data.requestTypeNameTh}/>
          <Detail label="เจ้าหน้าที่ผู้ส่ง" value={data.requestedBy}/>
          <Detail label="วันที่ส่งคำขอ" value={formatDateTime(data.requestedAt)}/>
          <Detail label="แพทย์ที่เกี่ยวข้อง" value={data.doctorName ?? '—'}/>
          <Detail label="วันที่ปิดคำขอ" value={data.closedAt ? formatDateTime(data.closedAt) : 'ยังไม่ปิด'}/>
        </dl>
      </section>

      <section className="ida-card">
        <h2 className="ida-card__title">ค่าที่ขอแก้</h2>
        {fields.length === 0 ? (<p className="ida-text-secondary">คำขอนี้ไม่มีรายละเอียดที่บันทึกไว้</p>) : (<dl className="ida-detail-grid">
            {fields.map(([key, value]) => (<Detail key={key} label={key} value={value}/>))}
          </dl>)}
      </section>

      <section className="ida-card">
        <h2 className="ida-card__title">เส้นทางการอนุมัติ</h2>
        <ol className="ida-steps">
          {data.steps.map((step) => (<li key={step.stepSeq} className={`ida-steps__item${step.action ? ' ida-steps__item--done' : ''}`}>
              <span className="ida-steps__marker" aria-hidden="true">
                <Icon name={step.action === 'APPROVE' ? 'check' : step.action ? 'close' : 'clock'} size={14}/>
              </span>
              <div>
                <p className="ida-steps__title">
                  ขั้นที่ {step.stepSeq} · {step.approverRoleNameTh}
                </p>
                <p className="ida-caption ida-text-secondary">
                  {step.action
                ? `${ACTION_LABELS[step.action]} โดย ${step.approverUser ?? '—'} · ${formatDateTime(step.actionAt!)}`
                : 'รอการตัดสิน'}
                </p>
                {step.comment && <p className="ida-steps__comment">{step.comment}</p>}
              </div>
            </li>))}
        </ol>
      </section>

      {data.canDecide && (<section className="ida-card">
          <h2 className="ida-card__title">ตัดสินคำขอ</h2>

          {decide.error != null && <ApiErrorAlert error={decide.error}/>}

          <div className="ida-field">
            <label className="ida-label" htmlFor="approval-comment">
              ความเห็น
            </label>
            <textarea id="approval-comment" className="ida-input" rows={3} value={comment} onChange={(e) => setComment(e.target.value)}/>
            
            <p className="ida-field-hint">
              การส่งกลับและการไม่อนุมัติต้องระบุเหตุผล ผู้ส่งคำขอจะเห็นข้อความนี้
            </p>
          </div>

          <div className="ida-form-actions ida-form-actions--inline">
            <button type="button" className="ida-btn ida-btn--secondary" onClick={() => {
                setPending('RETURN');
                decide.mutate('RETURN');
            }} disabled={decide.isPending}>
              <Icon name="undo" size={18}/>
              ส่งกลับให้แก้ไข
            </button>
            <button type="button" className="ida-btn ida-btn--danger" onClick={() => {
                setPending('REJECT');
                decide.mutate('REJECT');
            }} disabled={decide.isPending}>
              <Icon name="close" size={18}/>
              ไม่อนุมัติ
            </button>
            <button type="button" className="ida-btn ida-btn--primary" onClick={() => {
                setPending('APPROVE');
                decide.mutate('APPROVE');
            }} disabled={decide.isPending}>
              {decide.isPending ? (<span className="ida-spinner" aria-hidden="true"/>) : (<Icon name="check" size={18}/>)}
              อนุมัติ
            </button>
          </div>
        </section>)}

      <p>
        <Link className="ida-btn ida-btn--ghost" to="/approvals/mine">
          <Icon name="undo" size={18}/>
          กลับไปที่รายการคำขอ
        </Link>
      </p>
    </>);
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
function readPayload(payload: string): [
    string,
    string
][] {
    try {
        const parsed: unknown = JSON.parse(payload || '{}');
        if (!parsed || typeof parsed !== 'object')
            return [];
        return Object.entries(parsed as Record<string, unknown>).map(([key, value]) => [
            key,
            value === null || value === undefined ? '—' : String(value),
        ]);
    }
    catch {
        return [];
    }
}
function formatDateTime(iso: string): string {
    const date = new Date(iso);
    if (Number.isNaN(date.getTime()))
        return iso;
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}/${date.getFullYear()} ${pad(date.getHours())}:${pad(date.getMinutes())}`;
}
