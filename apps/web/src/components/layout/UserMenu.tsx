import * as DropdownMenu from '@radix-ui/react-dropdown-menu';
import { Icon } from '../Icon';
import { useT } from '../../i18n/useT';
export function UserMenu({ displayName, roleNameTh, initials, onSignOut, }: {
    displayName: string | undefined;
    roleNameTh: string | undefined;
    initials: string;
    onSignOut: () => void;
}) {
    const t = useT();
    return (<DropdownMenu.Root>
      <DropdownMenu.Trigger className="ida-user-trigger" aria-label={t('shell.userMenu')}>
        <span className="ida-topbar__user-text">
          <span className="ida-topbar__user-name">{displayName}</span>
          <span className="ida-caption ida-text-muted">{roleNameTh}</span>
        </span>
        <span className="ida-avatar" aria-hidden="true">
          <span>{initials}</span>
        </span>
        
        <Icon name="chevronDown" size={16} className="ida-user-trigger__caret" aria-hidden/>
      </DropdownMenu.Trigger>

      <DropdownMenu.Portal>
        <DropdownMenu.Content className="ida-menu" align="end" sideOffset={8}>
          
          <div className="ida-menu__header">
            <span className="ida-menu__name">{displayName}</span>
            <span className="ida-caption ida-text-muted">{roleNameTh}</span>
          </div>

          <DropdownMenu.Separator className="ida-menu__separator"/>

          <DropdownMenu.Item className="ida-menu__item" onSelect={onSignOut}>
            <Icon name="logout" size={18}/>
            {t('shell.signOut')}
          </DropdownMenu.Item>
        </DropdownMenu.Content>
      </DropdownMenu.Portal>
    </DropdownMenu.Root>);
}
