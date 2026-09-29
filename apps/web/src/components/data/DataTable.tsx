import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { Icon } from '../Icon';
import type { IconName } from '../Icon';
import { amountClass, formatTHB } from '../../styles/tokens';
import { ApiErrorAlert } from '../feedback/ApiErrorAlert';
import { StatusBadge } from './StatusBadge';
export interface ColumnDef<TRow> {
    key: string;
    header: string;
    align?: 'left' | 'right';
    width?: string;
    sortable?: boolean;
    format?: 'text' | 'date' | 'time' | 'amount' | 'status';
    render?: (row: TRow) => ReactNode;
    value?: (row: TRow) => unknown;
}
export interface RowAction<TRow> {
    icon: IconName;
    label: string;
    to?: (row: TRow) => string;
    onClick?: (row: TRow) => void;
    tone?: 'default' | 'danger';
    hidden?: boolean;
}
export interface EmptyStateProps {
    title: string;
    hint: string;
    action?: {
        label: string;
        to: string;
    } | {
        label: string;
        onClick: () => void;
    };
    icon?: IconName;
}
export interface RowSelection<TRow> {
    selected: ReadonlySet<string>;
    onChange: (next: Set<string>) => void;
    label: (row: TRow) => string;
}
interface DataTableProps<TRow> {
    caption: string;
    selection?: RowSelection<TRow>;
    columns: readonly ColumnDef<TRow>[];
    rows: readonly TRow[];
    rowKey: (row: TRow) => string;
    loading?: boolean;
    error?: unknown;
    sort?: string;
    onSortChange?: (sort: string) => void;
    rowActions?: readonly RowAction<TRow>[];
    empty: EmptyStateProps;
}
export function DataTable<TRow>({ caption, selection, columns, rows, rowKey, loading = false, error, sort, onSortChange, rowActions = [], empty, }: DataTableProps<TRow>) {
    const actions = rowActions.filter((a) => !a.hidden);
    if (error)
        return <ApiErrorAlert error={error}/>;
    if (loading) {
        return (<div className="ida-table-wrap">
        <div className="ida-table-scroll">
          <table className="ida-table">
            <caption className="ida-visually-hidden">{caption} (กำลังโหลด)</caption>
          <Head columns={columns} actions={actions.length > 0} sort={sort}/>
          <tbody>
            {Array.from({ length: 5 }, (_, i) => (<tr key={i}>
                {columns.map((c) => (<td key={c.key}>
                    <span className="ida-skeleton" style={{ height: '1em', display: 'block' }}/>
                  </td>))}
                {actions.length > 0 && <td />}
              </tr>))}
          </tbody>
        </table>
        </div>
      </div>);
    }
    if (rows.length === 0) {
        return (<div className="ida-table-wrap">
        <div className="ida-empty">
          <div className="ida-empty__icon">
            <Icon name={empty.icon ?? 'inbox'} size={28}/>
          </div>
          <p className="ida-empty__title">{empty.title}</p>
          <p className="ida-empty__hint">{empty.hint}</p>
          {empty.action &&
                ('to' in empty.action ? (<Link className="ida-btn ida-btn--primary" to={empty.action.to}>
                {empty.action.label}
              </Link>) : (<button type="button" className="ida-btn ida-btn--secondary" onClick={empty.action.onClick}>
                {empty.action.label}
              </button>))}
        </div>
      </div>);
    }
    return (<div className="ida-table-wrap">
      
      <div className="ida-table-scroll">
        <table className="ida-table">
          <caption className="ida-visually-hidden">{caption}</caption>
        <Head columns={columns} actions={actions.length > 0} sort={sort} onSortChange={onSortChange} selectAll={selection && {
            checked: rows.length > 0 && rows.every((r) => selection.selected.has(rowKey(r))),
            partial: rows.some((r) => selection.selected.has(rowKey(r))),
            onToggle: (on: boolean) => selection.onChange(on ? new Set(rows.map(rowKey)) : new Set()),
        }}/>
        <tbody>
          {rows.map((row) => (<tr key={rowKey(row)} aria-selected={selection ? selection.selected.has(rowKey(row)) : undefined}>
              {selection && (<td className="ida-table__select">
                  <input type="checkbox" checked={selection.selected.has(rowKey(row))} aria-label={`เลือก ${selection.label(row)}`} onChange={(e) => {
                    const next = new Set(selection.selected);
                    if (e.target.checked)
                        next.add(rowKey(row));
                    else
                        next.delete(rowKey(row));
                    selection.onChange(next);
                }}/>
                </td>)}
              {columns.map((column) => (<td key={column.key} style={column.align === 'right' ? { textAlign: 'right' } : undefined} className={cellClass(column, row)} data-label={column.header}>
                  {renderCell(column, row)}
                </td>))}
              {actions.length > 0 && (<td>
                  <div className="ida-table__actions">
                    {actions.map((action) => action.to ? (<Link key={action.label} className="ida-icon-btn" to={action.to(row)} title={action.label} aria-label={action.label}>
                          <Icon name={action.icon} size={18}/>
                        </Link>) : (<button key={action.label} type="button" className={`ida-icon-btn${action.tone === 'danger' ? ' ida-icon-btn--danger' : ''}`} onClick={() => action.onClick?.(row)} title={action.label} aria-label={action.label}>
                          <Icon name={action.icon} size={18}/>
                        </button>))}
                  </div>
                </td>)}
            </tr>))}
        </tbody>
        </table>
      </div>
    </div>);
}
function Head<TRow>({ columns, actions, sort, onSortChange, selectAll, }: {
    columns: readonly ColumnDef<TRow>[];
    actions: boolean;
    sort?: string;
    onSortChange?: (sort: string) => void;
    selectAll?: {
        checked: boolean;
        partial: boolean;
        onToggle: (on: boolean) => void;
    };
}) {
    const descending = sort?.startsWith('-') ?? false;
    const sortKey = descending ? sort!.slice(1) : sort;
    return (<thead>
      <tr>
        {selectAll && (<th scope="col" className="ida-table__select">
            <input type="checkbox" aria-label="เลือกทุกรายการในหน้านี้" checked={selectAll.checked} ref={(el) => {
                if (el)
                    el.indeterminate = selectAll.partial && !selectAll.checked;
            }} onChange={(e) => selectAll.onToggle(e.target.checked)}/>
          </th>)}
        {columns.map((column) => {
            const active = column.key === sortKey;
            const ariaSort = !column.sortable
                ? undefined
                : active
                    ? descending
                        ? 'descending'
                        : 'ascending'
                    : 'none';
            return (<th key={column.key} scope="col" style={{
                    width: column.width,
                    ...(column.align === 'right' ? { textAlign: 'right' } : {}),
                }} aria-sort={ariaSort}>
              {column.sortable && onSortChange ? (<button type="button" className="ida-table__sort" onClick={() => onSortChange(active && !descending ? `-${column.key}` : column.key)}>
                  {column.header}
                  <SortMark active={active} descending={descending}/>
                </button>) : (<span className="ida-table__sort">
                  {column.header}
                  {column.sortable && <SortMark active={active} descending={descending}/>}
                </span>)}
            </th>);
        })}
        {actions && (<th scope="col" style={{ width: '140px', textAlign: 'right' }}>
            จัดการ
          </th>)}
      </tr>
    </thead>);
}
function SortMark({ active, descending }: {
    active: boolean;
    descending: boolean;
}) {
    return (<Icon name={active ? (descending ? 'arrowDown' : 'arrowUp') : 'sortable'} size={14} className={`ida-table__sort-mark${active ? ' ida-table__sort-mark--active' : ''}`}/>);
}
function rawValue<TRow>(column: ColumnDef<TRow>, row: TRow): unknown {
    if (column.value)
        return column.value(row);
    return (row as Record<string, unknown>)[column.key];
}
function cellClass<TRow>(column: ColumnDef<TRow>, row: TRow): string | undefined {
    if (column.format !== 'amount')
        return undefined;
    const value = rawValue(column, row);
    return typeof value === 'number' ? amountClass(value) : 'ida-numeric';
}
function renderCell<TRow>(column: ColumnDef<TRow>, row: TRow): ReactNode {
    if (column.render)
        return column.render(row);
    const value = rawValue(column, row);
    if (value === null || value === undefined || value === '') {
        return <span className="ida-text-muted">—</span>;
    }
    switch (column.format) {
        case 'amount':
            return typeof value === 'number' ? formatTHB(value) : String(value);
        case 'date':
            return formatDate(String(value));
        case 'time':
            return formatTime(String(value));
        case 'status':
            return <StatusBadge status={String(value)}/>;
        default:
            return String(value);
    }
}
function formatTime(raw: string): string {
    const match = /^(\d{2}):(\d{2})/.exec(raw);
    return match ? `${match[1]}:${match[2]}` : raw;
}
function formatDate(iso: string): string {
    const date = new Date(iso);
    if (Number.isNaN(date.getTime()))
        return iso;
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}/${date.getFullYear()}`;
}
