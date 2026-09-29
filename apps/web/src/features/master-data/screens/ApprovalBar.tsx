import { useState } from 'react';
import { ConfirmDialog } from '../../../components/feedback/ConfirmDialog';
import { Icon } from '../../../components/Icon';
import type { ApprovalAction } from '../descriptor';
export function ApprovalBar({ count, busy, onDecide, }: {
    count: number;
    busy: boolean;
    onDecide: (action: ApprovalAction, comment: string | null) => void;
}) {
    const [asking, setAsking] = useState<ApprovalAction | null>(null);
    const [comment, setComment] = useState('');
    const needsReason = asking === 'REJECT' || asking === 'RETURN';
    const title = asking === 'APPROVE'
        ? `อนุมัติ ${count} รายการ`
        : asking === 'RETURN'
            ? `ส่งกลับ ${count} รายการให้แก้ไข`
            : `ไม่อนุมัติ ${count} รายการ`;
    return (<div className="ida-approval-bar" role="region" aria-label="คำสั่งอนุมัติ">
      <span className="ida-approval-bar__count">
        {count === 0 ? 'เลือกรายการที่ต้องการอนุมัติจากตารางด้านล่าง' : `เลือกไว้ ${count} รายการ`}
      </span>

      <button type="button" className="ida-btn ida-btn--primary ida-btn--sm" disabled={count === 0 || busy} onClick={() => setAsking('APPROVE')}>
        <Icon name="check" size={16}/>
        อนุมัติ
      </button>
      <button type="button" className="ida-btn ida-btn--secondary ida-btn--sm" disabled={count === 0 || busy} onClick={() => setAsking('RETURN')}>
        <Icon name="undo" size={16}/>
        ส่งกลับ
      </button>
      <button type="button" className="ida-btn ida-btn--secondary ida-btn--sm" disabled={count === 0 || busy} onClick={() => setAsking('REJECT')}>
        <Icon name="close" size={16}/>
        ไม่อนุมัติ
      </button>

      <ConfirmDialog open={asking !== null} title={title} confirmLabel={asking === 'APPROVE' ? 'อนุมัติ' : asking === 'RETURN' ? 'ส่งกลับ' : 'ไม่อนุมัติ'} tone={asking === 'APPROVE' ? 'primary' : 'danger'} busy={busy} onCancel={() => {
            setAsking(null);
            setComment('');
        }} onConfirm={() => {
            if (!asking)
                return;
            if (needsReason && comment.trim() === '')
                return;
            onDecide(asking, comment.trim() === '' ? null : comment.trim());
            setAsking(null);
            setComment('');
        }}>
        <div className="ida-field">
          <label className="ida-label" htmlFor="approval-comment">
            {needsReason ? 'เหตุผล' : 'ความเห็น (ถ้ามี)'}
            {needsReason && (<>
                {' '}
                <span className="ida-field__required" aria-hidden="true">
                  *
                </span>
              </>)}
          </label>
          <textarea id="approval-comment" className="ida-input" rows={3} value={comment} onChange={(e) => setComment(e.target.value)} aria-invalid={needsReason && comment.trim() === '' ? true : undefined}/>
          {needsReason && (<p className="ida-field-hint">
              ผู้บันทึกรายการจะเห็นเหตุผลนี้ และใช้มันตัดสินว่าต้องแก้อะไร
            </p>)}
        </div>
      </ConfirmDialog>
    </div>);
}
