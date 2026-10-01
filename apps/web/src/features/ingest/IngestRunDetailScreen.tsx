import { useQuery } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import { DataTable } from '../../components/data/DataTable';
import { StatusBadge } from '../../components/data/StatusBadge';
import { ApiErrorAlert } from '../../components/feedback/ApiErrorAlert';
import { Icon } from '../../components/Icon';
import { PageHeader } from '../../components/layout/PageHeader';
import { useAuth } from '../auth/authState';
import { ingestApi } from './api';
import { formatDate, formatDateTime, formatInt, prettyJson, shortId } from './format';
import { IngestPermissionState } from './IngestPermissionState';
import { useState } from 'react';
export function IngestRunDetailScreen() {
    const { runId } = useParams<{
        runId: string;
    }>();
    const { can } = useAuth();
    const canRead = can('ingest.read');
    const [rawPage, setRawPage] = useState<number | null>(null);
    const run = useQuery({
        queryKey: ['ingest', 'run', runId],
        queryFn: ({ signal }) => ingestApi.getRun(runId!, signal),
        enabled: !!runId && canRead,
        refetchInterval: (query) => query.state.data?.run.status === 'Running' ? 3000 : false,
    });
    const raw = useQuery({
        queryKey: ['ingest', 'run', runId, 'raw', rawPage],
        queryFn: ({ signal }) => ingestApi.getRawPage(runId!, rawPage!, signal),
        enabled: !!runId && rawPage !== null && canRead && can('ingest.raw.read'),
    });
    if (!canRead)
        return <IngestPermissionState detail/>;
    if (run.isPending) {
        return <span className="ida-skeleton" style={{ height: '320px', display: 'block' }}/>;
    }
    if (run.error)
        return <ApiErrorAlert error={run.error}/>;
    const data = run.data!;
    return (<>
      <PageHeader title={data.run.datasetName ?? data.run.datasetCode} description={`Run ${shortId(data.run.id)} · วันที่ข้อมูล ${formatDate(data.run.businessDate)} · ${data.run.sourceMode}`} breadcrumbs={[
            { label: 'การนำเข้าข้อมูล', to: '/ingest/batches' },
            ...(data.run.batchId
                ? [{ label: `Batch ${shortId(data.run.batchId)}`, to: `/ingest/batches/${data.run.batchId}` }]
                : [{ label: 'Legacy run' }]),
            { label: shortId(data.run.id) },
        ]} actions={<StatusBadge status={data.run.status}/>}/>

      {data.run.errorMessage && (<div className="ida-alert ida-alert--error">
          <Icon name="alert" size={20}/>
          <div>{data.run.errorMessage}</div>
        </div>)}

      {!data.run.batchId && (<div className="ida-alert ida-alert--warning">
          <Icon name="alert" size={20}/>
          <div>Run นี้เป็นข้อมูลเก่าที่ไม่รู้ขอบเขตการสั่ง batch เดิม</div>
        </div>)}

      <section className="ida-card">
        <h2 className="ida-card__title">สรุป run</h2>
        <dl className="ida-detail-grid">
          <Detail label="Dataset code" value={data.run.datasetCode}/>
          <Detail label="Fixture" value={data.run.fixtureVersion}/>
          <Detail label="เริ่ม" value={formatDateTime(data.run.startedAt)}/>
          <Detail label="จบ" value={formatDateTime(data.run.finishedAt)}/>
          <Detail label="รับเข้า" value={formatInt(data.run.receivedCount)}/>
          <Detail label="Staging" value={formatInt(data.run.stagedCount)}/>
          <Detail label="เปลี่ยน" value={formatInt(data.run.changedCount)}/>
          <Detail label="ซ้ำ" value={formatInt(data.run.duplicateCount)}/>
          <Detail label="รอตรวจ" value={formatInt(data.run.pendingCount)}/>
          <Detail label="ไม่ผ่าน" value={formatInt(data.run.rejectedCount)}/>
          <Detail label="Raw coverage" value={`${formatInt(data.run.rawBodyCount)}/${formatInt(data.run.rawPageCount)}`}/>
          <Detail label="Issue" value={formatInt(data.run.issueCount)}/>
        </dl>
      </section>

      <section className="ida-card">
        <h2 className="ida-card__title">กระทบยอดระดับ ingest</h2>
        {data.reconciliation ? (<dl className="ida-detail-grid">
            <Detail label="สถานะ" value={data.reconciliation.status}/>
            <Detail label="รับเข้า" value={formatInt(data.reconciliation.receivedCount)}/>
            <Detail label="เปลี่ยน" value={formatInt(data.reconciliation.changedCount)}/>
            <Detail label="ซ้ำ" value={formatInt(data.reconciliation.duplicateCount)}/>
            <Detail label="Source total" value={money(data.reconciliation.sourceTotal)}/>
            <Detail label="Loaded total" value={money(data.reconciliation.loadedTotal)}/>
            <Detail label="Diff" value={money(data.reconciliation.difference)}/>
          </dl>) : (<p className="ida-text-secondary">ยังไม่มีกระทบยอดสำหรับ run นี้</p>)}
      </section>

      <div className="ida-table-card">
        {data.stagingItemCount > data.stagingItems.length && (<div className="ida-alert ida-alert--info">
            <Icon name="info" size={20}/>
            <div>
              แสดงรายการ staging {formatInt(data.stagingItems.length)} รายการแรก จากทั้งหมด{' '}
              {formatInt(data.stagingItemCount)} รายการ
            </div>
          </div>)}
        <DataTable caption="รายการ staging" columns={[
            { key: 'pageNumber', header: 'Page', align: 'right', width: '80px' },
            { key: 'itemIndex', header: 'Index', align: 'right', width: '80px' },
            { key: 'sourceKeyCandidate', header: 'Source key' },
            { key: 'validationStatus', header: 'Validation', width: '130px' },
            { key: 'disposition', header: 'Disposition', width: '130px' },
            {
                key: 'targetRecordId',
                header: 'Target',
                width: '110px',
                value: (row) => (row.targetRecordId ? shortId(row.targetRecordId) : '—'),
            },
        ]} rows={data.stagingItems} rowKey={(row) => row.id} empty={{
            icon: 'inbox',
            title: 'ไม่มี staging item',
            hint: 'Run เก่าบางรายการไม่มี staging ย้อนหลัง',
        }}/>
      </div>

      <div className="ida-table-card">
        <DataTable caption="Issue" columns={[
            { key: 'issueKind', header: 'ชนิด', width: '110px' },
            { key: 'issueCode', header: 'Code', width: '170px' },
            { key: 'fieldName', header: 'Field', width: '150px' },
            { key: 'message', header: 'ข้อความ' },
            {
                key: 'occurredAt',
                header: 'เวลา',
                width: '150px',
                render: (row) => formatDateTime(row.occurredAt),
            },
        ]} rows={data.issues} rowKey={(row) => row.id} empty={{
            icon: 'check',
            title: 'ไม่มี issue ใน run นี้',
            hint: 'ไม่มีข้อผิดพลาดที่ worker mock บันทึกไว้',
        }}/>
      </div>

      <div className="ida-table-card">
        <DataTable caption="ประวัติสถานะ" columns={[
            { key: 'status', header: 'สถานะ', format: 'status', width: '140px' },
            {
                key: 'occurredAt',
                header: 'เวลา',
                width: '150px',
                render: (row) => formatDateTime(row.occurredAt),
            },
            { key: 'detail', header: 'รายละเอียด' },
        ]} rows={data.events} rowKey={(row) => row.id} empty={{
            icon: 'clock',
            title: 'ไม่มี event ย้อนหลัง',
            hint: 'Run เก่าก่อนเพิ่ม event อาจไม่มี timeline ครบ',
        }}/>
      </div>

      <div className="ida-table-card">
        <DataTable caption="ประวัติการควบคุม" columns={[
            {
                key: 'action',
                header: 'คำสั่ง',
                width: '130px',
                value: (row) => actionLabel(row.action),
            },
            { key: 'actor', header: 'ผู้ดำเนินการ', width: '160px' },
            { key: 'reason', header: 'เหตุผล' },
            {
                key: 'actedAt',
                header: 'เวลา',
                width: '150px',
                render: (row) => formatDateTime(row.actedAt),
            },
        ]} rows={data.controlActions} rowKey={(row) => row.id} empty={{
            icon: 'undo',
            title: 'ยังไม่มีคำสั่งควบคุม',
            hint: 'เมื่อมีการถอนผลหรือคำสั่งควบคุมอื่น ระบบจะแสดง audit trail ที่นี่',
        }}/>
      </div>

      <div className="ida-table-card">
        <DataTable caption="Raw page" columns={[
            { key: 'pageNumber', header: 'Page', align: 'right', width: '80px' },
            {
                key: 'receivedAt',
                header: 'รับเมื่อ',
                width: '150px',
                render: (row) => formatDateTime(row.receivedAt),
            },
            {
                key: 'rawAvailable',
                header: 'Raw body',
                width: '110px',
                value: (row) => (row.rawAvailable ? 'มี raw' : 'ไม่มี raw'),
            },
            {
                key: 'payloadAvailable',
                header: 'Payload',
                width: '110px',
                value: (row) => (row.payloadAvailable ? 'มี payload' : 'ไม่มี payload'),
            },
            { key: 'rawSha256', header: 'SHA-256' },
        ]} rows={data.rawPages} rowKey={(row) => String(row.pageNumber)} empty={{
            icon: 'fileText',
            title: 'ไม่มี raw page',
            hint: 'ไม่มีหลักฐาน raw ต้นฉบับในรอบเก่า',
        }} rowActions={[
            {
                icon: 'eye',
                label: 'เปิด raw body',
                onClick: (row) => setRawPage(row.pageNumber),
                hidden: !can('ingest.raw.read'),
            },
        ]}/>
      </div>

      {rawPage !== null && (<section className="ida-card">
          <h2 className="ida-card__title">Raw page {rawPage}</h2>
          {!can('ingest.raw.read') ? (<p className="ida-text-secondary">บัญชีนี้ไม่มีสิทธิ์ดู raw response</p>) : raw.error ? (<ApiErrorAlert error={raw.error}/>) : raw.isPending ? (<span className="ida-skeleton" style={{ height: '160px', display: 'block' }}/>) : (<>
              {raw.data?.rawSha256 && (<p className="ida-caption ida-text-secondary">SHA-256: {raw.data.rawSha256}</p>)}
              <pre className="ida-json-block">{raw.data?.rawBody ?? 'ไม่มี raw body ต้นฉบับในรอบนี้'}</pre>
              <details className="ida-json-details">
                <summary>JSON payload ที่ parse ได้</summary>
                <pre className="ida-json-block">{prettyJson(raw.data?.payloadJson ?? null)}</pre>
              </details>
            </>)}
        </section>)}

      <p>
        <Link className="ida-btn ida-btn--ghost" to={data.run.batchId ? `/ingest/batches/${data.run.batchId}` : '/ingest/batches'}>
          <Icon name="undo" size={18}/>
          กลับ
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
function money(value: number): string {
    return value.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}
function actionLabel(action: string): string {
    return action === 'Withdraw' ? 'ถอนผล' : action;
}
