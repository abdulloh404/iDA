import { useEffect, useState } from 'react';
import { Controller, useFormContext } from 'react-hook-form';
import { Icon } from '../Icon';
import { FormField } from './FormField';
import { Select } from './Select';
import type { SelectOption } from './Select';
export type { SelectOption };
interface BaseFieldProps {
    name: string;
    label: string;
    required?: boolean;
    hint?: string;
    disabled?: boolean;
    placeholder?: string;
    width?: 'sm' | 'md' | 'lg' | 'full';
}
export function TextField({ maxLength, ...props }: BaseFieldProps & {
    maxLength?: number;
}) {
    const { register } = useFormContext();
    return (<FormField {...props}>
      {(aria) => (<input {...aria} className="ida-input" type="text" maxLength={maxLength} disabled={props.disabled} placeholder={props.placeholder} {...register(props.name)}/>)}
    </FormField>);
}
export function PasswordField({ maxLength, ...props }: BaseFieldProps & {
    maxLength?: number;
}) {
    const { register } = useFormContext();
    const [visible, setVisible] = useState(false);
    return (<FormField {...props}>
      {(aria) => (<div className="ida-password">
          <input {...aria} className="ida-input" type={visible ? 'text' : 'password'} autoComplete="new-password" maxLength={maxLength} disabled={props.disabled} placeholder={props.placeholder} {...register(props.name)}/>
          <button type="button" className="ida-password__toggle" aria-label={visible ? 'ซ่อนรหัสผ่าน' : 'แสดงรหัสผ่าน'} aria-pressed={visible} onClick={() => setVisible((v) => !v)}>
            <Icon name={visible ? 'eyeOff' : 'eye'} size={18}/>
          </button>
        </div>)}
    </FormField>);
}
export function TextAreaField({ rows = 3, ...props }: BaseFieldProps & {
    rows?: number;
}) {
    const { register } = useFormContext();
    return (<FormField {...props}>
      {(aria) => (<textarea {...aria} className="ida-input" rows={rows} disabled={props.disabled} placeholder={props.placeholder} {...register(props.name)}/>)}
    </FormField>);
}
export function NumberField(props: BaseFieldProps & {
    min?: number;
    max?: number;
}) {
    const { register } = useFormContext();
    return (<FormField {...props}>
      {(aria) => (<input {...aria} className="ida-input" type="number" min={props.min} max={props.max} disabled={props.disabled} {...register(props.name, {
            setValueAs: (v) => (v === '' || v === null ? null : Number(v)),
        })}/>)}
    </FormField>);
}
export function AmountField(props: BaseFieldProps) {
    const { register } = useFormContext();
    return (<FormField {...props}>
      {(aria) => (<input {...aria} className="ida-input ida-input--amount" type="number" step="0.01" inputMode="decimal" disabled={props.disabled} {...register(props.name, {
            setValueAs: (v) => (v === '' || v === null ? null : Number(v)),
        })}/>)}
    </FormField>);
}
export function SelectField({ options, emptyLabel, ...props }: BaseFieldProps & {
    options: readonly SelectOption[];
    emptyLabel?: string;
}) {
    const { control } = useFormContext();
    return (<FormField {...props}>
      {(aria) => (<Controller control={control} name={props.name} render={({ field }) => (<Select id={aria.id} value={field.value == null ? '' : String(field.value)} onChange={field.onChange} options={options} emptyLabel={emptyLabel} placeholder={props.placeholder} disabled={props.disabled} invalid={aria['aria-invalid']} describedBy={aria['aria-describedby']}/>)}/>)}
    </FormField>);
}
export function RadioGroupField({ options, ...props }: BaseFieldProps & {
    options: readonly SelectOption[];
}) {
    const { register } = useFormContext();
    return (<FormField {...props}>
      {(aria) => (<div className="ida-radio-group" role="radiogroup" aria-invalid={aria['aria-invalid']} aria-describedby={aria['aria-describedby']}>
          {options.map((option, index) => (<label className="ida-radio" key={option.value}>
              <input type="radio" value={option.value} disabled={props.disabled} {...(index === 0 ? { id: aria.id } : {})} {...register(props.name)}/>
              <span className="ida-radio__dot" aria-hidden="true"/>
              <span className="ida-radio__text">{option.label}</span>
            </label>))}
        </div>)}
    </FormField>);
}
export function CheckboxField(props: BaseFieldProps) {
    const { register } = useFormContext();
    return (<FormField {...props} label={props.label}>
      {(aria) => (<label className="ida-checkbox">
          <input type="checkbox" {...aria} disabled={props.disabled} {...register(props.name)}/>
          <span className="ida-checkbox__box" aria-hidden="true">
            <Icon name="check" size={14}/>
          </span>
          <span>{props.hint ?? 'ใช่'}</span>
        </label>)}
    </FormField>);
}
export function DateField(props: BaseFieldProps) {
    const { register } = useFormContext();
    return (<FormField {...props}>
      {(aria) => (<input {...aria} className="ida-input" type="date" disabled={props.disabled} {...register(props.name)}/>)}
    </FormField>);
}
export function TimeField(props: BaseFieldProps) {
    const { register, watch, setValue } = useFormContext();
    const raw = watch(props.name);
    useEffect(() => {
        if (typeof raw === 'string' && /^\d{2}:\d{2}:\d{2}/.test(raw))
            setValue(props.name, raw.slice(0, 5), { shouldDirty: false });
    }, [raw, props.name, setValue]);
    return (<FormField {...props}>
      {(aria) => (<input {...aria} className="ida-input" type="time" disabled={props.disabled} {...register(props.name)}/>)}
    </FormField>);
}
export function DateTimeField(props: BaseFieldProps) {
    const { control } = useFormContext();
    return (<FormField {...props}>
      {(aria) => (<Controller control={control} name={props.name} render={({ field }) => (<input {...aria} className="ida-input" type="datetime-local" disabled={props.disabled} value={toLocalInput(field.value)} onChange={(e) => field.onChange(e.target.value === '' ? null : new Date(e.target.value).toISOString())} onBlur={field.onBlur}/>)}/>)}
    </FormField>);
}
function toLocalInput(value: unknown): string {
    if (typeof value !== 'string' || value === '')
        return '';
    const at = new Date(value);
    if (Number.isNaN(at.getTime()))
        return '';
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${at.getFullYear()}-${pad(at.getMonth() + 1)}-${pad(at.getDate())}T${pad(at.getHours())}:${pad(at.getMinutes())}`;
}
export function SwitchField({ onValue = 'true', offValue = 'false', onLabel = 'เปิด', offLabel = 'ปิด', ...props }: BaseFieldProps & {
    onValue?: string;
    offValue?: string;
    onLabel?: string;
    offLabel?: string;
}) {
    const { control } = useFormContext();
    return (<FormField {...props}>
      {(aria) => (<Controller control={control} name={props.name} render={({ field }) => {
                const checked = String(field.value) === onValue;
                return (<label className="ida-switch">
                <input {...aria} type="checkbox" role="switch" checked={checked} disabled={props.disabled} onChange={(e) => field.onChange(e.target.checked ? onValue : offValue)} onBlur={field.onBlur}/>
                <span className="ida-switch__track" aria-hidden="true">
                  <span className="ida-switch__thumb"/>
                </span>
                
                <span className="ida-switch__state" aria-hidden="true">
                  {checked ? onLabel : offLabel}
                </span>
              </label>);
            }}/>)}
    </FormField>);
}
export function BoolField({ trueLabel, falseLabel, ...props }: BaseFieldProps & {
    trueLabel: string;
    falseLabel: string;
}) {
    const { control } = useFormContext();
    return (<FormField {...props}>
      {(aria) => (<Controller control={control} name={props.name} render={({ field }) => (<div className="ida-radio-group" role="radiogroup" aria-invalid={aria['aria-invalid']} aria-describedby={aria['aria-describedby']}>
              {[
                    { value: true, label: trueLabel },
                    { value: false, label: falseLabel },
                ].map((option, index) => (<label className="ida-radio" key={String(option.value)}>
                  <input type="radio" name={props.name} value={String(option.value)} checked={field.value === option.value} disabled={props.disabled} onChange={() => field.onChange(option.value)} onBlur={field.onBlur} {...(index === 0 ? { id: aria.id } : {})}/>
                  <span className="ida-radio__dot" aria-hidden="true"/>
                  <span className="ida-radio__text">{option.label}</span>
                </label>))}
            </div>)}/>)}
    </FormField>);
}
