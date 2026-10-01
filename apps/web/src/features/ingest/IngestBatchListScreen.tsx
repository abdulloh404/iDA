import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { DataTable } from '../../components/data/DataTable';
import { Pagination } from '../../components/data/Pagination';
import { ConfirmDialog } from '../../components/feedback/ConfirmDialog';
import { ApiErrorAlert } from '../../components/feedback/ApiErrorAlert';
import { useToast } from '../../components/feedback/toastContext';
import { Icon, type IconName } from '../../components/Icon';
import { Select } from '../../components/form/Select';
import { PageHeader } from '../../components/layout/PageHeader';
import { invalidateAfterWrite } from '../../app/queryClient';
import { DEFAULT_PAGE_SIZE } from '../../api/types';
import type { ListParams } from '../../api/types';
import { useAuth } from '../auth/authState';
import { ListToolbar } from '../master-data/screens/ListToolbar';
import type { FilterDef } from '../master-data/descriptor';
import { ingestApi } from './api';
import type { SourceFilter, TriggerMockIngestInput } from './api';
import { formatDate, formatDateTime, formatInt, shortId, todayInput } from './format';
import { IngestPermissionState } from './IngestPermissionState';
import { useState } from 'react';
const SOURCE_OPTIONS = [
    { value: 'all', label: 'HIS + Oracle AR' },
    { value: 'his', label: 'HIS เท่านั้น' },
    { value: 'oracle', label: 'Oracle AR เท่านั้น' },
] as const;
const STATUS_OPTIONS = [
    { value: 'Running', label: 'กำลังทำงาน' },
    { value: 'Published', label: 'สำเร็จ' },
    { value: 'Failed', label: 'ล้มเหลว' },
    { value: 'Withdrawn', label: 'ถอนแล้ว' },
] as const;
export function IngestBatchListScreen() {
    const [searchParams, setSearchParams] = useSearchParams();
    const { can } = useAuth();
    const canRead = can('ingest.read');
    const canTrigger = can('ingest.trigger');
    const canUseTrigger = canRead && canTrigger;
    const queryClient = useQueryClient();
    const toast = useToast();
    const navigate = useNavigate();
    const [businessDate, setBusinessDate] = useState(todayInput());
    const [source, setSource] = useState<SourceFilter>('all');
    const [pendingTrigger, setPendingTrigger] = useState<TriggerMockIngestInput | null>(null);
    const filters: FilterDef[] = [
        { kind: 'date', name: 'calledFrom', label: 'วันที่เรียก ตั้งแต่' },
        { kind: 'date', name: 'calledTo', label: 'วันที่เรียก ถึง' },
        { kind: 'date', name: 'businessFrom', label: 'วันที่ข้อมูล ตั้งแต่' },
        { kind: 'date', name: 'businessTo', label: 'วันที่ข้อมูล ถึง' },
        {
            kind: 'select',
            name: 'source',
            label: 'แหล่งข้อมูล',
            options: SOURCE_OPTIONS.map((x) => ({ value: x.value, label: x.label })),
        },
        {
            kind: 'select',
            name: 'status',
            label: 'สถานะ',
            options: STATUS_OPTIONS.map((x) => ({ value: x.value, label: x.label })),
        },
    ];
    const params: ListParams = {
        page: Number(searchParams.get('page') ?? 1),
        pageSize: Number(searchParams.get('pageSize') ?? DEFAULT_PAGE_SIZE),
        sort: searchParams.get('sort') ?? '-startedAt',
        ...(searchParams.get('q') ? { q: searchParams.get('q')! } : {}),
    };
    for (const filter of filters) {
        const value = searchParams.get(filter.name);
        if (value)
            params[filter.name] = value;
    }
    const filterValues: Record<string, string> = {};
    for (const filter of filters)
        filterValues[filter.name] = searchParams.get(filter.name) ?? '';
    const narrowed = (searchParams.get('q') ?? '') !== '' || filters.some((f) => filterValues[f.name] !== '');
    const batches = useQuery({
        queryKey: ['ingest', 'batches', params],
        queryFn: ({ signal }) => ingestApi.listBatches(params, signal),
        enabled: canRead,
        placeholderData: keepPreviousData,
        refetchInterval: (query) => query.state.data?.batches.items.some((batch) => batch.status === 'Running') ? 3000 : false,
    });
    const trigger = useMutation({
        mutationFn: (input: TriggerMockIngestInput) => ingestApi.triggerMock(input),
        onSuccess: (result) => {
            invalidateAfterWrite(queryClient);
            setPendingTrigger(null);
            if (result.status === 'Running')
                toast.info('รับงานนำเข้าแล้ว กำลังรอ worker ประมวลผล');
            else if (result.status === 'Published')
                toast.success('เปิด batch mock สำเร็จแล้ว');
            else
                toast.error('เปิด batch mock แล้ว แต่จบด้วยสถานะล้มเหลว');
            navigate(`/ingest/batches/${result.batchId}`);
        },
    });
    function patchParams(patch: Record<string, string | number | undefined>) {
        const next = new URLSearchParams(searchParams);
        for (const [key, value] of Object.entries(patch)) {
            if (value === undefined || value === '')
                next.delete(key);
            else
                next.set(key, String(value));
        }
        if (!('page' in patch))
            next.delete('page');
        setSearchParams(next, { replace: true });
    }
    function buildTrigger(): TriggerMockIngestInput {
        const key = globalThis.crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random()}`;
        return { businessDate, source, idempotencyKey: key };
    }
    const data = batches.data;
    return (<>
      <PageHeader title="การนำเข้าข้อมูล" description="ติดตาม batch/run mock, raw coverage, staging, issue และกระทบยอดระดับ ingest" breadcrumbs={[{ label: 'การนำเข้าข้อมูล' }]}/>

      <section className="ida-ingest-trigger">
        <div>
          <p className="ida-ingest-trigger__title">นำเข้าข้อมูลจำลองตอนนี้</p>
          <p className="ida-text-secondary">
            คำสั่งนี้เปิด batch MOCK เท่านั้น ไม่เรียก HIS/Oracle จริง
          </p>
        </div>
        <div className="ida-ingest-trigger__controls">
          <label className="ida-field">
            <span className="ida-label">วันที่ข้อมูล</span>
            <input className="ida-input" type="date" value={businessDate} onChange={(event) => setBusinessDate(event.target.value)} disabled={!canUseTrigger || trigger.isPending}/>
          </label>
          <label className="ida-field">
            <span className="ida-label">แหล่งข้อมูล</span>
            <Select value={source} onChange={(value) => setSource(value as SourceFilter)} options={SOURCE_OPTIONS} disabled={!canUseTrigger || trigger.isPending} ariaLabel="แหล่งข้อมูล mock"/>
          </label>
          <button type="button" className="ida-btn ida-btn--primary" disabled={!canUseTrigger || trigger.isPending || businessDate === ''} onClick={() => setPendingTrigger(buildTrigger())}>
            <Icon name="refresh" size={18}/>
            นำเข้าข้อมูลจำลองตอนนี้
          </button>
        </div>
        {!canTrigger && (<p className="ida-field-hint">บัญชีนี้ไม่มีสิทธิ์สั่งนำเข้าข้อมูลจำลอง</p>)}
        {canTrigger && !canRead && (<p className="ida-field-hint">
            ต้องมีสิทธิ์ดูประวัติการนำเข้าก่อน จึงจะเปิด batch และตรวจผลต่อได้
          </p>)}
        {trigger.error && <ApiErrorAlert error={trigger.error}/>}
      </section>

      {data && (<section className="ida-metric-grid ida-ingest-summary" aria-label="สรุปการนำเข้า">
          <Metric label="Batch" value={data.summary.batchCount} icon="database" tone="info"/>
          <Metric label="Dataset-run" value={data.summary.datasetRunCount} icon="layers" tone="primary"/>
          <Metric label="สำเร็จ" value={data.summary.publishedRunCount} icon="check" tone="success"/>
          <Metric label="ล้มเหลว" value={data.summary.failedRunCount} icon="alert" tone="danger"/>
          <Metric label="รับเข้า" value={data.summary.receivedCount} icon="inbox" tone="accent"/>
          <Metric label="ซ้ำ" value={data.summary.duplicateCount} icon="refresh" tone="warning"/>
          <Metric label="รอตรวจ" value={data.summary.pendingCount} icon="clock" tone="pending"/>
          <Metric label="ไม่ผ่าน" value={data.summary.rejectedCount} icon="ban" tone="danger"/>
        </section>)}

      {data && data.legacy.datasetRunCount > 0 && (<div className="ida-alert ida-alert--warning ida-ingest-legacy-alert">
          <Icon name="alert" size={20}/>
          <div>
            มี run เก่า {formatInt(data.legacy.datasetRunCount)} รายการที่จัดกลุ่มเป็น batch ไม่ได้
            {data.legacy.rawPageCount > 0
                ? ` และมี raw page ${formatInt(data.legacy.rawPageCount)} หน้า`
                : ' และไม่มีหลักฐาน raw ต้นฉบับ'}
          </div>
        </div>)}

      <div className="ida-table-card ida-ingest-history">
        <ListToolbar keyword={searchParams.get('q') ?? ''} onKeywordChange={(q) => patchParams({ q })} searchPlaceholder="ค้นหา batch, source, สถานะ หรือผู้สั่ง…" filters={filters} values={filterValues} onFilterChange={(name, value) => patchParams({ [name]: value })} onClearAll={() => setSearchParams(new URLSearchParams(), { replace: true })} summary={narrowed && data
            ? `พบ ${data.batches.total.toLocaleString('th-TH')} batch`
            : undefined}/>

        {canRead ? (<DataTable caption="ประวัติการนำเข้าข้อมูล" columns={[
                {
                    key: 'startedAt',
                    header: 'เวลาเรียก',
                    sortable: true,
                    width: '150px',
                    render: (row) => formatDateTime(row.startedAt),
                },
                {
                    key: 'businessDate',
                    header: 'วันที่ข้อมูล',
                    sortable: true,
                    width: '130px',
                    render: (row) => formatDate(row.businessDate),
                },
                {
                    key: 'sourceFilter',
                    header: 'แหล่งข้อมูล',
                    sortable: true,
                    width: '150px',
                    value: (row) => sourceLabel(row.sourceFilter),
                },
                { key: 'status', header: 'สถานะ', sortable: true, format: 'status', width: '140px' },
                {
                    key: 'datasetRunCount',
                    header: 'Run',
                    align: 'right',
                    width: '90px',
                    value: (row) => row.datasetRunCount,
                },
                {
                    key: 'receivedCount',
                    header: 'รับเข้า',
                    align: 'right',
                    width: '100px',
                    value: (row) => row.receivedCount,
                },
                {
                    key: 'duplicateCount',
                    header: 'ซ้ำ',
                    align: 'right',
                    width: '90px',
                    value: (row) => row.duplicateCount,
                },
                {
                    key: 'rawBodyCount',
                    header: 'Raw',
                    align: 'right',
                    width: '90px',
                    render: (row) => `${formatInt(row.rawBodyCount)}/${formatInt(row.rawPageCount)}`,
                },
                {
                    key: 'triggeredBy',
                    header: 'ผู้สั่ง',
                    width: '150px',
                    value: (row) => row.triggeredBy ?? row.triggerKind ?? '—',
                },
                {
                    key: 'id',
                    header: 'Batch',
                    width: '110px',
                    render: (row) => <span className="ida-code">{shortId(row.id)}</span>,
                },
            ]} rows={data?.batches.items ?? []} rowKey={(row) => row.id} loading={batches.isPending} error={batches.error} sort={params.sort} onSortChange={(sort) => patchParams({ sort })} empty={{
                icon: 'database',
                title: 'ยังไม่มี batch การนำเข้า',
                hint: 'เมื่อมีการนำเข้า mock หรือรอบอัตโนมัติ รายการจะแสดงที่นี่',
            }} rowActions={[
                { icon: 'eye', label: 'ดูรายละเอียด batch', to: (row) => `/ingest/batches/${row.id}` },
            ]}/>) : (<IngestPermissionState />)}

        {canRead && (<Pagination page={data?.batches} onPageChange={(page) => patchParams({ page })} onPageSizeChange={(pageSize) => patchParams({ pageSize })}/>)}
      </div>

      <ConfirmDialog open={pendingTrigger !== null} title="ยืนยันการนำเข้าข้อมูลจำลอง" confirmLabel="เริ่มนำเข้า mock" busy={trigger.isPending} onCancel={() => setPendingTrigger(null)} onConfirm={() => pendingTrigger && trigger.mutate(pendingTrigger)}>
        <p>
          ระบบจะเปิด batch MOCK สำหรับวันที่ {pendingTrigger?.businessDate ?? '—'} ·{' '}
          {pendingTrigger ? sourceLabel(pendingTrigger.source) : '—'} และบันทึกผลไว้ตรวจย้อนหลัง
        </p>
      </ConfirmDialog>
    </>);
}
type MetricTone = 'accent' | 'danger' | 'info' | 'pending' | 'primary' | 'success' | 'warning';
function Metric({ label, value, icon, tone, }: {
    label: string;
    value: number;
    icon: IconName;
    tone: MetricTone;
}) {
    return (<div className={`ida-metric ida-metric--${tone}`}>
      <span className="ida-metric__icon">
        <Icon name={icon} size={20}/>
      </span>
      <span className="ida-metric__body">
        <span className="ida-metric__label">{label}</span>
        <strong className="ida-metric__value">{formatInt(value)}</strong>
      </span>
    </div>);
}
function sourceLabel(source: string): string {
    return SOURCE_OPTIONS.find((x) => x.value === source)?.label ?? source;
}
