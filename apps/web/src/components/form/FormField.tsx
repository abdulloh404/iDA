import { useId } from 'react';
import type { ReactNode } from 'react';
import { useFormContext } from 'react-hook-form';
import { Icon } from '../Icon';
interface FormFieldProps {
    name: string;
    label: string;
    required?: boolean;
    hint?: string;
    width?: 'sm' | 'md' | 'lg' | 'full';
    children: (props: {
        id: string;
        'aria-invalid': boolean;
        'aria-describedby': string | undefined;
    }) => ReactNode;
}
export function FormField({ name, label, required, hint, width, children }: FormFieldProps) {
    const { formState: { errors }, } = useFormContext();
    const uid = useId();
    const inputId = `${uid}-input`;
    const errorId = `${uid}-error`;
    const hintId = `${uid}-hint`;
    const error = getError(errors, name);
    const describedBy = [error ? errorId : null, hint ? hintId : null]
        .filter(Boolean)
        .join(' ');
    return (<div className={`ida-field${width ? ` ida-field--${width}` : ''}`}>
      
      <label className="ida-label" htmlFor={inputId}>
        {label}
        {required && (<>
            {' '}
            <span className="ida-field__required" aria-hidden="true">
              *
            </span>
            <span className="ida-visually-hidden">(จำเป็น)</span>
          </>)}
      </label>

      {children({
            id: inputId,
            'aria-invalid': !!error,
            'aria-describedby': describedBy || undefined,
        })}

      {error && (<p className="ida-field-error" id={errorId}>
          <Icon name="alert" size={14}/>
          {error}
        </p>)}

      {hint && (<p className="ida-field-hint" id={hintId}>
          {hint}
        </p>)}
    </div>);
}
function getError(errors: Record<string, unknown>, name: string): string | undefined {
    const node = name
        .split('.')
        .reduce<unknown>((acc, key) => (acc as Record<string, unknown> | undefined)?.[key], errors);
    const message = (node as {
        message?: unknown;
    } | undefined)?.message;
    return typeof message === 'string' ? message : undefined;
}
