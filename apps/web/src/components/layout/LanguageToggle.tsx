import { usePreferences } from '../../app/preferences';
import { useT } from '../../i18n/useT';
import type { Locale } from '../../app/preferences';
export function LanguageToggle() {
    const t = useT();
    const { locale, setLocale } = usePreferences();
    const next: Locale = locale === 'th' ? 'en' : 'th';
    return (<button type="button" className="ida-lang-btn" onClick={() => setLocale(next)} aria-label={`${t('prefs.language')}: ${t(`prefs.language.${locale}`)} — ${t('prefs.language.switchTo', { language: t(`prefs.language.${next}`) })}`}>
      
      <img className="ida-lang-btn__flag" src={`/icons/flag-${locale}.png`} alt="" aria-hidden/>
      <span className="ida-lang-btn__code">{locale.toUpperCase()}</span>
    </button>);
}
