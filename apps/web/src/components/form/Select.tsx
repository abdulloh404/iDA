import * as RadixSelect from '@radix-ui/react-select';
import { Icon } from '../Icon';
export interface SelectOption {
    value: string;
    label: string;
}
export interface SelectProps {
    id?: string;
    value: string;
    onChange: (value: string) => void;
    options: readonly SelectOption[];
    emptyLabel?: string;
    prefix?: string;
    placeholder?: string;
    disabled?: boolean;
    invalid?: boolean;
    describedBy?: string;
    ariaLabel?: string;
    className?: string;
}
export function Select({ id, value, onChange, options, emptyLabel, prefix, placeholder, disabled, invalid, describedBy, ariaLabel, className, }: SelectProps) {
    const EMPTY = '__ida_empty__';
    return (<RadixSelect.Root value={value === '' ? (emptyLabel !== undefined ? EMPTY : undefined) : value} onValueChange={(next) => onChange(next === EMPTY ? '' : next)} disabled={disabled}>
      <RadixSelect.Trigger id={id} className={`ida-select-trigger${className ? ` ${className}` : ''}`} aria-invalid={invalid || undefined} aria-describedby={describedBy} aria-label={ariaLabel}>
        <span className="ida-select-trigger__value">
          {prefix !== undefined && (<span className="ida-select-trigger__prefix" aria-hidden="true">
              {prefix}
            </span>)}
          <RadixSelect.Value placeholder={placeholder ?? 'เลือก…'}/>
        </span>
        <RadixSelect.Icon className="ida-select-trigger__caret">
          <Icon name="chevronDown" size={16}/>
        </RadixSelect.Icon>
      </RadixSelect.Trigger>

      <RadixSelect.Portal>
        <RadixSelect.Content className="ida-select-content" position="popper" sideOffset={6}>
          <RadixSelect.ScrollUpButton className="ida-select-scroll">
            <Icon name="chevronDown" size={14} className="ida-select-scroll__up"/>
          </RadixSelect.ScrollUpButton>

          <RadixSelect.Viewport className="ida-select-viewport">
            {emptyLabel !== undefined && (<Item value={EMPTY} label={emptyLabel} muted/>)}
            {options.map((option) => (<Item key={option.value} value={option.value} label={option.label}/>))}
          </RadixSelect.Viewport>

          <RadixSelect.ScrollDownButton className="ida-select-scroll">
            <Icon name="chevronDown" size={14}/>
          </RadixSelect.ScrollDownButton>
        </RadixSelect.Content>
      </RadixSelect.Portal>
    </RadixSelect.Root>);
}
function Item({ value, label, muted }: {
    value: string;
    label: string;
    muted?: boolean;
}) {
    return (<RadixSelect.Item value={value} className={`ida-select-item${muted ? ' ida-select-item--muted' : ''}`}>
      <RadixSelect.ItemText>{label}</RadixSelect.ItemText>
      
      <RadixSelect.ItemIndicator className="ida-select-item__check">
        <Icon name="check" size={16}/>
      </RadixSelect.ItemIndicator>
    </RadixSelect.Item>);
}
