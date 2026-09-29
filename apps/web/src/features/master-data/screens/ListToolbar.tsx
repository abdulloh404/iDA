import { useEffect, useRef, useState } from 'react';
import * as Popover from '@radix-ui/react-popover';
import { useQueries } from '@tanstack/react-query';
import { Icon } from '../../../components/Icon';
import { Select } from '../../../components/form/Select';
import type { SelectOption } from '../../../components/form/Select';
import { fetchLookup, lookupLabel } from '../../../api/lookup';
import { useDebouncedValue } from '../../../hooks/useDebouncedValue';
import { RECORD_STATUS_OPTIONS } from '../descriptor';
import type { FilterDef } from '../descriptor';
type LookupOptions = Record<string, readonly SelectOption[]>;
interface Lookups {
    options: LookupOptions;
    pending: Record<string, boolean>;
}
export interface ActiveFilter {
    name: string;
    label: string;
}
interface ListToolbarProps {
    keyword: string;
    onKeywordChange: (keyword: string) => void;
    searchPlaceholder: string;
    filters: readonly FilterDef[];
    values: Record<string, string>;
    onFilterChange: (name: string, value: string) => void;
    onClearAll: () => void;
    summary?: string;
}
const INLINE_LIMIT = 2;
export function ListToolbar({ keyword, onKeywordChange, searchPlaceholder, filters, values, onFilterChange, onClearAll, summary, }: ListToolbarProps) {
    const [draft, setDraft] = useState(keyword);
    const debounced = useDebouncedValue(draft, 300);
    const published = useRef(keyword);
    const [filtersOpen, setFiltersOpen] = useState(false);
    const lookups = useLookupOptions(filters);
    const lookupOptions = lookups.options;
    useEffect(() => {
        if (debounced === keyword)
            return;
        published.current = debounced;
        onKeywordChange(debounced);
    }, [debounced]);
    useEffect(() => {
        if (keyword === published.current)
            return;
        published.current = keyword;
        setDraft(keyword);
    }, [keyword]);
    const inline = filters.length <= INLINE_LIMIT ? filters : [];
    const advanced = inline.length > 0 ? [] : filters;
    const active = activeFilters(advanced, values, lookupOptions);
    const hasAnything = keyword !== '' || activeFilters(filters, values, lookupOptions).length > 0;
    return (<div className="ida-list-toolbar">
      <div className="ida-list-toolbar__row">
        <div className="ida-search">
          <Icon name="search" size={18} className="ida-search__icon"/>
          <label className="ida-visually-hidden" htmlFor="list-search">
            ค้นหา
          </label>
          <input id="list-search" className="ida-search__input" type="search" value={draft} onChange={(e) => setDraft(e.target.value)} placeholder={searchPlaceholder}/>
          {draft !== '' && (<button type="button" className="ida-search__clear" onClick={() => setDraft('')} aria-label="ล้างคำค้นหา">
              <Icon name="close" size={16}/>
            </button>)}
        </div>

        {inline.map((filter) => (<InlineFilter key={filter.name} filter={filter} value={values[filter.name] ?? ''} options={lookupOptions[filter.name]} pending={lookups.pending[filter.name]} onChange={(value) => onFilterChange(filter.name, value)}/>))}

        {advanced.length > 0 && (<Popover.Root open={filtersOpen} onOpenChange={setFiltersOpen}>
            <Popover.Trigger asChild>
              <button type="button" className="ida-btn ida-btn--secondary ida-filter-button">
                <Icon name="filter" size={18}/>
                ตัวกรอง
                {active.length > 0 && (<span className="ida-filter-button__count">{active.length}</span>)}
              </button>
            </Popover.Trigger>

            <Popover.Portal>
              <Popover.Content className="ida-filter-panel" align="end" sideOffset={6}>
                <p className="ida-filter-panel__title">ตัวกรองขั้นสูง</p>

                {advanced.map((filter) => (<FilterControl key={filter.name} filter={filter} value={values[filter.name] ?? ''} options={lookupOptions[filter.name]} onChange={(value) => onFilterChange(filter.name, value)}/>))}

                <div className="ida-filter-panel__footer">
                  <button type="button" className="ida-btn ida-btn--ghost ida-btn--sm" onClick={() => {
                setDraft('');
                onClearAll();
            }} disabled={!hasAnything}>
                    ล้างทั้งหมด
                  </button>
                  <button type="button" className="ida-btn ida-btn--primary ida-btn--sm" onClick={() => setFiltersOpen(false)}>
                    เสร็จสิ้น
                  </button>
                </div>
              </Popover.Content>
            </Popover.Portal>
          </Popover.Root>)}

        {summary && <span className="ida-list-toolbar__summary">{summary}</span>}
      </div>

      {active.length > 0 && (<div className="ida-chips">
          {active.map((chip) => (<span className="ida-chip" key={chip.name}>
              {chip.label}
              <button type="button" className="ida-chip__remove" onClick={() => onFilterChange(chip.name, '')} aria-label={`ล้างตัวกรอง ${chip.label}`}>
                <Icon name="close" size={14}/>
              </button>
            </span>))}

          <button type="button" className="ida-chips__clear" onClick={() => {
                setDraft('');
                onClearAll();
            }}>
            ล้างทั้งหมด
          </button>
        </div>)}
    </div>);
}
function InlineFilter({ filter, value, options, pending, onChange, }: {
    filter: FilterDef;
    value: string;
    options?: readonly SelectOption[];
    pending?: boolean;
    onChange: (value: string) => void;
}) {
    const narrowing = value !== '' && !(filter.kind === 'status' && value === 'all');
    const className = `ida-inline-filter${narrowing ? ' ida-inline-filter--on' : ''}`;
    if (filter.kind === 'date') {
        return (<input className={`ida-input ${className}`} type="date" value={value} onChange={(e) => onChange(e.target.value)} aria-label={filter.label}/>);
    }
    if (filter.kind === 'text') {
        return (<input className={`ida-input ${className}`} value={value} onChange={(e) => onChange(e.target.value)} placeholder={filter.placeholder ?? filter.label} aria-label={filter.label}/>);
    }
    const choices = optionsFor(filter, options);
    return (<Select ariaLabel={filter.label} className={className} prefix={filter.label} value={value} onChange={onChange} options={choices} emptyLabel={filter.kind === 'status' ? undefined : 'ทั้งหมด'} disabled={pending === true || (filter.kind === 'lookup' && choices.length === 0)}/>);
}
function FilterControl({ filter, value, options, onChange, }: {
    filter: FilterDef;
    value: string;
    options?: readonly SelectOption[];
    onChange: (value: string) => void;
}) {
    const id = `filter-${filter.name}`;
    return (<div className="ida-field">
      <label className="ida-label" htmlFor={id}>
        {filter.label}
      </label>
      {filter.kind === 'date' ? (<input id={id} className="ida-input" type="date" value={value} onChange={(e) => onChange(e.target.value)}/>) : filter.kind === 'text' ? (<input id={id} className="ida-input" value={value} onChange={(e) => onChange(e.target.value)} placeholder={filter.placeholder}/>) : (<Select id={id} value={value} onChange={onChange} options={optionsFor(filter, options)} emptyLabel={filter.kind === 'status' ? undefined : 'ทั้งหมด'}/>)}
    </div>);
}
function useLookupOptions(filters: readonly FilterDef[]): Lookups {
    const lookups = filters.filter((f) => f.kind === 'lookup');
    const results = useQueries({
        queries: lookups.map((filter) => ({
            queryKey: ['lookup', filter.resource],
            queryFn: ({ signal }: {
                signal: AbortSignal;
            }) => fetchLookup(filter.resource, undefined, signal),
            staleTime: 5 * 60 * 1000,
        })),
    });
    const options: LookupOptions = {};
    const pending: Record<string, boolean> = {};
    lookups.forEach((filter, index) => {
        options[filter.name] = (results[index]?.data ?? []).map((item) => ({
            value: item.id,
            label: lookupLabel(item),
        }));
        pending[filter.name] = results[index]?.isPending ?? true;
    });
    return { options, pending };
}
function optionsFor(filter: FilterDef, lookup?: readonly SelectOption[]) {
    if (filter.kind === 'select')
        return filter.options;
    if (filter.kind === 'date')
        return [];
    if (filter.kind === 'lookup')
        return lookup ?? [];
    if (filter.kind === 'status') {
        return [
            { value: 'all', label: 'ทั้งหมด' },
            ...RECORD_STATUS_OPTIONS.map((o) => ({ value: o.value.toLowerCase(), label: o.label })),
        ];
    }
    return [];
}
function activeFilters(filters: readonly FilterDef[], values: Record<string, string>, lookupOptions: LookupOptions): ActiveFilter[] {
    const chips: ActiveFilter[] = [];
    for (const filter of filters) {
        const value = values[filter.name] ?? '';
        if (value === '' || (filter.kind === 'status' && value === 'all'))
            continue;
        const options = optionsFor(filter, lookupOptions[filter.name]);
        const label = options.find((o) => o.value === value)?.label ??
            (filter.kind === 'lookup' ? 'กำลังโหลด…' : value);
        chips.push({ name: filter.name, label: `${filter.label}: ${label}` });
    }
    return chips;
}
