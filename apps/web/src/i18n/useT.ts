import { useCallback } from 'react';
import { usePreferences } from '../app/preferences';
import { STRINGS } from './strings';
import type { StringKey } from './strings';
export type Translate = (key: StringKey, vars?: Record<string, string | number>) => string;
export function useT(): Translate {
    const { locale } = usePreferences();
    return useCallback((key, vars) => {
        const table = STRINGS[locale] as Record<string, string>;
        let text = table[key] ?? STRINGS.th[key] ?? key;
        if (vars) {
            for (const [name, value] of Object.entries(vars))
                text = text.replaceAll(`{${name}}`, String(value));
        }
        return text;
    }, [locale]);
}
export function pickName(locale: 'th' | 'en', th: string, en?: string | null): string {
    return locale === 'en' && en ? en : th;
}
