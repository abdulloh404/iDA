import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { useSearchParams } from 'react-router-dom';
import { DataTable } from '../../components/data/DataTable';
import { Pagination } from '../../components/data/Pagination';
import { PageHeader } from '../../components/layout/PageHeader';
import { ListToolbar } from '../master-data/screens/ListToolbar';
import type { FilterDef } from '../master-data/descriptor';
import { DEFAULT_PAGE_SIZE } from '../../api/types';
import type { ListParams } from '../../api/types';
import { APPROVAL_STATUSES, APPROVAL_STATUS_LABELS, approvalsApi } from './api';
import type { ApprovalScope } from './api';
const TITLES: Record<ApprovalScope, {
    title: string;
    description: string;
}> = {
    mine: {
        title: 'คำขอของฉัน',
        description: 'คำขอที่คุณเป็นผู้ส่ง ติดตามได้ว่าตอนนี้ค้างอยู่ที่หน่วยงานใด',
    },
    pending: {
        title: 'รายการรอดำเนินการ',
        description: 'คำขอที่รอการตัดสินจากบทบาทของคุณ',
    },
    history: {
        title: 'ประวัติคำขอ',
        description: 'คำขอทั้งหมดของโรงพยาบาลนี้ ทั้งที่ปิดแล้วและที่ยังดำเนินอยู่',
    },
};
export function ApprovalListScreen({ scope }: {
    scope: ApprovalScope;
}) {
    const [searchParams, setSearchParams] = useSearchParams();
    const { title, description } = TITLES[scope];
    const requestTypes = useQuery({
        queryKey: ['approvals', 'request-types'],
        queryFn: ({ signal }) => approvalsApi.requestTypes(signal),
        staleTime: 10 * 60 * 1000,
    });
    const filters: FilterDef[] = [
        {
            kind: 'select',
            name: 'requestType',
            label: 'ประเภทคำขอ',
            options: (requestTypes.data ?? []).map((t) => ({ value: t.code, label: t.nameTh })),
        },
        { kind: 'date', name: 'requestedFrom', label: 'วันที่ส่งคำขอ ตั้งแต่' },
        { kind: 'date', name: 'requestedTo', label: 'วันที่ส่งคำขอ ถึง' },
        ...(scope === 'history'
            ? ([
                {
                    kind: 'select',
                    name: 'status',
                    label: 'สถานะ',
                    options: APPROVAL_STATUSES.map((s) => ({
                        value: s,
                        label: APPROVAL_STATUS_LABELS[s],
                    })),
                },
            ] as FilterDef[])
            : []),
    ];
    const params: ListParams = {
        page: Number(searchParams.get('page') ?? 1),
        pageSize: Number(searchParams.get('pageSize') ?? DEFAULT_PAGE_SIZE),
        sort: searchParams.get('sort') ?? '-requestedAt',
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
    const query = useQuery({
        queryKey: ['approvals', scope, params],
        queryFn: ({ signal }) => approvalsApi.list(scope, params, signal),
        placeholderData: keepPreviousData,
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
    return (<>
      <PageHeader title={title} description={description} breadcrumbs={[{ label: 'คำขอและการอนุมัติ' }]}/>

      <div className="ida-table-card">
        <ListToolbar keyword={searchParams.get('q') ?? ''} onKeywordChange={(q) => patchParams({ q })} searchPlaceholder="ค้นหาเลขที่รายการ ชื่อเจ้าหน้าที่ หรือชื่อแพทย์…" filters={filters} values={filterValues} onFilterChange={(name, value) => patchParams({ [name]: value })} onClearAll={() => setSearchParams(new URLSearchParams(), { replace: true })} summary={narrowed && query.data
            ? `พบ ${query.data.total.toLocaleString('th-TH')} รายการ`
            : undefined}/>

        <DataTable caption={title} columns={[
            { key: 'requestNo', header: 'เลขที่รายการ', sortable: true, width: '170px' },
            { key: 'requestTypeNameTh', header: 'ประเภทคำขอ', sortable: false, width: '200px' },
            { key: 'summary', header: 'รายละเอียด' },
            { key: 'requestedBy', header: 'เจ้าหน้าที่', sortable: true, width: '160px' },
            {
                key: 'requestedAt',
                header: 'วันที่ส่งคำขอ',
                sortable: true,
                format: 'date',
                width: '140px',
            },
            {
                key: 'updatedAt',
                header: 'อัพเดตล่าสุด',
                sortable: true,
                format: 'date',
                width: '140px',
            },
            {
                key: 'currentStepRoleNameTh',
                header: 'รออยู่ที่',
                width: '180px',
                value: (row) => row.currentStepRoleNameTh ?? 'ปิดแล้ว',
            },
            { key: 'status', header: 'สถานะ', sortable: true, format: 'status', width: '160px' },
        ]} rows={query.data?.items ?? []} rowKey={(row) => row.id} loading={query.isPending} error={query.error} sort={params.sort} onSortChange={(sort) => patchParams({ sort })} empty={narrowed
            ? {
                icon: 'search',
                title: 'ไม่พบคำขอที่ตรงกับเงื่อนไข',
                hint: 'ลองล้างตัวกรองเพื่อดูรายการทั้งหมด',
                action: {
                    label: 'ล้างตัวกรอง',
                    onClick: () => setSearchParams(new URLSearchParams(), { replace: true }),
                },
            }
            : EMPTY[scope]} rowActions={[
            {
                icon: 'eye',
                label: scope === 'pending' ? 'ตรวจและตัดสิน' : 'ดูรายละเอียด',
                to: (row) => `/approvals/${row.id}`,
            },
        ]}/>

        <Pagination page={query.data} onPageChange={(page) => patchParams({ page })} onPageSizeChange={(pageSize) => patchParams({ pageSize })}/>
      </div>
    </>);
}
const EMPTY: Record<ApprovalScope, {
    title: string;
    hint: string;
}> = {
    mine: {
        title: 'คุณยังไม่มีคำขอ',
        hint: 'คำขอจะถูกสร้างจากหน้าจอที่ต้องผ่านการอนุมัติ เช่น ข้อมูลประวัติแพทย์',
    },
    pending: {
        title: 'ไม่มีคำขอรอคุณดำเนินการ',
        hint: 'เมื่อมีคำขอมาถึงบทบาทของคุณ รายการจะขึ้นที่นี่และมีอีเมลแจ้ง',
    },
    history: {
        title: 'ยังไม่มีคำขอในระบบ',
        hint: 'ประวัติจะเริ่มสะสมเมื่อมีการส่งคำขอแรก',
    },
};
