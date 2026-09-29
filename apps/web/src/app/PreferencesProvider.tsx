import { useCallback, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { LOCALE_KEY, PreferencesContext, THEME_KEY, readStored, writeStored, } from './preferences';
import type { Locale, ThemeChoice } from './preferences';
const THEMES: readonly ThemeChoice[] = ['system', 'light', 'dark'];
const LOCALES: readonly Locale[] = ['th', 'en'];
const THEME_COLOR: Record<'light' | 'dark', string> = {
    light: '#f5f8fb',
    dark: '#0b1b2b',
};
export function PreferencesProvider({ children }: {
    children: ReactNode;
}) {
    const [theme, setThemeState] = useState<ThemeChoice>(() => readStored(THEME_KEY, THEMES, 'system'));
    const [locale, setLocaleState] = useState<Locale>(() => readStored(LOCALE_KEY, LOCALES, 'th'));
    const [systemDark, setSystemDark] = useState(() => window.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false);
    useEffect(() => {
        const query = window.matchMedia?.('(prefers-color-scheme: dark)');
        if (!query)
            return;
        const onChange = (e: MediaQueryListEvent) => setSystemDark(e.matches);
        query.addEventListener('change', onChange);
        return () => query.removeEventListener('change', onChange);
    }, []);
    const resolvedTheme = theme === 'system' ? (systemDark ? 'dark' : 'light') : theme;
    useEffect(() => {
        document.documentElement.setAttribute('data-theme', resolvedTheme);
        document
            .querySelector('meta[name="theme-color"]')
            ?.setAttribute('content', THEME_COLOR[resolvedTheme]);
    }, [resolvedTheme]);
    useEffect(() => {
        document.documentElement.setAttribute('lang', locale);
    }, [locale]);
    const setTheme = useCallback((next: ThemeChoice) => {
        setThemeState(next);
        writeStored(THEME_KEY, next);
    }, []);
    const setLocale = useCallback((next: Locale) => {
        setLocaleState(next);
        writeStored(LOCALE_KEY, next);
    }, []);
    const value = useMemo(() => ({ theme, resolvedTheme, setTheme, locale, setLocale }), [theme, resolvedTheme, setTheme, locale, setLocale]);
    return <PreferencesContext.Provider value={value}>{children}</PreferencesContext.Provider>;
}
