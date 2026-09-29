import { useEffect, useState } from 'react';
import { Controller, useFormContext } from 'react-hook-form';
import { EditorContent, useEditor, useEditorState } from '@tiptap/react';
import type { Editor } from '@tiptap/react';
import StarterKit from '@tiptap/starter-kit';
import { Icon } from '../Icon';
import type { IconName } from '../Icon';
import { FormField } from './FormField';
import { useFormReadOnly } from './formReadOnly';
export function RichTextField({ name, label, required, hint, maxLength, width = 'full', }: {
    name: string;
    label: string;
    required?: boolean;
    hint?: string;
    maxLength?: number;
    width?: 'sm' | 'md' | 'lg' | 'full';
}) {
    const { control } = useFormContext();
    const readOnly = useFormReadOnly();
    return (<FormField name={name} label={label} required={required} hint={hint} width={width}>
      {(aria) => (<Controller name={name} control={control} render={({ field }) => (<Editor_ id={aria.id} invalid={aria['aria-invalid']} describedBy={aria['aria-describedby']} label={label} value={(field.value as string | null) ?? ''} onChange={field.onChange} onBlur={field.onBlur} readOnly={readOnly} maxLength={maxLength}/>)}/>)}
    </FormField>);
}
function Editor_({ id, invalid, describedBy, label, value, onChange, onBlur, readOnly, maxLength, }: {
    id: string;
    invalid: boolean;
    describedBy: string | undefined;
    label: string;
    value: string;
    onChange: (html: string) => void;
    onBlur: () => void;
    readOnly: boolean;
    maxLength?: number;
}) {
    const editor = useEditor({
        extensions: [
            StarterKit.configure({
                heading: { levels: [2, 3] },
                code: false,
                codeBlock: false,
                link: { openOnClick: false, autolink: true, defaultProtocol: 'https' },
            }),
        ],
        content: value,
        editable: !readOnly,
        immediatelyRender: false,
        editorProps: {
            attributes: {
                id,
                class: 'ida-richtext__content',
                role: 'textbox',
                'aria-multiline': 'true',
                'aria-label': label,
                ...(describedBy ? { 'aria-describedby': describedBy } : {}),
                ...(invalid ? { 'aria-invalid': 'true' } : {}),
            },
        },
        onUpdate: ({ editor: e }) => onChange(e.isEmpty ? '' : e.getHTML()),
        onBlur: () => onBlur(),
    });
    useEffect(() => {
        if (!editor || editor.isFocused)
            return;
        const current = editor.isEmpty ? '' : editor.getHTML();
        if (current !== value)
            editor.commands.setContent(value, { emitUpdate: false });
    }, [editor, value]);
    useEffect(() => {
        editor?.setEditable(!readOnly);
    }, [editor, readOnly]);
    const length = value.length;
    const over = maxLength !== undefined && length > maxLength;
    return (<div className={`ida-richtext${invalid ? ' ida-richtext--invalid' : ''}${readOnly ? ' ida-richtext--readonly' : ''}`}>
      {!readOnly && editor && <Toolbar editor={editor}/>}
      <EditorContent editor={editor}/>
      {maxLength !== undefined && !readOnly && (<p className={`ida-richtext__count${over ? ' ida-richtext__count--over' : ''}`}>
          {over && <Icon name="alert" size={14}/>}
          {length.toLocaleString('th-TH')} / {maxLength.toLocaleString('th-TH')} ตัวอักษร
          <span className="ida-visually-hidden"> (นับรวมการจัดรูปแบบ)</span>
        </p>)}
    </div>);
}
interface ToolDef {
    icon: IconName;
    label: string;
    isActive?: (e: Editor) => boolean;
    run: (e: Editor) => void;
    disabled?: (e: Editor) => boolean;
}
const TOOLS: readonly (ToolDef | 'sep')[] = [
    {
        icon: 'bold',
        label: 'ตัวหนา',
        isActive: (e) => e.isActive('bold'),
        run: (e) => e.chain().focus().toggleBold().run(),
    },
    {
        icon: 'italic',
        label: 'ตัวเอียง',
        isActive: (e) => e.isActive('italic'),
        run: (e) => e.chain().focus().toggleItalic().run(),
    },
    {
        icon: 'underline',
        label: 'ขีดเส้นใต้',
        isActive: (e) => e.isActive('underline'),
        run: (e) => e.chain().focus().toggleUnderline().run(),
    },
    'sep',
    {
        icon: 'heading',
        label: 'หัวข้อ',
        isActive: (e) => e.isActive('heading', { level: 2 }),
        run: (e) => e.chain().focus().toggleHeading({ level: 2 }).run(),
    },
    {
        icon: 'listBullet',
        label: 'รายการแบบจุด',
        isActive: (e) => e.isActive('bulletList'),
        run: (e) => e.chain().focus().toggleBulletList().run(),
    },
    {
        icon: 'listOrdered',
        label: 'รายการแบบตัวเลข',
        isActive: (e) => e.isActive('orderedList'),
        run: (e) => e.chain().focus().toggleOrderedList().run(),
    },
    {
        icon: 'quote',
        label: 'ข้อความอ้างอิง',
        isActive: (e) => e.isActive('blockquote'),
        run: (e) => e.chain().focus().toggleBlockquote().run(),
    },
    'sep',
    {
        icon: 'undo',
        label: 'เลิกทำ',
        run: (e) => e.chain().focus().undo().run(),
        disabled: (e) => !e.can().undo(),
    },
    {
        icon: 'redo',
        label: 'ทำซ้ำ',
        run: (e) => e.chain().focus().redo().run(),
        disabled: (e) => !e.can().redo(),
    },
];
function Toolbar({ editor }: {
    editor: Editor;
}) {
    const [linking, setLinking] = useState(false);
    const [url, setUrl] = useState('');
    const state = useEditorState({
        editor,
        selector: ({ editor: e }) => ({
            active: TOOLS.map((t) => (t === 'sep' ? false : (t.isActive?.(e) ?? false))),
            disabled: TOOLS.map((t) => (t === 'sep' ? false : (t.disabled?.(e) ?? false))),
            link: e.isActive('link'),
        }),
    });
    const applyLink = () => {
        const href = url.trim();
        const chain = editor.chain().focus().extendMarkRange('link');
        if (href)
            chain.setLink({ href }).run();
        else
            chain.unsetLink().run();
        setLinking(false);
    };
    return (<>
      <div className="ida-richtext__toolbar" role="toolbar" aria-label="จัดรูปแบบข้อความ">
        {TOOLS.map((tool, index) => tool === 'sep' ? (<span key={`sep-${index}`} className="ida-richtext__sep" aria-hidden="true"/>) : (<button key={tool.icon} type="button" className="ida-richtext__tool" aria-label={tool.label} title={tool.label} aria-pressed={tool.isActive ? state.active[index] : undefined} disabled={state.disabled[index]} onClick={() => tool.run(editor)}>
              <Icon name={tool.icon} size={18}/>
            </button>))}
        <span className="ida-richtext__sep" aria-hidden="true"/>
        <button type="button" className="ida-richtext__tool" aria-label="ลิงก์" title="ลิงก์" aria-pressed={state.link} aria-expanded={linking} onClick={() => {
            setUrl((editor.getAttributes('link').href as string | undefined) ?? '');
            setLinking((open) => !open);
        }}>
          <Icon name="link" size={18}/>
        </button>
      </div>

      {linking && (<div className="ida-richtext__linkbar">
          <input className="ida-input" type="url" inputMode="url" placeholder="https://" aria-label="ที่อยู่ลิงก์ (เว้นว่างเพื่อเอาลิงก์ออก)" value={url} autoFocus onChange={(event) => setUrl(event.target.value)} onKeyDown={(event) => {
                if (event.key === 'Enter') {
                    event.preventDefault();
                    applyLink();
                }
                if (event.key === 'Escape')
                    setLinking(false);
            }}/>
          <button type="button" className="ida-btn ida-btn--secondary ida-btn--sm" onClick={applyLink}>
            {url.trim() ? 'ใส่ลิงก์' : 'เอาลิงก์ออก'}
          </button>
          <button type="button" className="ida-btn ida-btn--ghost ida-btn--sm" onClick={() => setLinking(false)}>
            ยกเลิก
          </button>
        </div>)}
    </>);
}
