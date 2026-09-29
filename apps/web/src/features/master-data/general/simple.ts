import { simpleMasterScreen } from '../simple-master';
const BREADCRUMB = [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลักทั่วไป' }] as const;
export const doctorTypeScreen = simpleMasterScreen({
    id: 'doctor-type',
    resource: 'doctor-types',
    path: '/master-data/general/doctor-type',
    titleTh: 'ประเภทแพทย์',
    breadcrumb: BREADCRUMB,
    codeLabel: 'รหัสประเภทแพทย์',
    nameLabel: 'ประเภทแพทย์',
    emptyHint: 'ประเภทแพทย์ เช่น AS แพทย์ประจำ CS แพทย์ที่ปรึกษา PT Part Time — แต่ละโรงพยาบาลกำหนดเอง',
});
export const statusPrivilegeScreen = simpleMasterScreen({
    id: 'status-privilege',
    resource: 'status-privileges',
    path: '/master-data/general/status-privilege',
    titleTh: 'ข้อมูล Status Privilege',
    breadcrumb: BREADCRUMB,
    codeLabel: 'รหัส Privilege',
    nameLabel: 'Privilege',
    emptyHint: 'สถานะสิทธิ์การทำหัตถการของแพทย์ เช่น Full, Provisional, Temporary',
});
export const privilegeTypeScreen = simpleMasterScreen({
    id: 'privilege-type',
    resource: 'privilege-types',
    path: '/master-data/general/privilege-type',
    titleTh: 'ข้อมูล Privilege Type',
    breadcrumb: BREADCRUMB,
    codeLabel: 'รหัส Privilege Type',
    nameLabel: 'Privilege Type',
    emptyHint: 'ประเภทสิทธิ์การทำหัตถการ เป็นหัวของ Privilege SubType',
});
