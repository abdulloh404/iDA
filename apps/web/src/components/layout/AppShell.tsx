import { useEffect, useState } from 'react';
import { Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../../features/auth/authState';
import { useMediaQuery } from '../../hooks/useMediaQuery';
import { Select } from '../form/Select';
import { Icon } from '../Icon';
import { ErrorBoundary } from '../feedback/ErrorBoundary';
import { Sidebar } from './Sidebar';
import { UserMenu } from './UserMenu';
import { ThemeToggle } from './ThemeToggle';
import { LanguageToggle } from './LanguageToggle';
import { useT } from '../../i18n/useT';
import { useDocumentTitle } from '../../app/useDocumentTitle';
export function AppShell() {
    const { session, signOut, changeHospital } = useAuth();
    const location = useLocation();
    const t = useT();
    useDocumentTitle();
    const [collapsed, setCollapsed] = useState(false);
    const [drawer, setDrawer] = useState({ open: false, at: location.pathname });
    const drawerOpen = drawer.open && drawer.at === location.pathname;
    const setDrawerOpen = (open: boolean) => setDrawer({ open, at: location.pathname });
    const railWidth = useMediaQuery('(min-width: 768px) and (max-width: 1023px)');
    const rail = (collapsed || railWidth) && !drawerOpen;
    useEffect(() => {
        if (!drawerOpen)
            return;
        const onKeyDown = (e: KeyboardEvent) => {
            if (e.key === 'Escape')
                setDrawer((current) => ({ ...current, open: false }));
        };
        document.addEventListener('keydown', onKeyDown);
        return () => document.removeEventListener('keydown', onKeyDown);
    }, [drawerOpen]);
    const hospital = session?.hospitals.find((h) => h.hospitalId === session.hospitalId);
    return (<>
      <div className="ida-brand-bar"/>
      <div className="ida-shell">
        
        <aside className={[
            'ida-sidebar',
            rail ? 'ida-sidebar--collapsed' : '',
            drawerOpen ? 'ida-sidebar--open' : '',
        ]
            .filter(Boolean)
            .join(' ')}>
          <div className="ida-sidebar__brand">
            
            <div className={[
            'ida-logo',
            'ida-logo--reversed',
            rail ? 'ida-logo--mark ida-brand__mark' : 'ida-logo--lockup ida-brand__lockup',
        ].join(' ')} role="img" aria-label={rail ? 'iDA' : 'iDA · Intelligent Doctor Application'}/>

            <button type="button" className="ida-icon-btn ida-icon-btn--on-dark ida-sidebar__close" onClick={() => setDrawerOpen(false)} aria-label={t('shell.closeMenu')}>
              <Icon name="close"/>
            </button>
          </div>

          <Sidebar collapsed={rail} onNavigate={() => setDrawerOpen(false)}/>
        </aside>

        
        {drawerOpen && (<div className="ida-sidebar__scrim" onClick={() => setDrawerOpen(false)} aria-hidden="true"/>)}

        <div className="ida-shell__body">
          <header className="ida-topbar">
            
            <button type="button" className="ida-icon-btn ida-topbar__collapse" onClick={() => setCollapsed((c) => !c)} aria-label={collapsed ? t('shell.expandMenu') : t('shell.collapseMenu')} aria-expanded={!collapsed}>
              <Icon name="menu"/>
            </button>
            <button type="button" className="ida-icon-btn ida-topbar__drawer-toggle" onClick={() => setDrawerOpen(true)} aria-label={t('shell.openMenu')} aria-expanded={drawerOpen}>
              <Icon name="menu"/>
            </button>

            
            <div className="ida-hospital-switcher">
              <Icon name="hospital" size={18} className="ida-hospital-switcher__icon"/>
              <Select id="hospital-switcher" ariaLabel={t('shell.hospital')} className="ida-select-trigger--bare" value={session?.hospitalId ?? ''} onChange={(id) => void changeHospital(id)} options={(session?.hospitals ?? []).map((h) => ({
            value: h.hospitalId,
            label: h.hospitalName,
        }))}/>
            </div>

            <span className="ida-topbar__spacer"/>

            
            <div className="ida-topbar__prefs">
              <LanguageToggle />
              <ThemeToggle />
            </div>

            <UserMenu displayName={session?.user.displayName} roleNameTh={hospital?.roleNameTh} initials={initials(session?.user.displayName)} onSignOut={signOut}/>
          </header>

          <main className="ida-shell__main" id="main">
            <div className="ida-shell__content">
              
              <ErrorBoundary resetKey={location.pathname}>
                <Outlet />
              </ErrorBoundary>
            </div>
          </main>
        </div>
      </div>
    </>);
}
function initials(name: string | undefined): string {
    if (!name)
        return '·';
    return name
        .trim()
        .split(/\s+/)
        .slice(0, 2)
        .map((word) => [...word][0] ?? '')
        .join('');
}
