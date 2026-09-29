import { useId, useState } from 'react';
import type { KeyboardEvent } from 'react';
import * as Popover from '@radix-ui/react-popover';
import { useQuery } from '@tanstack/react-query';
import { Controller, useFormContext } from 'react-hook-form';
import { loadPostcodes, searchPostcodes } from '../../data/thaiPostcodes';
import type { PostcodeEntry } from '../../data/thaiPostcodes';
import { FormField } from './FormField';
export interface PostcodeFill {
    subdistrict?: string;
    district?: string;
    province?: string;
    country?: string;
}
interface PostcodeFieldProps {
    name: string;
    label: string;
    required?: boolean;
    hint?: string;
    disabled?: boolean;
    width?: 'sm' | 'md' | 'lg' | 'full';
    fill: PostcodeFill;
}
const COUNTRY_TH = 'ไทย';
export function PostcodeField({ fill, ...props }: PostcodeFieldProps) {
    const { control, setValue, getValues } = useFormContext();
    const listId = useId();
    const [open, setOpen] = useState(false);
    const [active, setActive] = useState(0);
    const [wanted, setWanted] = useState(false);
    const data = useQuery({
        queryKey: ['thai-postcodes'],
        queryFn: loadPostcodes,
        enabled: wanted,
        staleTime: Infinity,
        gcTime: Infinity,
    });
    function choose(entry: PostcodeEntry, onChange: (value: string) => void) {
        onChange(entry.postcode);
        const options = { shouldDirty: true, shouldValidate: true } as const;
        if (fill.subdistrict)
            setValue(fill.subdistrict, entry.subdistrict, options);
        if (fill.district)
            setValue(fill.district, entry.district, options);
        if (fill.province)
            setValue(fill.province, entry.province, options);
        if (fill.country)
            setValue(fill.country, COUNTRY_TH, options);
        setOpen(false);
    }
    function clearFilled() {
        const options = { shouldDirty: true } as const;
        for (const target of [fill.subdistrict, fill.district, fill.province]) {
            if (target && getValues(target))
                setValue(target, '', options);
        }
    }
    return (<FormField {...props}>
      {(aria) => (<Controller control={control} name={props.name} render={({ field }) => {
                const text = field.value == null ? '' : String(field.value);
                const matches = data.data ? searchPostcodes(data.data, text) : [];
                const current = Math.min(active, Math.max(matches.length - 1, 0));
                const showList = open && text.trim() !== '';
                function onKeyDown(e: KeyboardEvent<HTMLInputElement>) {
                    if (e.key === 'ArrowDown') {
                        e.preventDefault();
                        if (!open)
                            setOpen(true);
                        else
                            setActive(Math.min(current + 1, matches.length - 1));
                    }
                    else if (e.key === 'ArrowUp') {
                        e.preventDefault();
                        setActive(Math.max(current - 1, 0));
                    }
                    else if (e.key === 'Enter' && showList && matches[current]) {
                        e.preventDefault();
                        choose(matches[current], field.onChange);
                    }
                    else if (e.key === 'Escape' && showList) {
                        e.preventDefault();
                        setOpen(false);
                    }
                }
                return (<Popover.Root open={showList} onOpenChange={setOpen}>
                <Popover.Anchor asChild>
                  <input {...aria} ref={field.ref} name={field.name} className="ida-input" type="text" autoComplete="off" placeholder="พิมพ์รหัสหรือชื่อตำบล/อำเภอ" disabled={props.disabled} role="combobox" aria-autocomplete="list" aria-expanded={showList} aria-controls={listId} aria-activedescendant={showList && matches[current] ? `${listId}-${current}` : undefined} value={text} onFocus={() => setWanted(true)} onChange={(e) => {
                        setWanted(true);
                        setActive(0);
                        setOpen(true);
                        clearFilled();
                        field.onChange(e.target.value);
                    }} onBlur={() => {
                        setOpen(false);
                        field.onBlur();
                    }} onKeyDown={onKeyDown}/>
                </Popover.Anchor>

                <Popover.Portal>
                  <Popover.Content className="ida-select-content ida-combobox-content" align="start" sideOffset={6} onOpenAutoFocus={(e) => e.preventDefault()} onCloseAutoFocus={(e) => e.preventDefault()}>
                    <ul id={listId} role="listbox" aria-label={props.label} className="ida-select-viewport">
                      {data.isPending ? (<li className="ida-select-item ida-select-item--muted" aria-disabled="true">
                          กำลังโหลดรายการรหัสไปรษณีย์…
                        </li>) : data.isError ? (<li className="ida-select-item ida-select-item--muted" aria-disabled="true">
                          โหลดรายการรหัสไปรษณีย์ไม่สำเร็จ พิมพ์รหัสเองได้
                        </li>) : matches.length === 0 ? (<li className="ida-select-item ida-select-item--muted" aria-disabled="true">
                          ไม่พบรหัสไปรษณีย์ที่ตรงกัน
                        </li>) : (matches.map((entry, i) => (<li key={`${entry.postcode}-${entry.subdistrict}-${entry.district}`} id={`${listId}-${i}`} role="option" aria-selected={i === current} data-highlighted={i === current ? '' : undefined} className="ida-select-item ida-combobox-item" onMouseDown={(e) => e.preventDefault()} onMouseMove={() => setActive(i)} onClick={() => choose(entry, field.onChange)}>
                            <span className="ida-combobox-item__code">{entry.postcode}</span>
                            <span className="ida-combobox-item__detail">
                              {entry.subdistrict} · {entry.district} · {entry.province}
                            </span>
                          </li>)))}
                    </ul>
                  </Popover.Content>
                </Popover.Portal>
              </Popover.Root>);
            }}/>)}
    </FormField>);
}
