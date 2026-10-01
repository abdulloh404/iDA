import { useQuery } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import { DataTable } from '../../components/data/DataTable';
import { StatusBadge } from '../../components/data/StatusBadge';
import { ApiErrorAlert } from '../../components/feedback/ApiErrorAlert';
import { Icon } from '../../components/Icon';
import { PageHeader } from '../../components/layout/PageHeader';
import { useAuth } from '../auth/authState';
import { ingestApi } from './api';
import { formatDate, formatDateTime, formatInt, shortId } from './format';
import { IngestPermissionState } from './IngestPermissionState';
export function IngestBatchDetailScreen() {
    const { batchId } = useParams<{
        batchId: string;
    }>();
    const { can } = useAuth();
    const canRead = can('ingest.read');
    const batch = useQuery({
        queryKey: ['ingest', 'batch', batchId],
        queryFn: ({ signal }) => ingestApi.getBatch(batchId!, signal),
        enabled: !!batchId && canRead,
        refetchInterval: (query) => query.state.data?.batch.status === 'Running' || query.state.data?.runs.some((run) => run.status === 'Running') ? 3000 : false,
    });
    if (!canRead)
        return <IngestPermissionState detail/>;
    if (batch.isPending) {
        return <span className="ida-skeleton" style={{ height: '320px', display: 'block' }}/>;
    }
    if (batch.error)
        return <ApiErrorAlert error={batch.error}/>;
    const data = batch.data!;
    return (<>
      <PageHeader title={`Batch ${shortId(data.batch.id)}`} description={`${sourceLabel(data.batch.sourceFilter)} · วันที่ข้อมูล ${formatDate(data.batch.businessDate)} · ${data.batch.sourceMode}`} breadcrumbs={[
            { label: 'การนำเข้าข้อมูล', to: '/ingest/batches' },
            { label: `Batch ${shortId(data.batch.id)}` },
        ]} actions={<StatusBadge status={data.batch.status}/>}/>

      {data.batch.errorMessage && (<div className="ida-alert ida-alert--error">
          <Icon name="alert" size={20}/>
          <div>{data.batch.errorMessage}</div>
        </div>)}

      <section className="ida-card">
        <h2 className="ida-card__title">ข้อมูล batch</h2>
        <dl className="ida-detail-grid">
          <Detail label="เริ่ม" value={formatDateTime(data.batch.startedAt)}/>
          <Detail label="จบ" value={formatDateTime(data.batch.finishedAt)}/>
          <Detail label="ผู้สั่ง" value={data.batch.triggeredBy ?? data.batch.triggerKind ?? '—'}/>
          <Detail label="Dataset-run" value={formatInt(data.batch.datasetRunCount)}/>
          <Detail label="Run ล้มเหลว" value={formatInt(data.batch.failedRunCount)}/>
          <Detail label="Run ถอนแล้ว" value={formatInt(data.batch.withdrawnRunCount)}/>
          <Detail label="รับเข้า" value={formatInt(data.batch.receivedCount)}/>
          <Detail label="เปลี่ยน" value={formatInt(data.batch.changedCount)}/>
          <Detail label="ซ้ำ" value={formatInt(data.batch.duplicateCount)}/>
          <Detail label="รอตรวจ" value={formatInt(data.batch.pendingCount)}/>
          <Detail label="ไม่ผ่าน" value={formatInt(data.batch.rejectedCount)}/>
          <Detail label="Raw coverage" value={`${formatInt(data.batch.rawBodyCount)}/${formatInt(data.batch.rawPageCount)}`}/>
        </dl>
      </section>

      <div className="ida-table-card">
        <DataTable caption="Run ใน batch" columns={[
            {
                key: 'datasetCode',
                header: 'Dataset',
                width: '230px',
                render: (row) => (<div>
                  <strong>{row.datasetName ?? row.datasetCode}</strong>
                  <div className="ida-caption ida-text-secondary">{row.datasetCode}</div>
                </div>),
            },
            { key: 'status', header: 'สถานะ', format: 'status', width: '140px' },
            {
                key: 'receivedCount',
                header: 'รับเข้า',
                align: 'right',
                width: '90px',
                value: (row) => row.receivedCount,
            },
            {
                key: 'stagedCount',
                header: 'Staging',
                align: 'right',
                width: '90px',
                value: (row) => row.stagedCount,
            },
            {
                key: 'changedCount',
                header: 'เปลี่ยน',
                align: 'right',
                width: '90px',
                value: (row) => row.changedCount,
            },
            {
                key: 'duplicateCount',
                header: 'ซ้ำ',
                align: 'right',
                width: '90px',
                value: (row) => row.duplicateCount,
            },
            {
                key: 'issueCount',
                header: 'Issue',
                align: 'right',
                width: '90px',
                value: (row) => row.issueCount,
            },
            {
                key: 'rawBodyCount',
                header: 'Raw',
                align: 'right',
                width: '90px',
                render: (row) => `${formatInt(row.rawBodyCount)}/${formatInt(row.rawPageCount)}`,
            },
            {
                key: 'reconciliationStatus',
                header: 'กระทบยอด',
                width: '130px',
                value: (row) => row.reconciliationStatus ?? 'ยังไม่มี',
            },
            {
                key: 'startedAt',
                header: 'เวลาเริ่ม',
                width: '150px',
                render: (row) => formatDateTime(row.startedAt),
            },
        ]} rows={data.runs} rowKey={(row) => row.id} empty={{
            icon: 'database',
            title: 'Batch นี้ยังไม่มี run',
            hint: 'ถ้า batch เพิ่งเริ่ม อาจต้องรีเฟรชอีกครั้ง',
        }} rowActions={[
            { icon: 'eye', label: 'ดูรายละเอียด run', to: (row) => `/ingest/runs/${row.id}` },
        ]}/>
      </div>

      <p>
        <Link className="ida-btn ida-btn--ghost" to="/ingest/batches">
          <Icon name="undo" size={18}/>
          กลับประวัติการนำเข้า
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
function sourceLabel(source: string): string {
    return source === 'all'
        ? 'HIS + Oracle AR'
        : source === 'his'
            ? 'HIS เท่านั้น'
            : source === 'oracle'
                ? 'Oracle AR เท่านั้น'
                : source;
}
