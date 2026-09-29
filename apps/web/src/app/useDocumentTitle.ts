import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';
import { MENU } from '../nav/menu';
import { MASTER_DATA_SCREENS } from '../features/master-data/registry';
import { usePreferences } from './preferences';
import { pickName } from '../i18n/useT';
const PRODUCT = 'iDA';
export function useDocumentTitle() {
    const { pathname } = useLocation();
    const { locale } = usePreferences();
    useEffect(() => {
        const title = titleFor(pathname, locale);
        document.title = title ? `${title} · ${PRODUCT}` : PRODUCT;
    }, [pathname, locale]);
}
function titleFor(pathname: string, locale: 'th' | 'en'): string | null {
    const screens = [...MASTER_DATA_SCREENS].sort((a, b) => b.path.length - a.path.length);
    const screen = screens.find((s) => pathname === s.path || pathname.startsWith(`${s.path}/`));
    if (screen)
        return pickName(locale, screen.titleTh, screen.titleEn);
    for (const group of MENU) {
        for (const item of group.items) {
            if (item.path && pathname === item.path)
                return pickName(locale, item.label, item.labelEn);
        }
    }
    return null;
}
