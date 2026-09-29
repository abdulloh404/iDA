import * as DropdownMenu from '@radix-ui/react-dropdown-menu';
import { Icon } from '../Icon';
import { usePreferences } from '../../app/preferences';
import type { ThemeChoice } from '../../app/preferences';
import { useT } from '../../i18n/useT';
import type { StringKey } from '../../i18n/strings';
import type { IconName } from '../Icon';
export function ThemeToggle() {
    const t = useT();
    const { theme, resolvedTheme, setTheme } = usePreferences();
    return (<DropdownMenu.Root>
      <DropdownMenu.Trigger className="ida-icon-btn" aria-label={`${t('prefs.theme')}: ${t(THEME_OPTIONS.find((o) => o.value === theme)!.key)}`}>
        <Icon name={resolvedTheme === 'dark' ? 'moon' : 'sun'}/>
      </DropdownMenu.Trigger>

      <DropdownMenu.Portal>
        <DropdownMenu.Content className="ida-menu ida-menu--compact" align="end" sideOffset={8}>
          <DropdownMenu.Label className="ida-menu__label">{t('prefs.theme')}</DropdownMenu.Label>
          <DropdownMenu.RadioGroup value={theme} onValueChange={(v) => setTheme(v as ThemeChoice)}>
            {THEME_OPTIONS.map(({ value, key, icon }) => (<DropdownMenu.RadioItem className="ida-menu__item" key={value} value={value}>
                <Icon name={icon} size={18}/>
                {t(key)}
                <DropdownMenu.ItemIndicator className="ida-menu__check">
                  <Icon name="check" size={16}/>
                </DropdownMenu.ItemIndicator>
              </DropdownMenu.RadioItem>))}
          </DropdownMenu.RadioGroup>
        </DropdownMenu.Content>
      </DropdownMenu.Portal>
    </DropdownMenu.Root>);
}
const THEME_OPTIONS: {
    value: ThemeChoice;
    key: StringKey;
    icon: IconName;
}[] = [
    { value: 'system', key: 'prefs.theme.system', icon: 'monitor' },
    { value: 'light', key: 'prefs.theme.light', icon: 'sun' },
    { value: 'dark', key: 'prefs.theme.dark', icon: 'moon' },
];
