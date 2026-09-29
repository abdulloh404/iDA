import { AmountField, BoolField, CheckboxField, DateField, DateTimeField, NumberField, PasswordField, RadioGroupField, SelectField, SwitchField, TextAreaField, TextField, TimeField, } from '../../../components/form/fields';
import { LookupField } from '../../../components/form/LookupField';
import { PostcodeField } from '../../../components/form/PostcodeField';
import { LazyRichTextField } from '../../../components/form/LazyRichTextField';
import { fieldWidth } from '../descriptor';
import type { FieldDef, FormMode } from '../descriptor';
export function renderField(field: FieldDef, mode: FormMode) {
    const width = fieldWidth(field);
    const disabled = 'immutableOnEdit' in field && field.immutableOnEdit && mode === 'edit';
    switch (field.kind) {
        case 'text':
            return (<TextField key={field.name} width={width} name={field.name} label={field.label} required={field.required} maxLength={field.maxLength} hint={disabled ? 'ไม่สามารถแก้ไขรหัสได้ภายหลัง' : field.hint} disabled={disabled || field.autoFilled}/>);
        case 'textarea':
            return (<TextAreaField key={field.name} width={width} name={field.name} label={field.label} rows={field.rows} hint={field.hint}/>);
        case 'password':
            return (<PasswordField key={field.name} width={width} name={field.name} label={field.label} maxLength={field.maxLength} placeholder={field.placeholder} hint={field.hint}/>);
        case 'richtext':
            return (<LazyRichTextField key={field.name} width={width} name={field.name} label={field.label} required={field.required} maxLength={field.maxLength} hint={field.hint}/>);
        case 'number':
            return (<NumberField key={field.name} width={width} name={field.name} label={field.label} required={field.required} min={field.min} max={field.max} hint={field.hint}/>);
        case 'amount':
            return (<AmountField key={field.name} width={width} name={field.name} label={field.label} required={field.required} hint={field.hint}/>);
        case 'select':
            return (<SelectField key={field.name} width={width} name={field.name} label={field.label} required={field.required} options={field.options} emptyLabel={field.emptyLabel} hint={field.hint}/>);
        case 'lookup':
            return (<LookupField key={field.name} width={width} name={field.name} label={field.label} required={field.required} resource={field.resource} emptyLabel={field.emptyLabel} hint={field.hint}/>);
        case 'bool':
            return (<BoolField key={field.name} width={width} name={field.name} label={field.label} trueLabel={field.trueLabel} falseLabel={field.falseLabel} hint={field.hint}/>);
        case 'radio':
            return (<RadioGroupField key={field.name} width={width} name={field.name} label={field.label} required={field.required} options={field.options} hint={field.hint}/>);
        case 'checkbox':
            return (<CheckboxField key={field.name} width={width} name={field.name} label={field.label} hint={field.hint}/>);
        case 'switch':
            return (<SwitchField key={field.name} width={width} name={field.name} label={field.label} hint={field.hint} onValue={field.onValue} offValue={field.offValue} onLabel={field.onLabel} offLabel={field.offLabel}/>);
        case 'time':
            return (<TimeField key={field.name} width={width} name={field.name} label={field.label} required={field.required} hint={field.hint}/>);
        case 'datetime':
            return (<DateTimeField key={field.name} width={width} name={field.name} label={field.label} required={field.required} hint={field.hint} disabled={mode === 'view'}/>);
        case 'postcode':
            return (<PostcodeField key={field.name} width={width} name={field.name} label={field.label} required={field.required} hint={field.hint} fill={field.fill} disabled={mode === 'view'}/>);
        case 'date':
            return (<DateField key={field.name} width={width} name={field.name} label={field.label} required={field.required} hint={field.hint}/>);
    }
}
