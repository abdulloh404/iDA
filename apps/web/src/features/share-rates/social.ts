import { shareRateScreen } from './shared';
const treatmentColumn = {
    key: 'treatmentCode' as const,
    header: 'Treatment',
    width: '160px',
    value: (row: {
        treatmentCode: string | null;
    }) => row.treatmentCode ?? 'ทุก Treatment',
};
const treatmentField = {
    kind: 'lookup' as const,
    name: 'treatmentId',
    label: 'Treatment',
    resource: 'treatments' as const,
    emptyLabel: 'ทุก Treatment',
};
export const socialArCodeShareScreen = shareRateScreen({
    id: 'social-ar-code',
    level: 'SOCIAL_AR_CODE',
    group: 'ประกันสังคม',
    path: '/share-rates/social/ar-code',
    titleTh: 'ส่วนแบ่งประกันสังคม ระดับ AR Code',
    searchHint: 'ค้นหารหัสแพทย์หรือรหัส Treatment',
    emptyHint: 'อัตราตามคู่สัญญาประกันสังคม เว้นแพทย์หรือ Treatment ไว้ = ทุกรายการ',
    socialKind: true,
    withFixAmount: true,
    keyFilters: [
        { kind: 'lookup', name: 'arCodeId', label: 'AR Code', resource: 'ar-codes' },
        { kind: 'lookup', name: 'doctorCodeId', label: 'แพทย์', resource: 'doctor-codes' },
        { kind: 'lookup', name: 'treatmentId', label: 'Treatment', resource: 'treatments' },
    ],
    keyColumns: [
        { key: 'arCode', header: 'AR Code', width: '150px' },
        {
            key: 'doctorCode',
            header: 'แพทย์',
            width: '150px',
            value: (row) => row.doctorCode ?? 'ทุกแพทย์',
        },
        treatmentColumn,
    ],
    keyFields: [
        {
            kind: 'lookup',
            name: 'arCodeId',
            label: 'AR Code',
            resource: 'ar-codes',
            required: true,
            width: 'lg',
        },
        {
            kind: 'lookup',
            name: 'doctorCodeId',
            label: 'แพทย์',
            resource: 'doctor-codes',
            emptyLabel: 'ทุกแพทย์',
        },
        treatmentField,
    ],
});
export const socialDoctorTreatmentShareScreen = shareRateScreen({
    id: 'social-doctor-treatment',
    level: 'SOCIAL_DOCTOR_TREATMENT',
    group: 'ประกันสังคม',
    path: '/share-rates/social/doctor-treatment',
    titleTh: 'ส่วนแบ่งประกันสังคม ระดับ Doctor Treatment',
    searchHint: 'ค้นหารหัสแพทย์หรือรหัส Treatment',
    emptyHint: 'อัตรารายแพทย์สำหรับคนไข้ประกันสังคม เว้น Treatment ไว้ = ทุกรายการ',
    socialKind: true,
    withFixAmount: true,
    keyFilters: [
        { kind: 'lookup', name: 'doctorCodeId', label: 'แพทย์', resource: 'doctor-codes' },
        { kind: 'lookup', name: 'treatmentId', label: 'Treatment', resource: 'treatments' },
    ],
    keyColumns: [
        { key: 'doctorCode', header: 'แพทย์', sortable: true, width: '170px' },
        treatmentColumn,
    ],
    keyFields: [
        {
            kind: 'lookup',
            name: 'doctorCodeId',
            label: 'แพทย์',
            resource: 'doctor-codes',
            required: true,
            width: 'lg',
        },
        treatmentField,
    ],
});
export const socialDepartmentTreatmentShareScreen = shareRateScreen({
    id: 'social-department-treatment',
    level: 'SOCIAL_DEPARTMENT_TREATMENT',
    group: 'ประกันสังคม',
    path: '/share-rates/social/department-treatment',
    titleTh: 'ส่วนแบ่งประกันสังคม ระดับ Department Treatment',
    searchHint: 'ค้นหารหัส Treatment',
    emptyHint: 'อัตรารายแผนกสำหรับคนไข้ประกันสังคม เว้น Treatment ไว้ = ทุกรายการ',
    socialKind: true,
    withFixAmount: true,
    keyFilters: [
        { kind: 'lookup', name: 'departmentId', label: 'Department', resource: 'departments' },
        { kind: 'lookup', name: 'treatmentId', label: 'Treatment', resource: 'treatments' },
    ],
    keyColumns: [
        { key: 'departmentName', header: 'Department', width: '200px' },
        treatmentColumn,
        {
            key: 'doctorCode',
            header: 'แพทย์',
            width: '150px',
            value: (row) => row.doctorCode ?? 'ทุกแพทย์',
        },
    ],
    keyFields: [
        {
            kind: 'lookup',
            name: 'departmentId',
            label: 'Department',
            resource: 'departments',
            required: true,
            width: 'lg',
        },
        treatmentField,
        {
            kind: 'lookup',
            name: 'doctorCodeId',
            label: 'แพทย์',
            resource: 'doctor-codes',
            emptyLabel: 'ทุกแพทย์',
        },
    ],
});
export const socialDoctorActivityShareScreen = shareRateScreen({
    id: 'social-doctor-activity',
    level: 'SOCIAL_DOCTOR_ACTIVITY',
    group: 'ประกันสังคม',
    path: '/share-rates/social/doctor-activity',
    titleTh: 'ส่วนแบ่งประกันสังคม ระดับ Doctor Activity',
    searchHint: 'ค้นหารหัสแพทย์หรือรหัส Activity',
    emptyHint: 'อัตรารายแพทย์ตามรหัส Activity ของ HIS',
    socialKind: true,
    withFixAmount: true,
    keyFilters: [{ kind: 'lookup', name: 'doctorCodeId', label: 'แพทย์', resource: 'doctor-codes' }],
    keyColumns: [
        { key: 'doctorCode', header: 'แพทย์', sortable: true, width: '170px' },
        { key: 'activityCode', header: 'Activity', width: '160px' },
    ],
    keyFields: [
        {
            kind: 'lookup',
            name: 'doctorCodeId',
            label: 'แพทย์',
            resource: 'doctor-codes',
            required: true,
            width: 'lg',
        },
        { kind: 'text', name: 'activityCode', label: 'รหัส Activity', required: true },
    ],
});
export const socialTreatmentShareScreen = shareRateScreen({
    id: 'social-treatment',
    level: 'SOCIAL_TREATMENT',
    group: 'ประกันสังคม',
    path: '/share-rates/social/treatment',
    titleTh: 'ส่วนแบ่งประกันสังคม ระดับ Treatment',
    searchHint: 'ค้นหารหัส Treatment',
    emptyHint: 'อัตรารายรายการรักษาสำหรับคนไข้ประกันสังคม ใช้เมื่อไม่มีอัตรารายแพทย์หรือรายแผนก',
    socialKind: true,
    withFixAmount: true,
    keyFilters: [{ kind: 'lookup', name: 'treatmentId', label: 'Treatment', resource: 'treatments' }],
    keyColumns: [{ key: 'treatmentCode', header: 'Treatment', sortable: true, width: '180px' }],
    keyFields: [
        {
            kind: 'lookup',
            name: 'treatmentId',
            label: 'Treatment',
            resource: 'treatments',
            required: true,
            width: 'lg',
        },
    ],
});
export const socialActivityShareScreen = shareRateScreen({
    id: 'social-activity',
    level: 'SOCIAL_ACTIVITY',
    group: 'ประกันสังคม',
    path: '/share-rates/social/activity',
    titleTh: 'ส่วนแบ่งประกันสังคม ระดับ Activity',
    searchHint: 'ค้นหารหัส Activity',
    emptyHint: 'อัตราตามรหัส Activity ของ HIS ใช้เมื่อไม่มีอัตรารายแพทย์',
    socialKind: true,
    withFixAmount: true,
    keyColumns: [{ key: 'activityCode', header: 'Activity', width: '180px' }],
    keyFields: [{ kind: 'text', name: 'activityCode', label: 'รหัส Activity', required: true }],
});
export const socialBaseShareScreen = shareRateScreen({
    id: 'social-base',
    level: 'SOCIAL_BASE',
    group: 'ประกันสังคม',
    path: '/share-rates/social/base',
    titleTh: 'ส่วนแบ่งประกันสังคม ส่วนแบ่งหลัก',
    searchHint: 'ค้นหารายละเอียด',
    emptyHint: 'ส่วนแบ่งที่ใช้เมื่อไม่มีอัตราระดับใดตรงเลย',
    socialKind: true,
    withFixAmount: true,
    keyColumns: [{ key: 'detail', header: 'รายละเอียด' }],
    keyFields: [{ kind: 'text', name: 'detail', label: 'รายละเอียด', width: 'lg' }],
});
