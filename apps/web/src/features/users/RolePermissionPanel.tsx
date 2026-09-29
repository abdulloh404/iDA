import { useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '../../api/client';
import { Icon } from '../../components/Icon';
import { ApiErrorAlert } from '../../components/feedback/ApiErrorAlert';
import { useToast } from '../../components/feedback/toastContext';
import { invalidateAfterWrite } from '../../app/queryClient';
import { usePermission } from '../auth/authState';
import type { RoleDetail } from './role';
interface PermissionItem {
    code: string;
    resource: string;
    action: string;
    module: string;
    nameTh: string;
    isGroupLevel: boolean;
    granted: boolean;
    assignable: boolean;
}
interface RolePermissions {
    roleId: string;
    roleCode: string;
    isGroupLevel: boolean;
    lockedReason: string | null;
    permissions: PermissionItem[];
}
const COLUMNS = [
    { action: 'read', label: 'ดูรายการ' },
    { action: 'write', label: 'สร้าง / แก้ไข' },
    { action: 'delete', label: 'ลบ' },
    { action: 'export', label: 'Export' },
] as const;
const STANDARD = new Set<string>(COLUMNS.map((c) => c.action));
const MODULES: readonly (readonly [
    string,
    string
])[] = [
    ['approvals', 'คำขอและการอนุมัติ'],
    ['master-data-general', 'ข้อมูลหลักทั่วไป'],
    ['master-data-accounting', 'ข้อมูลหลักทางบัญชี'],
    ['master-data-tax-402', 'ข้อมูลหลัก 40(2)'],
    ['master-data-tax-406', 'ข้อมูลหลัก 40(6)'],
    ['master-data-doctor', 'จัดการข้อมูลแพทย์'],
    ['doctors', 'จัดการข้อมูลแพทย์'],
    ['share-rates', 'ส่วนแบ่งค่าแพทย์'],
    ['duty-rates', 'อัตราค่าเวรและประกันรายได้'],
    ['duty-schedules', 'ตารางเวรและการลงชื่อเข้าเวร'],
    ['doctor-fee-402', 'จัดการค่าแพทย์ 40(2)'],
    ['doctor-fee-406', 'จัดการค่าแพทย์ 40(6)'],
    ['income-documents', 'เอกสารรายได้'],
    ['admin', 'จัดการผู้ใช้'],
    ['system-settings', 'ตั้งค่าระบบ'],
];
const MODULE_ORDER = new Map(MODULES.map(([key], index) => [key, index]));
const MODULE_LABEL = new Map(MODULES);
const ACTION_PREFIXES = ['ดูข้อมูล', 'บันทึกข้อมูล', 'ลบข้อมูล', 'Export ', 'ดู', 'บันทึก', 'ลบ'];
function resourceLabel(items: PermissionItem[]): string {
    const read = items.find((p) => p.action === 'read') ?? items[0];
    const prefix = ACTION_PREFIXES.find((p) => read.nameTh.startsWith(p));
    return prefix ? read.nameTh.slice(prefix.length).trim() : read.nameTh;
}
interface ResourceRow {
    resource: string;
    label: string;
    byAction: Map<string, PermissionItem>;
    extras: PermissionItem[];
}
interface ModuleGroup {
    module: string;
    label: string;
    rows: ResourceRow[];
}
function groupPermissions(items: PermissionItem[]): ModuleGroup[] {
    const home = new Map<string, string>();
    for (const item of items)
        if (item.action === 'read' || !home.has(item.resource))
            home.set(item.resource, item.module);
    const modules = new Map<string, Map<string, PermissionItem[]>>();
    for (const item of items) {
        const module = home.get(item.resource) ?? item.module;
        const resources = modules.get(module) ?? new Map<string, PermissionItem[]>();
        resources.set(item.resource, [...(resources.get(item.resource) ?? []), item]);
        modules.set(module, resources);
    }
    return [...modules.entries()]
        .sort(([a], [b]) => (MODULE_ORDER.get(a) ?? 999) - (MODULE_ORDER.get(b) ?? 999) || a.localeCompare(b))
        .map(([module, resources]) => ({
        module,
        label: MODULE_LABEL.get(module) ?? module,
        rows: [...resources.entries()]
            .map(([resource, perms]) => ({
            resource,
            label: resourceLabel(perms),
            byAction: new Map(perms.filter((p) => STANDARD.has(p.action)).map((p) => [p.action, p])),
            extras: perms.filter((p) => !STANDARD.has(p.action)),
        }))
            .sort((a, b) => a.label.localeCompare(b.label, 'th')),
    }));
}
export function RolePermissionPanel({ id, detail }: {
    id: string;
    detail: RoleDetail;
    readOnly: boolean;
}) {
    const canWrite = usePermission('roles.write');
    const queryClient = useQueryClient();
    const toast = useToast();
    const [draft, setDraft] = useState<Set<string> | null>(null);
    const [filter, setFilter] = useState('');
    const query = useQuery({
        queryKey: ['master', 'roles', id, 'permissions'],
        queryFn: ({ signal }) => api<RolePermissions>(`/api/master-data/roles/${id}/permissions`, { signal }),
    });
    const saved = useMemo(() => new Set(query.data?.permissions.filter((p) => p.granted).map((p) => p.code) ?? []), [query.data]);
    const selected = draft ?? saved;
    const groups = useMemo(() => groupPermissions(query.data?.permissions ?? []), [query.data]);
    const save = useMutation({
        mutationFn: () => api<RolePermissions>(`/api/master-data/roles/${id}/permissions`, {
            method: 'PUT',
            body: { codes: [...selected] },
            params: { rowVersion: detail.rowVersion },
        }),
        onSuccess: () => {
            setDraft(null);
            invalidateAfterWrite(queryClient);
            toast.success('บันทึกสิทธิ์การใช้งานเรียบร้อยแล้ว — ผู้ใช้เห็นเมนูใหม่ภายในไม่กี่นาที');
        },
    });
    const locked = query.data?.lockedReason ?? null;
    const editable = canWrite && locked === null && !save.isPending;
    const changes = useMemo(() => {
        if (!draft)
            return 0;
        let n = 0;
        for (const code of draft)
            if (!saved.has(code))
                n++;
        for (const code of saved)
            if (!draft.has(code))
                n++;
        return n;
    }, [draft, saved]);
    const toggle = (row: ResourceRow, item: PermissionItem, on: boolean) => {
        const next = new Set(selected);
        const read = row.byAction.get('read');
        if (on) {
            next.add(item.code);
            if (read && read.assignable)
                next.add(read.code);
        }
        else {
            next.delete(item.code);
            if (item.action === 'read') {
                for (const p of [...row.byAction.values(), ...row.extras])
                    next.delete(p.code);
            }
        }
        setDraft(next);
    };
    const toggleColumn = (group: ModuleGroup, action: string, on: boolean) => {
        const next = new Set(selected);
        for (const row of group.rows) {
            const item = row.byAction.get(action);
            if (!item || !item.assignable)
                continue;
            if (on) {
                next.add(item.code);
                const read = row.byAction.get('read');
                if (read?.assignable)
                    next.add(read.code);
            }
            else {
                next.delete(item.code);
                if (action === 'read')
                    for (const p of [...row.byAction.values(), ...row.extras])
                        next.delete(p.code);
            }
        }
        setDraft(next);
    };
    const keyword = filter.trim().toLowerCase();
    const visibleGroups = keyword
        ? groups
            .map((g) => ({
            ...g,
            rows: g.rows.filter((r) => r.label.toLowerCase().includes(keyword) || g.label.toLowerCase().includes(keyword)),
        }))
            .filter((g) => g.rows.length > 0)
        : groups;
    return (<section className="ida-card">
      <h2 className="ida-card__title">สิทธิ์การเข้าถึงเมนู</h2>

      {query.error && <ApiErrorAlert error={query.error}/>}
      {query.isPending && <span className="ida-skeleton" style={{ height: '240px', display: 'block' }}/>}

      {locked && (<div className="ida-alert ida-alert--info" role="status" style={{ marginBottom: 'var(--ida-space-4)' }}>
          <div className="ida-alert__body">
            <Icon name="info" size={18}/>
            <span>{locked}</span>
          </div>
        </div>)}

      {query.data && !locked && !query.data.isGroupLevel && (<p className="ida-text-secondary" style={{ marginTop: 0 }}>
          สิทธิ์การใช้งานระดับโรงพยาบาลดูและ Export ข้อมูลหลักของทั้งเครือได้ แต่แก้หรือลบไม่ได้ —
          ช่องเหล่านั้นจึงติ๊กไม่ได้
        </p>)}

      {query.data && (<>
          <div className="ida-perm-toolbar">
            <label className="ida-visually-hidden" htmlFor={`perm-filter-${id}`}>
              ค้นหาเมนู
            </label>
            <div className="ida-search">
              <Icon name="search" size={18} className="ida-search__icon"/>
              <input id={`perm-filter-${id}`} className="ida-search__input" type="search" placeholder="ค้นหาเมนู…" value={filter} onChange={(event) => setFilter(event.target.value)}/>
            </div>
            <span className="ida-caption ida-text-secondary">
              เลือกไว้ {selected.size.toLocaleString('th-TH')} จาก{' '}
              {query.data.permissions.length.toLocaleString('th-TH')} สิทธิ์
            </span>
          </div>

          <div className="ida-table-wrap">
            <table className="ida-table ida-perm-table">
              <caption className="ida-visually-hidden">สิทธิ์ของ {detail.nameTh} แยกตามเมนู</caption>
              <thead>
                <tr>
                  <th scope="col">เมนู</th>
                  {COLUMNS.map((c) => (<th scope="col" key={c.action} className="ida-perm-table__check">
                      {c.label}
                    </th>))}
                  <th scope="col">อื่น ๆ</th>
                </tr>
              </thead>
              {visibleGroups.map((group) => (<tbody key={group.module}>
                  <tr className="ida-perm-table__group">
                    <th scope="row">{group.label}</th>
                    {COLUMNS.map((c) => {
                    const items = group.rows
                        .map((r) => r.byAction.get(c.action))
                        .filter((p): p is PermissionItem => !!p && p.assignable);
                    const on = items.filter((p) => selected.has(p.code)).length;
                    return (<td key={c.action} className="ida-perm-table__check">
                          {items.length > 0 && (<CheckBox label={`${c.label} ทุกเมนูใน${group.label}`} checked={on === items.length} indeterminate={on > 0 && on < items.length} disabled={!editable} onChange={(v) => toggleColumn(group, c.action, v)}/>)}
                        </td>);
                })}
                    <td />
                  </tr>
                  {group.rows.map((row) => (<tr key={row.resource}>
                      <th scope="row" className="ida-perm-table__name">
                        {row.label}
                      </th>
                      {COLUMNS.map((c) => {
                        const item = row.byAction.get(c.action);
                        return (<td key={c.action} className="ida-perm-table__check">
                            {item && (<CheckBox label={`${c.label} ${row.label}`} checked={selected.has(item.code)} disabled={!editable || !item.assignable} title={item.assignable ? undefined : 'เฉพาะสิทธิ์การใช้งานระดับเครือ'} onChange={(v) => toggle(row, item, v)}/>)}
                          </td>);
                    })}
                      <td>
                        <div className="ida-perm-table__extras">
                          {row.extras.map((item) => (<label key={item.code} className="ida-checkbox ida-perm-extra">
                              <input type="checkbox" checked={selected.has(item.code)} disabled={!editable || !item.assignable} onChange={(event) => toggle(row, item, event.target.checked)}/>
                              <span className="ida-checkbox__box" aria-hidden="true">
                                <Icon name="check" size={14}/>
                              </span>
                              <span>{item.nameTh}</span>
                            </label>))}
                        </div>
                      </td>
                    </tr>))}
                </tbody>))}
            </table>
          </div>

          {visibleGroups.length === 0 && (<p className="ida-text-secondary">ไม่พบเมนูที่ตรงกับ “{filter}”</p>)}

          {save.error != null && (<div style={{ marginTop: 'var(--ida-space-4)' }}>
              <ApiErrorAlert error={save.error}/>
            </div>)}

          {canWrite && !locked && (<div className="ida-form-actions ida-form-actions--inline">
              <p className="ida-form-actions__note" role="status">
                {changes > 0 ? `แก้ไขแล้ว ${changes.toLocaleString('th-TH')} ช่อง ยังไม่ได้บันทึก` : 'ยังไม่มีการเปลี่ยนแปลง'}
              </p>
              <button type="button" className="ida-btn ida-btn--secondary" disabled={changes === 0 || save.isPending} onClick={() => setDraft(null)}>
                คืนค่าเดิม
              </button>
              <button type="button" className="ida-btn ida-btn--primary" disabled={changes === 0 || save.isPending} onClick={() => save.mutate()}>
                {save.isPending ? (<span className="ida-spinner" aria-hidden="true"/>) : (<Icon name="check" size={18}/>)}
                บันทึกสิทธิ์
              </button>
            </div>)}
        </>)}
    </section>);
}
function CheckBox({ label, checked, indeterminate = false, disabled, title, onChange, }: {
    label: string;
    checked: boolean;
    indeterminate?: boolean;
    disabled: boolean;
    title?: string;
    onChange: (checked: boolean) => void;
}) {
    return (<label className="ida-checkbox ida-checkbox--bare" title={title}>
      <input type="checkbox" aria-label={label} checked={checked} disabled={disabled} ref={(el) => {
            if (el)
                el.indeterminate = indeterminate;
        }} onChange={(event) => onChange(event.target.checked)}/>
      <span className="ida-checkbox__box" aria-hidden="true">
        <Icon name="check" size={14}/>
      </span>
    </label>);
}
