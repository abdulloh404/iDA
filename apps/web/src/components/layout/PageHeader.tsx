import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
export interface Crumb {
    label: string;
    to?: string;
}
export function Breadcrumbs({ items }: {
    items: readonly Crumb[];
}) {
    if (items.length === 0)
        return null;
    return (<nav className="ida-breadcrumbs" aria-label="เส้นทาง">
      {items.map((crumb, index) => (<span key={`${crumb.label}-${index}`}>
          {index > 0 && <span aria-hidden="true">/ </span>}
          {crumb.to ? <Link to={crumb.to}>{crumb.label}</Link> : <span>{crumb.label}</span>}
        </span>))}
    </nav>);
}
export function PageHeader({ title, description, breadcrumbs = [], actions, }: {
    title: string;
    description?: string;
    breadcrumbs?: readonly Crumb[];
    actions?: ReactNode;
}) {
    return (<>
      <Breadcrumbs items={breadcrumbs}/>
      <div className="ida-page-header">
        
        <div className="ida-page-header__text">
          <h1 className="ida-page-header__title">{title}</h1>
          {description && <p className="ida-page-header__description">{description}</p>}
        </div>
        {actions && <div className="ida-toolbar">{actions}</div>}
      </div>
    </>);
}
