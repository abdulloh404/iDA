import { Icon } from '../../components/Icon';
export function IngestPermissionState({ detail = false }: { detail?: boolean }) {
    return (<div className="ida-table-wrap">
      <div className="ida-empty">
        <div className="ida-empty__icon">
          <Icon name="shield" size={28}/>
        </div>
        <p className="ida-empty__title">
          {detail
            ? 'บัญชีนี้ยังไม่มีสิทธิ์ดูรายละเอียดการนำเข้า'
            : 'บัญชีนี้ยังไม่มีสิทธิ์ดูประวัติการนำเข้า'}
        </p>
        <p className="ida-empty__hint">
          ต้องมีสิทธิ์ ingest.read จึงจะดู batch/run และผลตรวจสอบย้อนหลังได้
        </p>
      </div>
    </div>);
}
