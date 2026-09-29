import { createContext, useContext } from 'react';
export type ThemeChoice = 'system' | 'light' | 'dark';
export type Locale = 'th' | 'en';
export const THEME_KEY = 'ida.theme';
export const LOCALE_KEY = 'ida.locale';
export interface Preferences {
    theme: ThemeChoice;
    resolvedTheme: 'light' | 'dark';
    setTheme: (theme: ThemeChoice) => void;
    locale: Locale;
    setLocale: (locale: Locale) => void;
}
export const PreferencesContext = createContext<Preferences | null>(null);
export function usePreferences(): Preferences {
    const value = useContext(PreferencesContext);
    if (!value)
        throw new Error('usePreferences ต้องอยู่ใต้ PreferencesProvider');
    return value;
}
export function readStored<T extends string>(key: string, allowed: readonly T[], fallback: T): T {
    try {
        const raw = localStorage.getItem(key);
        return allowed.includes(raw as T) ? (raw as T) : fallback;
    }
    catch {
        return fallback;
    }
}
export function writeStored(key: string, value: string): void {
    try {
        localStorage.setItem(key, value);
    }
    catch {
    }
}
