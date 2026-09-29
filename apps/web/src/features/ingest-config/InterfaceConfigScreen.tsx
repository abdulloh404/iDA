import { useEffect, useRef, useState } from 'react';
import type { UIEvent } from 'react';
import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { PageHeader } from '../../components/layout/PageHeader';
import { ApiErrorAlert } from '../../components/feedback/ApiErrorAlert';
import { Icon } from '../../components/Icon';
import { ConfirmDialog } from '../../components/feedback/ConfirmDialog';
import { useToast } from '../../components/feedback/toastContext';
import { invalidateAfterWrite } from '../../app/queryClient';
import { usePreferences } from '../../app/preferences';
import type { Locale } from '../../app/preferences';
import { useT } from '../../i18n/useT';
import type { Translate } from '../../i18n/useT';
import { useAuth } from '../auth/authState';
import { ingestConfigApi } from './api';
import type { CancelledScheduleCursor, IngestSchedule, InterfaceInput, ScheduleInput } from './api';
import './interface-config.css';
const units: IngestSchedule['intervalUnit'][] = ['minute', 'hour', 'day'];
type ScheduleDraft = Omit<ScheduleInput, 'intervalValue'> & {
    intervalValue: number | '';
};
function newSchedule(): ScheduleDraft {
    return { name: '', intervalValue: 1, intervalUnit: 'hour',
        enabled: true, datasetCodes: [], firstRunAt: null };
}
function when(value: string, locale: Locale): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime()))
        return '—';
    return date.toLocaleString(locale === 'th' ? 'th-TH' : 'en-GB', {
        timeZone: 'Asia/Bangkok', dateStyle: 'medium', timeStyle: 'short',
    });
}
function toLocalInput(iso: string | null | undefined): string {
    if (!iso)
        return '';
    const value = new Date(iso);
    if (Number.isNaN(value.getTime()))
        return '';
    return new Date(value.getTime() - value.getTimezoneOffset() * 60000)
        .toISOString().slice(0, 16);
}
function rows<T>(value: T[] | null | undefined): T[] {
    return Array.isArray(value) ? value : [];
}
function datasetCodes(row: IngestSchedule): string[] {
    return rows(row.datasetCodes);
}
function unitLabel(value: IngestSchedule['intervalUnit'] | string, t: Translate, count?: number): string {
    switch (value) {
        case 'minute': return t(count === 1 ? 'ingestConfig.unit.minuteOne' : 'ingestConfig.unit.minute');
        case 'hour': return t(count === 1 ? 'ingestConfig.unit.hourOne' : 'ingestConfig.unit.hour');
        case 'day': return t(count === 1 ? 'ingestConfig.unit.dayOne' : 'ingestConfig.unit.day');
        default: return value || '—';
    }
}
function toEnglishDateTime(iso: string | null | undefined): string {
    if (!iso)
        return '';
    const date = new Date(iso);
    if (Number.isNaN(date.getTime()))
        return '';
    const two = (value: number) => String(value).padStart(2, '0');
    return `${two(date.getDate())}/${two(date.getMonth() + 1)}/${date.getFullYear()} ${two(date.getHours())}:${two(date.getMinutes())}`;
}
function parseEnglishDateTime(text: string): string | null {
    const match = /^(\d{2})\/(\d{2})\/(\d{4}) (\d{2}):(\d{2})$/.exec(text);
    if (!match)
        return null;
    const [, dayText, monthText, yearText, hourText, minuteText] = match;
    const [day, month, year, hour, minute] = [dayText, monthText, yearText, hourText, minuteText].map(Number);
    const date = new Date(year, month - 1, day, hour, minute);
    if (date.getFullYear() !== year || date.getMonth() !== month - 1 || date.getDate() !== day ||
        date.getHours() !== hour || date.getMinutes() !== minute)
        return null;
    return date.toISOString();
}
function EnglishFirstRunInput({ value, onChange, t }: {
    value: string | null | undefined;
    onChange: (value: string | null) => void;
    t: Translate;
}) {
    const [text, setText] = useState(() => toEnglishDateTime(value));
    const lastValue = useRef(value);
    const textInput = useRef<HTMLInputElement>(null);
    const picker = useRef<HTMLInputElement>(null);
    useEffect(() => {
        if (value !== lastValue.current) {
            lastValue.current = value;
            setText(toEnglishDateTime(value));
            textInput.current?.setCustomValidity('');
        }
    }, [value]);
    return <div className="ida-ingest-first-run-field">
    <label htmlFor="ingest-first-run-en">{t('ingestConfig.firstRun')}</label>
    <div className="ida-ingest-first-run-control">
      <input ref={textInput} id="ingest-first-run-en" className="ida-input" type="text" inputMode="numeric" placeholder={t('ingestConfig.firstRunPlaceholder')} value={text} onChange={event => {
            const nextText = event.target.value;
            const nextValue = nextText === '' ? null : parseEnglishDateTime(nextText);
            setText(nextText);
            event.target.setCustomValidity(nextText && !nextValue ? t('ingestConfig.firstRunInvalid') : '');
            lastValue.current = nextValue;
            onChange(nextValue);
        }}/>
      <button type="button" className="ida-ingest-first-run-picker" aria-label={t('ingestConfig.firstRunPicker')} onClick={() => picker.current?.showPicker?.()}>
        <Icon name="calendar" size={18}/>
      </button>
      <input ref={picker} className="ida-ingest-native-picker" type="datetime-local" tabIndex={-1} aria-hidden="true" value={toLocalInput(value)} onChange={event => {
            const nextValue = event.target.value ? new Date(event.target.value).toISOString() : null;
            lastValue.current = nextValue;
            setText(toEnglishDateTime(nextValue));
            textInput.current?.setCustomValidity('');
            onChange(nextValue);
        }}/>
    </div>
  </div>;
}
function resultLabel(value: string | null, t: Translate): string {
    switch (value) {
        case 'Published': return t('ingestConfig.result.Published');
        case 'Failed': return t('ingestConfig.result.Failed');
        case 'Running': return t('ingestConfig.result.Running');
        default: return value || t('ingestConfig.neverRun');
    }
}
function scheduledAt(row: IngestSchedule, locale: Locale): string {
    return row.enabled && row.nextRunAt ? when(row.nextRunAt, locale) : '—';
}
export function InterfaceConfigScreen() {
    const { session } = useAuth();
    return <InterfaceConfigContent key={session?.hospitalId ?? ''}/>;
}
function InterfaceConfigContent() {
    const { session, can } = useAuth();
    const { locale } = usePreferences();
    const t = useT();
    const errorCopy = {
        connectionFailed: t('ingestConfig.connectionFailed'),
        details: t('error.details'),
        referenceId: t('ingestConfig.referenceId'),
    };
    const isAdmin = session?.roles.some(role => role === 'GROUP_ADMIN' || role === 'HOSPITAL_ADMIN') ?? false;
    const canRead = isAdmin && can('ingest-config.read');
    const canWrite = isAdmin && can('ingest-config.write');
    const toast = useToast();
    const client = useQueryClient();
    const scheduleForm = useRef<HTMLFormElement>(null);
    const cancelledList = useRef<HTMLDivElement>(null);
    const loadingMoreCancelled = useRef(false);
    const query = useQuery({
        queryKey: ['ingest-config', session?.hospitalId],
        queryFn: ({ signal }) => ingestConfigApi.get(signal),
        enabled: canRead,
        refetchInterval: 15000,
        refetchOnWindowFocus: 'always',
    });
    const config = query.data;
    const cancelledCount = config?.cancelledScheduleCount ?? 0;
    const [cancelledOpen, setCancelledOpen] = useState(false);
    const [visibleCancelledCount, setVisibleCancelledCount] = useState(10);
    const cancelledQuery = useInfiniteQuery({
        queryKey: ['ingest-cancelled-schedules', session?.hospitalId, cancelledCount],
        queryFn: ({ pageParam, signal }) => ingestConfigApi.getCancelledSchedules(pageParam, signal),
        initialPageParam: null as CancelledScheduleCursor | null,
        getNextPageParam: (page): CancelledScheduleCursor | undefined => {
            const last = page.items.at(-1);
            return page.hasMore && last?.cancelledAt
                ? { beforeAt: last.cancelledAt, beforeId: last.id } : undefined;
        },
        enabled: canRead && cancelledOpen && cancelledCount > 0,
    });
    const [editingInterface, setEditingInterface] = useState<string | null>(null);
    const [interfaceDraft, setInterfaceDraft] = useState<InterfaceInput | null>(null);
    const [editingSchedule, setEditingSchedule] = useState<string | null>(null);
    const [scheduleDraft, setScheduleDraft] = useState<ScheduleDraft>(newSchedule);
    const [scheduleFormVersion, setScheduleFormVersion] = useState(0);
    const [cancellingSchedule, setCancellingSchedule] = useState<IngestSchedule | null>(null);
    const saveInterface = useMutation({
        mutationFn: ({ code, input }: {
            code: string;
            input: InterfaceInput;
        }) => ingestConfigApi.saveInterface(code, input),
        onSuccess: () => {
            invalidateAfterWrite(client);
            setEditingInterface(null);
            setInterfaceDraft(null);
            toast.success(t('ingestConfig.urlSaved'));
        },
    });
    const saveSchedule = useMutation({
        mutationFn: ({ id, input }: {
            id: string | null;
            input: ScheduleInput;
        }) => ingestConfigApi.saveSchedule(id, input),
        onSuccess: () => {
            invalidateAfterWrite(client);
            setEditingSchedule(null);
            setScheduleDraft(newSchedule());
            setScheduleFormVersion(version => version + 1);
            toast.success(t('ingestConfig.scheduleSaved'));
        },
    });
    const toggleSchedule = useMutation({
        mutationFn: (row: IngestSchedule) => ingestConfigApi.saveSchedule(row.id, {
            name: row.name, intervalValue: row.intervalValue,
            intervalUnit: row.intervalUnit, enabled: !row.enabled,
            datasetCodes: datasetCodes(row), revision: row.revision, firstRunAt: null,
        }),
        onSuccess: (_, row) => {
            invalidateAfterWrite(client);
            if (editingSchedule === row.id)
                clearScheduleForm();
            toast.success(row.enabled ? t('ingestConfig.schedulePaused') : t('ingestConfig.scheduleResumed'));
        },
    });
    const cancelSchedule = useMutation({
        mutationFn: (row: IngestSchedule) => ingestConfigApi.cancelSchedule(row.id, row.revision),
        onSuccess: (_, row) => {
            invalidateAfterWrite(client);
            if (editingSchedule === row.id)
                clearScheduleForm();
            setCancellingSchedule(null);
            toast.success(t('ingestConfig.scheduleCancelled'));
        },
    });
    function editSchedule(row: IngestSchedule) {
        setEditingSchedule(row.id);
        setScheduleFormVersion(version => version + 1);
        setScheduleDraft({ name: row.name, intervalValue: row.intervalValue,
            intervalUnit: row.intervalUnit, enabled: row.enabled,
            datasetCodes: datasetCodes(row), revision: row.revision, firstRunAt: null });
        scheduleForm.current?.scrollIntoView({ block: 'start' });
    }
    function clearScheduleForm() {
        setEditingSchedule(null);
        setScheduleDraft(newSchedule());
        setScheduleFormVersion(version => version + 1);
    }
    const interfaces = rows(config?.interfaces);
    const schedules = rows(config?.schedules);
    const cancelledSchedules = cancelledQuery.data?.pages.flatMap(page => page.items) ?? [];
    const visibleCancelledSchedules = cancelledSchedules.slice(0, visibleCancelledCount);
    const canShowMoreCancelled = visibleCancelledCount < cancelledSchedules.length ||
        cancelledQuery.hasNextPage === true;
    async function showMoreCancelled() {
        if (loadingMoreCancelled.current || !canShowMoreCancelled)
            return;
        loadingMoreCancelled.current = true;
        try {
            if (visibleCancelledCount < cancelledSchedules.length) {
                setVisibleCancelledCount(count => count + 10);
            }
            else if (cancelledQuery.hasNextPage) {
                const result = await cancelledQuery.fetchNextPage();
                if (!result.isError)
                    setVisibleCancelledCount(count => count + 10);
            }
        }
        finally {
            loadingMoreCancelled.current = false;
        }
    }
    function handleCancelledScroll(event: UIEvent<HTMLDivElement>) {
        const list = event.currentTarget;
        if (list.scrollHeight <= list.clientHeight)
            return;
        if (list.scrollTop + list.clientHeight >= list.scrollHeight - list.clientHeight / 4) {
            void showMoreCancelled();
        }
    }
    function collapseCancelled() {
        setVisibleCancelledCount(10);
        if (cancelledList.current)
            cancelledList.current.scrollTop = 0;
    }
    const selectedDomainCount = interfaces.filter(row => scheduleDraft.datasetCodes.includes(row.code)).length;
    return <>
    <PageHeader title={t('ingestConfig.title')} description={t('ingestConfig.description', { hospital: config?.hospitalId ?? session?.hospitalId ?? '' })} breadcrumbs={[{ label: t('ingestConfig.home'), to: '/' }, { label: t('ingestConfig.title') }]}/>
    <p className="ida-ingest-note" role="status">
      {t('ingestConfig.mockNotice')}
    </p>
    {query.data && <p className={`ida-ingest-worker ${query.data.workerOnline ? 'ida-ingest-worker--online' : 'ida-ingest-worker--offline'}`} role="status">
      {query.data.workerOnline
                ? t('ingestConfig.workerOnline')
                : t('ingestConfig.workerOffline')}
    </p>}
    {!canRead && <p role="alert">{t('ingestConfig.accessDenied')}</p>}
    {query.isPending && canRead && <p role="status">{t('ingestConfig.loading')}</p>}
    <ApiErrorAlert error={query.error ?? saveInterface.error ?? saveSchedule.error ?? toggleSchedule.error} copy={errorCopy}/>

    {config && <>
      <section className="ida-card ida-ingest-section" aria-labelledby="ingest-schedule-title">
        <div className="ida-ingest-heading">
          <h2 id="ingest-schedule-title">{t('ingestConfig.scheduleHeading')}</h2>
          <p className="ida-text-secondary">{t('ingestConfig.scheduleHelp')}</p>
        </div>

        {canWrite && <form id="ingest-schedule-form" ref={scheduleForm} className="ida-ingest-schedule-form" onSubmit={event => {
                    event.preventDefault();
                    if (scheduleDraft.intervalValue === '')
                        return;
                    saveSchedule.mutate({ id: editingSchedule, input: {
                            ...scheduleDraft, intervalValue: scheduleDraft.intervalValue,
                        } });
                }}>
          <div className="ida-ingest-form-heading">
            <h3>{editingSchedule ? t('ingestConfig.editHeading') : t('ingestConfig.createHeading')}</h3>
            <button className="ida-btn ida-btn--ghost ida-btn--sm" type="button" onClick={clearScheduleForm}>
              {editingSchedule ? t('ingestConfig.cancelEdit') : t('ingestConfig.clearForm')}
            </button>
          </div>
          <div className="ida-ingest-fields">
            <label>{t('ingestConfig.name')}<input className="ida-input" required minLength={2} maxLength={100} placeholder={t('ingestConfig.nameExample')} value={scheduleDraft.name} onChange={event => setScheduleDraft({ ...scheduleDraft, name: event.target.value })}/></label>
            <label>{t('ingestConfig.interval')}<input className="ida-input" type="number" required min={1} max={31536000} value={scheduleDraft.intervalValue} onChange={event => setScheduleDraft({ ...scheduleDraft,
                    intervalValue: event.target.value === '' ? '' : Number(event.target.value) })}/></label>
            <label>{t('ingestConfig.unit')}<select className="ida-input" value={scheduleDraft.intervalUnit} onChange={event => setScheduleDraft({ ...scheduleDraft,
                    intervalUnit: event.target.value as ScheduleInput['intervalUnit'] })}>
              {units.map(unit => <option key={unit} value={unit}>{unitLabel(unit, t)}</option>)}</select></label>
            {locale === 'en'
                    ? <EnglishFirstRunInput key={scheduleFormVersion} value={scheduleDraft.firstRunAt} t={t} onChange={firstRunAt => setScheduleDraft(current => ({ ...current, firstRunAt }))}/>
                    : <label>{t('ingestConfig.firstRun')}<input className="ida-input" type="datetime-local" lang={locale} value={scheduleDraft.firstRunAt ? toLocalInput(scheduleDraft.firstRunAt) : ''} onChange={event => setScheduleDraft({ ...scheduleDraft,
                            firstRunAt: event.target.value
                                ? new Date(event.target.value).toISOString() : null })}/></label>}
          </div>
          <fieldset className="ida-ingest-domains">
            <legend>{t('ingestConfig.selectDomains')}</legend>
            <div className="ida-ingest-domain-toolbar">
              <span className="ida-ingest-domain-count" aria-live="polite">
                {t('ingestConfig.selectedDomains', { selected: selectedDomainCount, total: interfaces.length })}
              </span>
              <div className="ida-ingest-domain-actions">
                <button className="ida-btn ida-btn--secondary ida-btn--sm" type="button" disabled={interfaces.length === 0 || selectedDomainCount === interfaces.length} onClick={() => setScheduleDraft(current => ({
                    ...current,
                    datasetCodes: Array.from(new Set([
                        ...current.datasetCodes, ...interfaces.map(row => row.code),
                    ])),
                }))}>
                  {t('ingestConfig.selectAll')}
                </button>
                <button className="ida-btn ida-btn--ghost ida-btn--sm" type="button" disabled={scheduleDraft.datasetCodes.length === 0} onClick={() => setScheduleDraft(current => ({ ...current, datasetCodes: [] }))}>
                  {t('ingestConfig.clearSelected')}
                </button>
              </div>
            </div>
            {interfaces.map(row => <label key={row.code} className="ida-ingest-check">
              <input type="checkbox" checked={scheduleDraft.datasetCodes.includes(row.code)} onChange={event => setScheduleDraft({ ...scheduleDraft,
                        datasetCodes: event.target.checked
                            ? [...scheduleDraft.datasetCodes, row.code]
                            : scheduleDraft.datasetCodes.filter(code => code !== row.code) })}/>
              <span>{row.name} <code>{row.code}</code></span>
            </label>)}
          </fieldset>
          <div className="ida-ingest-actions">
            <label className="ida-ingest-check"><input type="checkbox" checked={scheduleDraft.enabled} onChange={event => setScheduleDraft({ ...scheduleDraft,
                    enabled: event.target.checked })}/> {t('ingestConfig.runByWorker')}</label>
            <button className="ida-btn ida-btn--primary" type="submit" disabled={saveSchedule.isPending || scheduleDraft.intervalValue === '' || scheduleDraft.datasetCodes.length === 0}>
              {editingSchedule ? t('ingestConfig.saveEdit') : t('ingestConfig.saveNew')}
            </button>
          </div>
          <p className="ida-caption">{t('ingestConfig.formHint')}</p>
        </form>}

        <div className="ida-ingest-saved">
          <h3>{t('ingestConfig.savedHeading')}</h3>
          <p className="ida-text-secondary">{t('ingestConfig.savedHelp')}</p>
          {schedules.length === 0 ?
                <p className="ida-text-secondary">{t('ingestConfig.noSchedules')}</p> :
                <div className="ida-table-wrap"><table className="ida-table"><thead><tr>
              <th>{t('ingestConfig.name')}</th><th>{t('ingestConfig.domain')}</th>
              <th>{t('ingestConfig.frequency')}</th><th>{t('ingestConfig.nextRun')}</th>
              <th>{t('ingestConfig.latestResult')}</th><th>{t('ingestConfig.operation')}</th>
              <th>{t('table.actions')}</th>
            </tr></thead><tbody>{schedules.map(row => <tr key={row.id}>
              <td><strong>{row.name}</strong></td><td>{datasetCodes(row).join(', ') || '—'}</td>
              <td>{t('ingestConfig.every', { value: row.intervalValue, unit: unitLabel(row.intervalUnit, t, row.intervalValue) })}</td>
              <td>{scheduledAt(row, locale)}</td>
              <td>{resultLabel(row.lastStatus, t)}{row.lastStatus && row.lastStartedAt ? ` · ${when(row.lastStartedAt, locale)}` : ''}</td>
              <td>{row.enabled
                            ? (config.workerOnline ? t('ingestConfig.enabled') : t('ingestConfig.waitingForWorker'))
                            : t('ingestConfig.paused')}</td>
              <td>{canWrite && <div className="ida-ingest-row-actions">
                <button className="ida-btn ida-btn--secondary ida-btn--sm" type="button" onClick={() => editSchedule(row)}>{t('action.edit')}</button>
                <button className="ida-btn ida-btn--secondary ida-btn--sm" type="button" disabled={toggleSchedule.isPending || cancelSchedule.isPending} onClick={() => toggleSchedule.mutate(row)}>
                  {row.enabled ? t('ingestConfig.paused') : t('ingestConfig.resume')}
                </button>
                <button className="ida-btn ida-btn--ghost ida-btn--sm ida-ingest-cancel-button" type="button" disabled={toggleSchedule.isPending || cancelSchedule.isPending} onClick={() => setCancellingSchedule(row)}>{t('ingestConfig.cancelSchedule')}</button>
              </div>}</td>
            </tr>)}</tbody></table></div>}
          {cancelledCount > 0 && <details className="ida-ingest-cancelled" onToggle={event => {
                    setCancelledOpen(event.currentTarget.open);
                    if (!event.currentTarget.open)
                        collapseCancelled();
                }}>
            <summary>{t('ingestConfig.cancelledHeading', { count: cancelledCount })}</summary>
            {cancelledQuery.isPending && <p className="ida-text-secondary">{t('ingestConfig.loadingCancelled')}</p>}
            {cancelledQuery.isError && <ApiErrorAlert error={cancelledQuery.error} copy={errorCopy}/>}
            {visibleCancelledSchedules.length > 0 && <div ref={cancelledList} className="ida-table-wrap ida-ingest-cancelled-list" role="region" aria-label={t('ingestConfig.cancelledRegion')} tabIndex={0} onScroll={handleCancelledScroll}><table className="ida-table"><thead><tr>
              <th>{t('ingestConfig.name')}</th><th>{t('ingestConfig.domain')}</th>
              <th>{t('ingestConfig.cancelledAt')}</th><th>{t('ingestConfig.lastBeforeCancel')}</th>
            </tr></thead><tbody>{visibleCancelledSchedules.map(row => <tr key={row.id}>
              <td>{row.name}</td><td>{datasetCodes(row).join(', ') || '—'}</td>
              <td>{row.cancelledAt ? when(row.cancelledAt, locale) : '—'}</td>
              <td>{resultLabel(row.lastStatus, t)}</td>
            </tr>)}</tbody></table></div>}
            {visibleCancelledSchedules.length > 0 && <div className="ida-ingest-cancelled-actions">
              {canShowMoreCancelled && <button className="ida-btn ida-btn--secondary ida-btn--sm" type="button" disabled={cancelledQuery.isFetchingNextPage} onClick={() => void showMoreCancelled()}>
                {cancelledQuery.isFetchingNextPage ? t('ingestConfig.loadingMore') : t('ingestConfig.showMore')}
              </button>}
              {visibleCancelledSchedules.length > 10 && <button className="ida-btn ida-btn--ghost ida-btn--sm" type="button" onClick={collapseCancelled}>{t('ingestConfig.collapse')}</button>}
            </div>}
          </details>}
        </div>
      </section>

      <section className="ida-card ida-ingest-section" aria-labelledby="ingest-domain-title">
        <div className="ida-ingest-heading">
          <h2 id="ingest-domain-title">{t('ingestConfig.domainsHeading')}</h2>
          <p className="ida-text-secondary">{t('ingestConfig.domainsHelp')}</p>
        </div>
        <div className="ida-table-wrap"><table className="ida-table">
          <thead><tr><th>{t('ingestConfig.sourceDomain')}</th><th>{t('ingestConfig.sourceUrl')}</th><th>{t('ingestConfig.manageUrl')}</th></tr></thead>
          <tbody>{interfaces.map(row => <tr key={row.code}>
            <td><strong>{row.name}</strong><br /><code>{row.sourceSystem} · {row.code}</code></td>
            <td>{row.endpointUrl || t('ingestConfig.urlUnset')}</td>
            <td>{canWrite && <button className="ida-btn ida-btn--secondary ida-btn--sm" type="button" onClick={() => {
                        setEditingInterface(row.code);
                        setInterfaceDraft({ endpointUrl: row.endpointUrl });
                    }}>{t('ingestConfig.editUrl')}</button>}</td>
          </tr>)}</tbody>
        </table></div>
        {editingInterface && interfaceDraft && <form className="ida-ingest-editor" onSubmit={event => {
                    event.preventDefault();
                    saveInterface.mutate({ code: editingInterface, input: interfaceDraft });
                }}>
          <h3>{t('ingestConfig.urlFor', { code: editingInterface })}</h3>
          <div className="ida-ingest-fields">
            <label>{t('ingestConfig.sourceUrl')}<input className="ida-input" type="url" placeholder="https://…" value={interfaceDraft.endpointUrl ?? ''} onChange={event => setInterfaceDraft({ endpointUrl: event.target.value || null })}/></label>
          </div>
          <div className="ida-ingest-actions">
            <button className="ida-btn ida-btn--primary" disabled={saveInterface.isPending}>{t('ingestConfig.saveUrl')}</button>
            <button className="ida-btn ida-btn--ghost" type="button" onClick={() => { setEditingInterface(null); setInterfaceDraft(null); }}>{t('action.cancel')}</button>
          </div>
        </form>}
      </section>
      <ConfirmDialog open={cancellingSchedule !== null} title={t('ingestConfig.confirmTitle', { name: cancellingSchedule?.name ?? '' })} confirmLabel={t('ingestConfig.cancelSchedule')} cancelLabel={t('ingestConfig.back')} tone="danger" busy={cancelSchedule.isPending} onCancel={() => { cancelSchedule.reset(); setCancellingSchedule(null); }} onConfirm={() => { if (cancellingSchedule)
            cancelSchedule.mutate(cancellingSchedule); }}>
        {t('ingestConfig.confirmHelp')}
        {cancelSchedule.error && <ApiErrorAlert error={cancelSchedule.error} copy={errorCopy}/>}
      </ConfirmDialog>
    </>}
  </>;
}
