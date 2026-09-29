import { useEffect, useState } from 'react';
import type { ReactNode } from 'react';
import { useBlocker } from 'react-router-dom';
import { FormProvider, useForm } from 'react-hook-form';
import type { DefaultValues, FieldValues, Path } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import type { ZodType } from 'zod';
import { ApiError } from '../../api/client';
import { ApiErrorAlert } from '../feedback/ApiErrorAlert';
import { ConfirmDialog } from '../feedback/ConfirmDialog';
import { FormReadOnlyContext } from './formReadOnly';
interface FormProps<TValues extends FieldValues> {
    schema: ZodType<TValues>;
    defaultValues: DefaultValues<TValues>;
    values?: TValues;
    onSubmit: (values: TValues) => Promise<void>;
    readOnly?: boolean;
    children: ReactNode;
}
export function Form<TValues extends FieldValues>({ schema, defaultValues, values, onSubmit, readOnly = false, children, }: FormProps<TValues>) {
    const [formError, setFormError] = useState<unknown>(null);
    const [leaveAnyway, setLeaveAnyway] = useState(false);
    const methods = useForm<TValues>({
        resolver: zodResolver(schema as never),
        defaultValues,
        values,
        resetOptions: { keepDirtyValues: true },
        mode: 'onTouched',
    });
    const guarding = !readOnly &&
        methods.formState.isDirty &&
        !methods.formState.isSubmitting &&
        !methods.formState.isSubmitSuccessful &&
        !leaveAnyway;
    const blocker = useBlocker(({ currentLocation, nextLocation }) => guarding && currentLocation.pathname !== nextLocation.pathname);
    useEffect(() => {
        if (!guarding)
            return;
        const warn = (event: BeforeUnloadEvent) => event.preventDefault();
        window.addEventListener('beforeunload', warn);
        return () => window.removeEventListener('beforeunload', warn);
    }, [guarding]);
    const submit = methods.handleSubmit(async (values) => {
        setFormError(null);
        try {
            await onSubmit(values as TValues);
        }
        catch (error) {
            if (error instanceof ApiError && error.fields.length > 0) {
                let unmapped = false;
                for (const field of error.fields) {
                    if (field.field in methods.getValues()) {
                        methods.setError(field.field as Path<TValues>, {
                            type: 'server',
                            message: field.message,
                        });
                    }
                    else {
                        unmapped = true;
                    }
                }
                if (unmapped)
                    setFormError(error);
                return;
            }
            setFormError(error);
        }
    });
    return (<FormProvider {...methods}>
      <form onSubmit={submit} noValidate>
        {formError != null && (<div style={{ marginBottom: 'var(--ida-space-5)' }}>
            <ApiErrorAlert error={formError}/>
          </div>)}
        
        <fieldset disabled={readOnly || methods.formState.isSubmitting} style={{ border: 0, padding: 0, margin: 0, minInlineSize: 0 }}>
          <FormReadOnlyContext.Provider value={readOnly || methods.formState.isSubmitting}>
            {children}
          </FormReadOnlyContext.Provider>
        </fieldset>
      </form>

      <ConfirmDialog open={blocker.state === 'blocked'} title="ออกจากหน้านี้โดยไม่บันทึก?" confirmLabel="ออกโดยไม่บันทึก" cancelLabel="อยู่หน้านี้ต่อ" tone="danger" onCancel={() => blocker.reset?.()} onConfirm={() => {
            setLeaveAnyway(true);
            blocker.proceed?.();
        }}>
        ข้อมูลที่กรอกไว้ในหน้านี้ยังไม่ได้บันทึก ถ้าออกตอนนี้จะหายทั้งหมด
      </ConfirmDialog>
    </FormProvider>);
}
export function FormSection({ title, description, children, }: {
    title?: string;
    description?: string;
    children: ReactNode;
}) {
    return (<section className="ida-form-section">
      {title && (<header className="ida-form-section__header">
          <h2 className="ida-form-section__title">{title}</h2>
          {description && <p className="ida-form-section__description">{description}</p>}
        </header>)}
      <div className="ida-form-grid">{children}</div>
    </section>);
}
