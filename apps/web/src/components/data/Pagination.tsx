import { Select } from '../form/Select';
import type { Paged } from '../../api/types';
interface PaginationProps {
    page: Paged<unknown> | undefined;
    onPageChange: (page: number) => void;
    onPageSizeChange: (pageSize: number) => void;
}
const PAGE_SIZES = [10, 25, 50, 100];
export function Pagination({ page, onPageChange, onPageSizeChange }: PaginationProps) {
    if (!page || page.total === 0)
        return null;
    const first = (page.page - 1) * page.pageSize + 1;
    const last = Math.min(page.page * page.pageSize, page.total);
    return (<nav className="ida-pagination" aria-label="แบ่งหน้า">
      <span className="ida-pagination__summary">
        แสดง {first.toLocaleString('th-TH')}–{last.toLocaleString('th-TH')} จาก{' '}
        {page.total.toLocaleString('th-TH')} รายการ
      </span>

      <span className="ida-pagination__size">
        <span className="ida-caption" id="page-size-label">
          แถวต่อหน้า
        </span>
        <Select ariaLabel="แถวต่อหน้า" className="ida-select-trigger--compact" value={String(page.pageSize)} onChange={(size) => onPageSizeChange(Number(size))} options={PAGE_SIZES.map((size) => ({ value: String(size), label: String(size) }))}/>
      </span>

      <button type="button" className="ida-btn ida-btn--secondary ida-btn--sm" onClick={() => onPageChange(page.page - 1)} disabled={page.page <= 1}>
        ก่อนหน้า
      </button>

      <span className="ida-caption">
        หน้า {page.page} / {page.totalPages}
      </span>

      <button type="button" className="ida-btn ida-btn--secondary ida-btn--sm" onClick={() => onPageChange(page.page + 1)} disabled={page.page >= page.totalPages}>
        ถัดไป
      </button>
    </nav>);
}
