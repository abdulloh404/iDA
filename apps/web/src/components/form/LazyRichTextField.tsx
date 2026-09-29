import { Suspense, lazy } from 'react';
import type { ComponentProps } from 'react';
const RichTextField = lazy(() => import('./RichTextField').then((module) => ({ default: module.RichTextField })));
export function LazyRichTextField(props: ComponentProps<typeof RichTextField>) {
    return (<Suspense fallback={<div className={`ida-field ida-field--${props.width ?? 'full'}`}>
          <span className="ida-label">{props.label}</span>
          <span className="ida-skeleton" style={{ height: '240px', display: 'block' }}/>
        </div>}>
      <RichTextField {...props}/>
    </Suspense>);
}
