import { useQuery } from '@tanstack/react-query';
import type { AuditEntry } from '../../../api/types';
import { ApiErrorAlert } from '../../../components/feedback/ApiErrorAlert';
import type { AnyScreenDescriptor } from '../descriptor';
const ACTION_LABELS: Record<AuditEntry['action'], string> = {
    INSERT: 'สร้างข้อมูล',
    UPDATE: 'แก้ไขข้อมูล',
    DELETE: 'ลบข้อมูล',
};
const ACTION_BADGES: Record<AuditEntry['action'], string> = {
    INSERT: 'ida-badge--success',
    UPDATE: 'ida-badge--info',
    DELETE: 'ida-badge--error',
};
export function HistoryPanel({ descriptor, id, }: {
    descriptor: AnyScreenDescriptor;
    id: string;
}) {
    const history = useQuery({
        queryKey: ['master', descriptor.resource, id, 'history'],
        queryFn: ({ signal }) => descriptor.api.history(id, signal),
    });
    return (<section className="ida-card">
      <h2 className="ida-card__title">ประวัติการดำเนินการ</h2>

      {history.error && <ApiErrorAlert error={history.error}/>}

      {history.isPending && (<span className="ida-skeleton" style={{ height: '64px', display: 'block' }}/>)}

      {history.data?.length === 0 && (<p className="ida-text-secondary">ยังไม่มีประวัติการแก้ไข</p>)}

      {history.data?.map((entry) => (<div className="ida-history__entry" key={entry.id}>
          <div className="ida-history__meta">
            <span className={`ida-badge ${ACTION_BADGES[entry.action]}`}>
              {ACTION_LABELS[entry.action]}
            </span>
            <strong>{entry.changedBy}</strong>
            <span className="ida-caption ida-text-secondary">
              {formatDateTime(entry.changedAt)}
            </span>
          </div>

          {entry.changes.length > 0 && (<ul style={{ margin: 0, paddingInlineStart: 'var(--ida-space-5)' }}>
              {entry.changes.map((change) => (<li className="ida-history__change" key={change.field}>
                  {change.field}:{' '}
                  {change.oldValue != null && (<>
                      <span className="ida-history__old">{change.oldValue}</span>{' '}
                      <span aria-hidden="true">→</span>{' '}
                    </>)}
                  <span>{change.newValue ?? '—'}</span>
                </li>))}
            </ul>)}
        </div>))}
    </section>);
}
function formatDateTime(iso: string): string {
    const date = new Date(iso);
    if (Number.isNaN(date.getTime()))
        return iso;
    const pad = (n: number) => String(n).padStart(2, '0');
    return (`${pad(date.getDate())}/${pad(date.getMonth() + 1)}/${date.getFullYear()} ` +
        `${pad(date.getHours())}:${pad(date.getMinutes())}`);
}
