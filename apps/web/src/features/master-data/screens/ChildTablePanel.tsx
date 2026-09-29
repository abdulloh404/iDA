import { useState } from 'react';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { DataTable } from '../../../components/data/DataTable';
import { ApiErrorAlert } from '../../../components/feedback/ApiErrorAlert';
import { ConfirmDialog } from '../../../components/feedback/ConfirmDialog';
import { useToast } from '../../../components/feedback/toastContext';
import { Icon } from '../../../components/Icon';
import { Form, FormSection } from '../../../components/form/Form';
import { invalidateAfterWrite } from '../../../app/queryClient';
import { usePermission } from '../../auth/authState';
import { renderField } from './renderField';
import type { AnyChildTableDef } from '../descriptor';
export function ChildTablePanel({ child, parentId, parentRow, readOnly, }: {
    child: AnyChildTableDef;
    parentId: string;
    parentRow?: Record<string, unknown>;
    readOnly: boolean;
}) {
    const queryClient = useQueryClient();
    const toast = useToast();
    const fromOtherSystem = child.readOnly === true;
    const canWrite = usePermission(`${child.api.resource}.write`) && !readOnly && !fromOtherSystem;
    const canDelete = usePermission(`${child.api.resource}.delete`) && !readOnly && !fromOtherSystem;
    const [editing, setEditing] = useState<Record<string, unknown> | null>(null);
    const [adding, setAdding] = useState(false);
    const [pendingDelete, setPendingDelete] = useState<Record<string, unknown> | null>(null);
    const parentValue = child.parentValueFrom
        ? ((parentRow?.[child.parentValueFrom] as string | undefined) ?? '')
        : parentId;
    const rows = useQuery({
        queryKey: ['child', child.api.resource, parentValue],
        queryFn: ({ signal }) => child.api.list({ [child.parentKey]: parentValue, pageSize: 200, sort: child.defaultSort }, signal),
        enabled: parentValue !== '',
        placeholderData: keepPreviousData,
    });
    function refresh() {
        invalidateAfterWrite(queryClient);
    }
    const remove = useMutation({
        mutationFn: (id: string) => child.api.remove(id),
        onSuccess: () => {
            refresh();
            toast.success(`ลบ${child.title}เรียบร้อยแล้ว`);
            setPendingDelete(null);
        },
        onError: (error) => {
            toast.error(error instanceof Error ? error.message : 'ลบรายการไม่สำเร็จ');
            setPendingDelete(null);
        },
    });
    const items = rows.data?.items ?? [];
    const editorOpen = adding || editing !== null;
    return (<section className="ida-card">
      <h2 className="ida-card__title">{child.title}</h2>
      {child.description && <p className="ida-text-secondary">{child.description}</p>}

      <DataTable caption={child.title} columns={child.columns} rows={items} rowKey={child.rowKey} loading={rows.isPending} error={rows.error} empty={{
            title: `ยังไม่มี${child.title}`,
            hint: child.emptyHint,
            ...(canWrite
                ? { action: { label: child.addLabel ?? 'เพิ่มรายการ', onClick: () => setAdding(true) } }
                : {}),
        }} rowActions={[
            {
                icon: 'pencil',
                label: 'แก้ไข',
                onClick: (row) => {
                    setAdding(false);
                    setEditing(row as Record<string, unknown>);
                },
                hidden: !canWrite,
            },
            {
                icon: 'trash',
                label: 'ลบ',
                tone: 'danger',
                onClick: (row) => setPendingDelete(row as Record<string, unknown>),
                hidden: !canDelete,
            },
        ]}/>

      {canWrite && !editorOpen && items.length > 0 && (<button type="button" className="ida-btn ida-btn--secondary ida-btn--sm" onClick={() => setAdding(true)}>
          <Icon name="plus" size={16}/>
          {child.addLabel ?? 'เพิ่มรายการ'}
        </button>)}

      {editorOpen && (<ChildRowEditor key={(editing?.id as string) ?? 'new'} child={child} parentId={parentValue} row={editing} onDone={() => {
                refresh();
                setAdding(false);
                setEditing(null);
            }} onCancel={() => {
                setAdding(false);
                setEditing(null);
            }}/>)}

      <ConfirmDialog open={pendingDelete !== null} title={`ลบ${child.title}`} tone="danger" confirmLabel="ลบรายการ" busy={remove.isPending} onCancel={() => setPendingDelete(null)} onConfirm={() => pendingDelete && remove.mutate(child.rowKey(pendingDelete) as string)}>
        ต้องการลบรายการนี้หรือไม่
      </ConfirmDialog>
    </section>);
}
function ChildRowEditor({ child, parentId, row, onDone, onCancel, }: {
    child: AnyChildTableDef;
    parentId: string;
    row: Record<string, unknown> | null;
    onDone: () => void;
    onCancel: () => void;
}) {
    const isEdit = row !== null;
    const rowId = isEdit ? (child.rowKey(row as never) as string) : null;
    const detail = useQuery({
        queryKey: ['child', child.api.resource, 'detail', rowId],
        queryFn: ({ signal }) => child.api.get(rowId!, signal),
        enabled: rowId !== null,
    });
    const save = useMutation({
        mutationFn: async (input: Record<string, unknown>) => {
            const body = { ...input, [child.parentKey]: parentId };
            if (!isEdit)
                return child.api.create(body);
            return child.api.update(rowId!, body, detail.data!.rowVersion);
        },
        onSuccess: onDone,
    });
    if (isEdit && detail.isPending) {
        return <span className="ida-skeleton" style={{ height: '120px', display: 'block' }}/>;
    }
    if (detail.error)
        return <ApiErrorAlert error={detail.error}/>;
    const values = isEdit
        ? child.toInput(detail.data as never)
        : { ...child.defaultValues, [child.parentKey]: parentId };
    return (<div className="ida-allowance-editor">
      <Form schema={child.schema} defaultValues={values as never} values={isEdit ? (values as never) : undefined} onSubmit={async (submitted) => {
            await save.mutateAsync(submitted as Record<string, unknown>);
        }}>
        <FormSection>{child.fields.map((field) => renderField(field, 'create'))}</FormSection>

        <div className="ida-allowance-editor__actions">
          <button type="button" className="ida-btn ida-btn--ghost ida-btn--sm" onClick={onCancel}>
            ยกเลิก
          </button>
          <button type="submit" className="ida-btn ida-btn--primary ida-btn--sm" disabled={save.isPending}>
            {save.isPending ? (<span className="ida-spinner" aria-hidden="true"/>) : (<Icon name="check" size={16}/>)}
            {isEdit ? 'บันทึกการแก้ไข' : (child.addLabel ?? 'เพิ่มรายการ')}
          </button>
        </div>
      </Form>
    </div>);
}
