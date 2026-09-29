import { useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { DataTable } from '../../../components/data/DataTable';
import { Pagination } from '../../../components/data/Pagination';
import { ConfirmDialog } from '../../../components/feedback/ConfirmDialog';
import { useToast } from '../../../components/feedback/toastContext';
import { ApiErrorAlert } from '../../../components/feedback/ApiErrorAlert';
import { PageHeader } from '../../../components/layout/PageHeader';
import { Icon } from '../../../components/Icon';
import { ListToolbar } from './ListToolbar';
import { ApprovalBar } from './ApprovalBar';
import type { ApprovalAction } from '../descriptor';
import { invalidateAfterWrite } from '../../../app/queryClient';
import { usePermission } from '../../auth/authState';
import { DEFAULT_PAGE_SIZE } from '../../../api/types';
import type { ListParams, StatusFilter } from '../../../api/types';
import type { AnyScreenDescriptor } from '../descriptor';
export function MasterListScreen({ descriptor }: {
    descriptor: AnyScreenDescriptor;
}) {
    const [searchParams, setSearchParams] = useSearchParams();
    const queryClient = useQueryClient();
    const toast = useToast();
    const canWrite = usePermission(`${descriptor.resource}.write`);
    const canCreate = canWrite && !descriptor.systemDefined;
    const canDelete = usePermission(`${descriptor.resource}.delete`) && !descriptor.systemDefined;
    const canExport = usePermission(`${descriptor.resource}.export`);
    const [pendingDelete, setPendingDelete] = useState<Record<string, unknown> | null>(null);
    const [exporting, setExporting] = useState(false);
    const [exportError, setExportError] = useState<unknown>(null);
    const selectionKey = searchParams.toString();
    const [selection, setSelection] = useState<{
        key: string;
        ids: Set<string>;
    }>({
        key: selectionKey,
        ids: new Set(),
    });
    const selected = selection.key === selectionKey ? selection.ids : new Set<string>();
    const canApproveRole = usePermission(descriptor.approval?.permission);
    const params: ListParams = {
        page: Number(searchParams.get('page') ?? 1),
        pageSize: Number(searchParams.get('pageSize') ?? DEFAULT_PAGE_SIZE),
        sort: searchParams.get('sort') ?? descriptor.defaultSort,
        status: (searchParams.get('status') ?? 'all') as StatusFilter,
        ...(searchParams.get('q') ? { q: searchParams.get('q')! } : {}),
    };
    for (const filter of descriptor.filters) {
        if (filter.kind === 'status')
            continue;
        const value = searchParams.get(filter.name);
        if (value)
            params[filter.name] = value;
    }
    Object.assign(params, descriptor.fixedFilters ?? {});
    const filterValues: Record<string, string> = {};
    for (const filter of descriptor.filters) {
        filterValues[filter.name] =
            searchParams.get(filter.name) ?? (filter.kind === 'status' ? 'all' : '');
    }
    const narrowed = (searchParams.get('q') ?? '') !== '' ||
        descriptor.filters.some((f) => filterValues[f.name] !== (f.kind === 'status' ? 'all' : ''));
    const query = useQuery({
        queryKey: ['master', descriptor.resource, 'list', params],
        queryFn: ({ signal }) => descriptor.api.list(params, signal),
        placeholderData: keepPreviousData,
    });
    const total = query.data?.total;
    const firstRun = total === 0 && !narrowed;
    const approving = descriptor.approval !== undefined &&
        descriptor.approval.permission !== '' &&
        canApproveRole &&
        filterValues[descriptor.approval.statusFilter] === 'PENDING';
    const decide = useMutation({
        mutationFn: ({ action, comment }: {
            action: ApprovalAction;
            comment: string | null;
        }) => descriptor.approval!.decide([...selected], action, comment),
        onSuccess: (_result, { action }) => {
            invalidateAfterWrite(queryClient);
            const verb = action === 'APPROVE' ? 'อนุมัติ' : action === 'RETURN' ? 'ส่งกลับ' : 'ไม่อนุมัติ';
            toast.success(`${verb} ${selected.size} รายการแล้ว`);
            setSelection({ key: selectionKey, ids: new Set() });
        },
        onError: (error) => toast.error(error instanceof Error ? error.message : 'ดำเนินการไม่สำเร็จ'),
    });
    const remove = useMutation({
        mutationFn: (id: string) => descriptor.api.remove(id),
        onSuccess: () => {
            invalidateAfterWrite(queryClient);
            toast.success(`ลบข้อมูล${descriptor.titleTh}เรียบร้อยแล้ว`);
            setPendingDelete(null);
        },
        onError: (error) => {
            toast.error(error instanceof Error ? error.message : 'ลบข้อมูลไม่สำเร็จ');
            setPendingDelete(null);
        },
    });
    function patchParams(patch: Record<string, string | number | undefined>) {
        const next = new URLSearchParams(searchParams);
        for (const [key, value] of Object.entries(patch)) {
            if (value === undefined || value === '' || value === 'all')
                next.delete(key);
            else
                next.set(key, String(value));
        }
        if (!('page' in patch))
            next.delete('page');
        setSearchParams(next, { replace: true });
    }
    async function runExport() {
        setExporting(true);
        setExportError(null);
        try {
            const { blob, fileName } = await descriptor.api.exportXlsx({ ...params, page: 1 });
            const url = URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = fileName;
            link.click();
            URL.revokeObjectURL(url);
        }
        catch (error) {
            setExportError(error);
        }
        finally {
            setExporting(false);
        }
    }
    return (<>
      
      <PageHeader title={descriptor.titleTh} breadcrumbs={descriptor.breadcrumb} actions={firstRun ? undefined : (<>
              {descriptor.ListActions && <descriptor.ListActions />}
              {canExport && (<button type="button" className="ida-btn ida-btn--secondary" onClick={() => void runExport()} disabled={exporting || total === 0}>
                  {exporting ? (<span className="ida-spinner" aria-hidden="true"/>) : (<Icon name="download" size={18}/>)}
                  Export
                </button>)}
              {canCreate && (<Link className="ida-btn ida-btn--primary" to={`${descriptor.path}/new`}>
                  <Icon name="plus" size={18}/>
                  สร้างข้อมูล
                </Link>)}
            </>)}/>

      {exportError != null && (<div style={{ marginBottom: 'var(--ida-space-4)' }}>
          <ApiErrorAlert error={exportError}/>
        </div>)}

      <div className="ida-table-card">
        <ListToolbar keyword={searchParams.get('q') ?? ''} onKeywordChange={(q) => patchParams({ q })} searchPlaceholder={`${descriptor.searchHint ?? `ค้นหา${descriptor.titleTh}`}…`} filters={descriptor.filters} values={filterValues} onFilterChange={(name, value) => patchParams({ [name]: value })} onClearAll={() => setSearchParams(new URLSearchParams(), { replace: true })} summary={narrowed && query.data
            ? `พบ ${query.data.total.toLocaleString('th-TH')} รายการ`
            : undefined}/>

      {approving && (<ApprovalBar count={selected.size} busy={decide.isPending} onDecide={(action, comment) => decide.mutate({ action, comment })}/>)}

      <DataTable caption={descriptor.titleTh} selection={approving
            ? {
                selected,
                onChange: (ids) => setSelection({ key: selectionKey, ids }),
                label: (row) => descriptor.rowKey(row),
            }
            : undefined} columns={descriptor.columns} rows={query.data?.items ?? []} rowKey={descriptor.rowKey} loading={query.isPending} error={query.error} sort={params.sort} onSortChange={(sort) => patchParams({ sort })} empty={narrowed
            ? {
                icon: 'search',
                title: 'ไม่พบข้อมูลที่ตรงกับเงื่อนไข',
                hint: 'ลองใช้คำค้นที่สั้นลง หรือล้างตัวกรองเพื่อดูรายการทั้งหมด',
                action: {
                    label: 'ล้างตัวกรอง',
                    onClick: () => setSearchParams(new URLSearchParams(), { replace: true }),
                },
            }
            : {
                title: `ยังไม่มีข้อมูล${descriptor.titleTh}`,
                hint: descriptor.emptyHint,
                ...(canCreate
                    ? { action: { label: 'สร้างข้อมูล', to: `${descriptor.path}/new` } }
                    : {}),
            }} rowActions={[
            { icon: 'eye', label: 'ดูข้อมูล', to: (row) => `${descriptor.path}/${descriptor.rowKey(row)}` },
            {
                icon: 'pencil',
                label: 'แก้ไข',
                to: (row) => `${descriptor.path}/${descriptor.rowKey(row)}/edit`,
                hidden: !canWrite,
            },
            {
                icon: 'trash',
                label: 'ลบ',
                tone: 'danger',
                onClick: (row) => setPendingDelete(row),
                hidden: !canDelete,
            },
        ]}/>

      <Pagination page={query.data} onPageChange={(page) => patchParams({ page })} onPageSizeChange={(pageSize) => patchParams({ pageSize })}/>
      </div>

      <ConfirmDialog open={pendingDelete !== null} title={`ลบข้อมูล${descriptor.titleTh}`} tone="danger" confirmLabel="ลบข้อมูล" busy={remove.isPending} onCancel={() => setPendingDelete(null)} onConfirm={() => pendingDelete && remove.mutate(descriptor.rowKey(pendingDelete))}>
        ต้องการลบรายการนี้หรือไม่ ข้อมูลจะไม่แสดงในระบบอีกต่อไป
        แต่รายการเดิมที่อ้างถึงอยู่จะยังคงอยู่
      </ConfirmDialog>
    </>);
}
