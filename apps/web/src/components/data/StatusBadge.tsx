import { Icon } from '../Icon';
import type { IconName } from '../Icon';
const BADGES: Record<string, {
    variant: string;
    icon: IconName;
    label: string;
}> = {
    ACTIVE: { variant: 'ida-badge--success', icon: 'check', label: 'ใช้งาน' },
    INACTIVE: { variant: 'ida-badge--closed', icon: 'ban', label: 'ไม่ใช้งาน' },
    DRAFT: { variant: 'ida-badge--info', icon: 'pencil', label: 'ร่าง' },
    PENDING: { variant: 'ida-badge--pending', icon: 'clock', label: 'รออนุมัติ' },
    APPROVED: { variant: 'ida-badge--success', icon: 'check', label: 'อนุมัติแล้ว' },
    RETURNED: { variant: 'ida-badge--pending', icon: 'undo', label: 'ส่งกลับให้แก้ไข' },
    REJECTED: { variant: 'ida-badge--error', icon: 'close', label: 'ไม่อนุมัติ' },
    CANCELLED: { variant: 'ida-badge--closed', icon: 'ban', label: 'ยกเลิกคำขอ' },
    Running: { variant: 'ida-badge--pending', icon: 'clock', label: 'กำลังทำงาน' },
    Published: { variant: 'ida-badge--success', icon: 'check', label: 'สำเร็จ' },
    Failed: { variant: 'ida-badge--error', icon: 'close', label: 'ล้มเหลว' },
    Withdrawn: { variant: 'ida-badge--closed', icon: 'undo', label: 'ถอนแล้ว' },
    Pending: { variant: 'ida-badge--pending', icon: 'clock', label: 'รอตรวจ' },
    Valid: { variant: 'ida-badge--success', icon: 'check', label: 'ผ่าน' },
    Invalid: { variant: 'ida-badge--error', icon: 'close', label: 'ไม่ผ่าน' },
    Insert: { variant: 'ida-badge--success', icon: 'plus', label: 'เพิ่มใหม่' },
    Update: { variant: 'ida-badge--info', icon: 'pencil', label: 'แก้ไข' },
    Duplicate: { variant: 'ida-badge--closed', icon: 'layers', label: 'ซ้ำ' },
    Rejected: { variant: 'ida-badge--error', icon: 'close', label: 'ไม่ผ่าน' },
    Matched: { variant: 'ida-badge--success', icon: 'check', label: 'ตรงกัน' },
};
export function StatusBadge({ status }: {
    status: string;
}) {
    const badge = BADGES[status];
    if (!badge)
        return <span className="ida-badge">{status}</span>;
    return (<span className={`ida-badge ${badge.variant}`} data-status={status}>
      <Icon name={badge.icon} size={14}/>
      {badge.label}
    </span>);
}
