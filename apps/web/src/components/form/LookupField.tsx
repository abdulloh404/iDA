import { Controller, useFormContext } from 'react-hook-form';
import { useQuery } from '@tanstack/react-query';
import { fetchLookup, lookupLabel } from '../../api/lookup';
import type { LookupResource } from '../../api/lookup';
import { Icon } from '../Icon';
import { FormField } from './FormField';
import { Select } from './Select';
interface LookupFieldProps {
    name: string;
    label: string;
    required?: boolean;
    hint?: string;
    disabled?: boolean;
    width?: 'sm' | 'md' | 'lg' | 'full';
    resource: LookupResource;
    emptyLabel?: string;
}
export function LookupField({ resource, emptyLabel, ...props }: LookupFieldProps) {
    const { control } = useFormContext();
    const options = useQuery({
        queryKey: ['lookup', resource],
        queryFn: ({ signal }) => fetchLookup(resource, undefined, signal),
        staleTime: 5 * 60 * 1000,
    });
    const failed = options.isError;
    return (<FormField {...props}>
      {(aria) => (<>
          <Controller control={control} name={props.name} render={({ field }) => (<Select id={aria.id} value={field.value == null ? '' : String(field.value)} onChange={(value) => field.onChange(value === '' ? null : value)} options={(options.data ?? []).map((item) => ({
                    value: item.id,
                    label: lookupLabel(item),
                }))} emptyLabel={emptyLabel} placeholder={options.isPending
                    ? 'กำลังโหลดตัวเลือก…'
                    : failed
                        ? 'โหลดตัวเลือกไม่สำเร็จ'
                        : 'เลือกรายการ'} disabled={props.disabled || options.isPending || failed} invalid={aria['aria-invalid'] || failed} describedBy={aria['aria-describedby']}/>)}/>

          {failed && (<p className="ida-field-error" role="alert">
              <Icon name="alert" size={14}/>
              โหลดรายการ{props.label}ไม่สำเร็จ
              <button type="button" className="ida-link-button" onClick={() => void options.refetch()} disabled={options.isFetching}>
                {options.isFetching ? 'กำลังลองใหม่…' : 'ลองอีกครั้ง'}
              </button>
            </p>)}
        </>)}
    </FormField>);
}
