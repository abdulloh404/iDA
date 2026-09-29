import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Form, FormSection } from '../../../components/form/Form';
import { useToast } from '../../../components/feedback/toastContext';
import { ApiErrorAlert } from '../../../components/feedback/ApiErrorAlert';
import { PageHeader } from '../../../components/layout/PageHeader';
import { Icon } from '../../../components/Icon';
import { invalidateAfterWrite } from '../../../app/queryClient';
import { usePermission } from '../../auth/authState';
import type { HasRowVersion } from '../../../api/types';
import { HistoryPanel } from './HistoryPanel';
import { renderField } from './renderField';
import type { AnyScreenDescriptor } from '../descriptor';
export function SettingsScreen({ descriptor }: {
    descriptor: AnyScreenDescriptor;
}) {
    const queryClient = useQueryClient();
    const toast = useToast();
    const canWrite = usePermission(`${descriptor.resource}.write`);
    const [generation, setGeneration] = useState(0);
    const list = useQuery({
        queryKey: ['master', descriptor.resource, 'list', { page: 1, pageSize: 1 }],
        queryFn: ({ signal }) => descriptor.api.list({ page: 1, pageSize: 1 }, signal),
    });
    const first = list.data?.items[0];
    const id = first ? descriptor.rowKey(first) : undefined;
    const detail = useQuery({
        queryKey: ['master', descriptor.resource, id],
        queryFn: ({ signal }) => descriptor.api.get(id!, signal),
        enabled: !!id,
    });
    const save = useMutation({
        mutationFn: async (values: Record<string, unknown>) => {
            if (!id)
                return descriptor.api.create(values);
            const rowVersion = (detail.data as HasRowVersion | undefined)?.rowVersion ?? '';
            return descriptor.api.update(id, values, rowVersion);
        },
        onSuccess: () => {
            invalidateAfterWrite(queryClient);
            toast.success(`บันทึก${descriptor.titleTh}เรียบร้อยแล้ว`);
        },
    });
    const header = (<PageHeader title={descriptor.titleTh} description={descriptor.singleton?.intro} breadcrumbs={descriptor.breadcrumb}/>);
    if (list.isPending || (id && detail.isPending)) {
        return (<>
        {header}
        <span className="ida-skeleton" style={{ height: '280px', display: 'block' }}/>
      </>);
    }
    const error = list.error ?? detail.error;
    if (error) {
        return (<>
        {header}
        <ApiErrorAlert error={error}/>
      </>);
    }
    const values = detail.data ? descriptor.toInput(detail.data) : descriptor.defaultValues;
    const updatedAt = (detail.data as {
        updatedAt?: string;
    } | undefined)?.updatedAt;
    const rowVersion = (detail.data as HasRowVersion | undefined)?.rowVersion;
    const readOnly = !canWrite;
    const singleSection = descriptor.sections.length === 1 && !descriptor.sections[0].description;
    return (<>
      {header}

      <p className="ida-settings-status" role="status">
        {id ? (<>
            <Icon name="check" size={16}/>
            <span>
              ใช้งานอยู่
              {updatedAt && <> · แก้ไขล่าสุด {formatDateTime(updatedAt)}</>}
            </span>
          </>) : (<>
            <Icon name="info" size={16}/>
            <span>
              ยังไม่เคยตั้งค่า — ค่าด้านล่างเป็นค่าที่แนะนำ จะมีผลเมื่อกดบันทึก
            </span>
          </>)}
      </p>

      
      <Form key={`${rowVersion ?? 'new'}-${generation}`} schema={descriptor.schema} defaultValues={values as never} readOnly={readOnly} onSubmit={async (submitted) => {
            await save.mutateAsync(submitted as Record<string, unknown>);
        }}>
        {descriptor.sections.map((section, index) => (<FormSection key={section.title ?? index} title={singleSection ? undefined : section.title} description={section.description}>
            {section.fields.map((field) => renderField(field, readOnly ? 'view' : 'edit'))}
          </FormSection>))}

        {!readOnly && (<div className="ida-form-actions">
            <p className="ida-form-actions__note">
              ช่องที่มี <span className="ida-field__required">*</span> ต้องกรอก
            </p>
            <button type="button" className="ida-btn ida-btn--secondary" onClick={() => setGeneration((g) => g + 1)}>
              ยกเลิก
            </button>
            <button type="submit" className="ida-btn ida-btn--primary" disabled={save.isPending}>
              {save.isPending ? (<span className="ida-spinner" aria-hidden="true"/>) : (<Icon name="check" size={18}/>)}
              บันทึก
            </button>
          </div>)}
      </Form>

      {id && <HistoryPanel descriptor={descriptor} id={id}/>}
    </>);
}
function formatDateTime(iso: string): string {
    const date = new Date(iso);
    if (Number.isNaN(date.getTime()))
        return iso;
    const pad = (n: number) => String(n).padStart(2, '0');
    return (`${pad(date.getDate())}/${pad(date.getMonth() + 1)}/${date.getFullYear()} ` +
        `${pad(date.getHours())}:${pad(date.getMinutes())}`);
}
