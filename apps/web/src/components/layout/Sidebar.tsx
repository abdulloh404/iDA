import { useMemo, useState } from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import { useAuth } from '../../features/auth/authState';
import { usePreferences } from '../../app/preferences';
import { pickName } from '../../i18n/useT';
import { resolveMenu } from '../../nav/menu';
import type { ResolvedMenuItem } from '../../nav/menu';
import { GROUP_ICONS } from '../../nav/icons';
import { useOverflowTitle } from '../../hooks/useOverflowTitle';
import { Icon } from '../Icon';
type DisplayMenuItem = ResolvedMenuItem & {
    displayLabel: string;
};
export function Sidebar({ collapsed, onNavigate, }: {
    collapsed: boolean;
    onNavigate?: () => void;
}) {
    const { can, session } = useAuth();
    const { locale } = usePreferences();
    const location = useLocation();
    const [filter, setFilter] = useState('');
    const [manuallyToggled, setManuallyToggled] = useState<Record<string, boolean>>({});
    const groups = useMemo(() => {
        const query = filter.trim().toLowerCase();
        return resolveMenu()
            .map((group) => ({
            label: group.label,
            displayLabel: pickName(locale, group.label, group.labelEn),
            items: group.items.filter((item) => {
                if (item.requires && !can(item.requires))
                    return false;
                if (item.allowedRoles && !item.allowedRoles.some(role => session?.roles.includes(role)))
                    return false;
                if (!query)
                    return true;
                return (item.label.toLowerCase().includes(query) ||
                    item.labelEn?.toLowerCase().includes(query) ||
                    group.label.toLowerCase().includes(query) ||
                    group.labelEn?.toLowerCase().includes(query) ||
                    item.figure === query);
            }).map(item => ({ ...item, displayLabel: pickName(locale, item.label, item.labelEn) })),
        }))
            .filter((group) => group.items.length > 0);
    }, [filter, can, session?.roles, locale]);
    const activeGroup = groups.find((group) => group.items.some((item) => item.to && isActivePath(location.pathname, item.to)))?.label;
    const searching = filter.trim().length > 0;
    const isOpen = (label: string) => searching || (manuallyToggled[label] ?? label === activeGroup);
    if (collapsed) {
        return (<nav className="ida-nav ida-nav--rail" aria-label="เมนูหลัก">
        {groups
                .filter((group) => group.items.some((i) => i.to))
                .map((group) => {
                const first = group.items.find((i) => i.to)!;
                return (<NavLink key={group.label} to={first.to!} className={({ isActive }) => `ida-nav__rail-item${isActive ? ' ida-nav__rail-item--active' : ''}`} title={group.displayLabel} aria-label={group.displayLabel} onClick={onNavigate}>
                <Icon name={GROUP_ICONS[group.label] ?? 'layers'}/>
              </NavLink>);
            })}
      </nav>);
    }
    return (<nav className="ida-nav" aria-label="เมนูหลัก">
      <div className="ida-nav__search">
        <Icon name="search" size={16} className="ida-nav__search-icon"/>
        <label className="ida-visually-hidden" htmlFor="menu-filter">
          ค้นหาเมนู
        </label>
        <input id="menu-filter" className="ida-nav__search-input" type="search" value={filter} onChange={(e) => setFilter(e.target.value)} placeholder="ค้นหาเมนู…"/>
      </div>

      <div className="ida-nav__groups">
        {groups.map((group) => (<Group key={group.label} label={group.label} displayLabel={group.displayLabel} items={group.items} open={isOpen(group.label)} current={group.label === activeGroup} onToggle={() => setManuallyToggled((state) => ({
                ...state,
                [group.label]: !isOpen(group.label),
            }))} onNavigate={onNavigate}/>))}

        {groups.length === 0 && (<p className="ida-nav__empty">ไม่พบเมนูที่ตรงกับ “{filter}”</p>)}
      </div>
    </nav>);
}
function Group({ label, displayLabel, items, open, current, onToggle, onNavigate, }: {
    label: string;
    displayLabel: string;
    items: readonly DisplayMenuItem[];
    open: boolean;
    current: boolean;
    onToggle: () => void;
    onNavigate?: () => void;
}) {
    const { ref: labelRef, title: labelTitle } = useOverflowTitle<HTMLSpanElement>(displayLabel);
    return (<div className="ida-nav__group">
      <button type="button" className={'ida-nav__group-toggle' +
            (open ? ' ida-nav__group-toggle--open' : '') +
            (current ? ' ida-nav__group-toggle--current' : '')} aria-expanded={open} onClick={onToggle} title={labelTitle}>
        <Icon name={GROUP_ICONS[label] ?? 'layers'} size={18} className="ida-nav__group-icon"/>
        <span className="ida-nav__group-label" ref={labelRef}>
          {displayLabel}
        </span>
        <Icon name={open ? 'chevronDown' : 'chevronRight'} size={14} className="ida-nav__group-caret"/>
      </button>

      {open && (<div className="ida-nav__items">
          {items.map((item) => (<Item key={item.figure} item={item} onNavigate={onNavigate}/>))}
        </div>)}
    </div>);
}
function Item({ item, onNavigate }: {
    item: DisplayMenuItem;
    onNavigate?: () => void;
}) {
    const { ref: labelRef, title: labelTitle } = useOverflowTitle<HTMLSpanElement>(item.displayLabel);
    if (!item.to) {
        return (<span className="ida-nav__item ida-nav__item--pending" aria-disabled="true" title={labelTitle ? `${labelTitle} · ยังไม่เปิดใช้งาน` : 'ยังไม่เปิดใช้งาน'}>
        <span className="ida-nav__item-label" ref={labelRef}>
          {item.displayLabel}
        </span>
      </span>);
    }
    return (<NavLink to={item.to} className={({ isActive }) => `ida-nav__item${isActive ? ' ida-nav__item--active' : ''}`} onClick={onNavigate} title={labelTitle}>
      <span className="ida-nav__item-label" ref={labelRef}>
        {item.displayLabel}
      </span>
    </NavLink>);
}
function isActivePath(pathname: string, to: string): boolean {
    return to === '/' ? pathname === '/' : pathname.startsWith(to);
}
