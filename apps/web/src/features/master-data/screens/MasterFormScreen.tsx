import { Link, useNavigate, useParams } from 'react-router-dom';
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
import type { AnyScreenDescriptor, FormMode } from '../descriptor';
import { renderField } from './renderField';
import { ChildTablePanel } from './ChildTablePanel';
export function MasterFormScreen({ descriptor, mode, }: {
    descriptor: AnyScreenDescriptor;
    mode: FormMode;
}) {
    const { id } = useParams<{
        id: string;
    }>();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const toast = useToast();
    const canWrite = usePermission(`${descriptor.resource}.write`);
    const readOnly = mode === 'view' || !canWrite;
    const detail = useQuery({
        queryKey: ['master', descriptor.resource, id],
        queryFn: ({ signal }) => descriptor.api.get(id!, signal),
        enabled: mode !== 'create' && !!id,
    });
    const save = useMutation({
        mutationFn: async (values: Record<string, unknown>) => {
            if (mode === 'create')
                return descriptor.api.create(values);
            const rowVersion = (detail.data as HasRowVersion | undefined)?.rowVersion ?? '';
            return descriptor.api.update(id!, values, rowVersion);
        },
        onSuccess: () => {
            invalidateAfterWrite(queryClient);
            toast.success(`บันทึกข้อมูล${descriptor.titleTh}เรียบร้อยแล้ว`);
            navigate(descriptor.path);
        },
    });
    if (mode !== 'create' && detail.isPending) {
        return <span className="ida-skeleton" style={{ height: '320px', display: 'block' }}/>;
    }
    if (detail.error)
        return <ApiErrorAlert error={detail.error}/>;
    const values = mode === 'create' ? descriptor.defaultValues : descriptor.toInput(detail.data);
    const locked = mode === 'create' ? null : (descriptor.isLocked?.(detail.data) ?? null);
    const formReadOnly = readOnly || locked !== null;
    const fieldMode: FormMode = locked !== null ? 'view' : mode;
    const title = mode === 'create'
        ? `สร้างข้อมูล${descriptor.titleTh}`
        : mode === 'edit'
            ? `แก้ไขข้อมูล${descriptor.titleTh}`
            : `ข้อมูล${descriptor.titleTh}`;
    const singleSection = descriptor.sections.length === 1 && !descriptor.sections[0].description;
    return (<>
      <PageHeader title={title} breadcrumbs={[...descriptor.breadcrumb, { label: descriptor.titleTh, to: descriptor.path }]} actions={mode === 'view' && canWrite && locked === null ? (<Link className="ida-btn ida-btn--primary" to={`${descriptor.path}/${id}/edit`}>
              <Icon name="pencil" size={18}/>
              แก้ไข
            </Link>) : undefined}/>

      {locked !== null && (<div className="ida-alert ida-alert--info" role="status" style={{ marginBottom: 'var(--ida-space-5)' }}>
          <div className="ida-alert__body">
            <Icon name="info" size={18}/>
            <span>{locked}</span>
          </div>
        </div>)}

      
      <Form schema={descriptor.schema} defaultValues={values as never} values={mode === 'create' ? undefined : (values as never)} readOnly={formReadOnly} onSubmit={async (submitted) => {
            await save.mutateAsync(submitted as Record<string, unknown>);
        }}>
        {descriptor.sections.map((section, index) => (<FormSection key={section.title ?? index} title={singleSection ? undefined : section.title} description={section.description}>
            {section.fields.map((field) => renderField(field, fieldMode))}
          </FormSection>))}

        
        {!formReadOnly && (<div className="ida-form-actions">
            <p className="ida-form-actions__note">
              ช่องที่มี <span className="ida-field__required">*</span> ต้องกรอก
            </p>
            <Link className="ida-btn ida-btn--secondary" to={descriptor.path}>
              ยกเลิก
            </Link>
            <button type="submit" className="ida-btn ida-btn--primary" disabled={save.isPending}>
              {save.isPending ? (<span className="ida-spinner" aria-hidden="true"/>) : (<Icon name="check" size={18}/>)}
              บันทึก
            </button>
          </div>)}
      </Form>

      
      {mode !== 'create' &&
            id &&
            descriptor.childTables?.map((child) => (<ChildTablePanel key={child.api.resource} child={child} parentId={id} parentRow={detail.data as Record<string, unknown> | undefined} readOnly={formReadOnly}/>))}

      {mode !== 'create' &&
            id &&
            detail.data &&
            descriptor.formPanels?.map((Panel, index) => (<Panel key={index} id={id} detail={detail.data} readOnly={formReadOnly}/>))}

      {mode !== 'create' && id && (<HistoryPanel descriptor={descriptor} id={id}/>)}
    </>);
}
