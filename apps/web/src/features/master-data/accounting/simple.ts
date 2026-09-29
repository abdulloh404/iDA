import { simpleMasterScreen } from '../simple-master';
export const shareCategoryScreen = simpleMasterScreen({
    id: 'share-category',
    resource: 'share-categories',
    path: '/master-data/accounting/share-category',
    titleTh: 'ข้อมูล Category',
    breadcrumb: [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลักทางบัญชี' }],
    codeLabel: 'รหัสประเภทส่วนแบ่ง',
    nameLabel: 'ประเภทส่วนแบ่ง',
    emptyHint: 'ประเภทส่วนแบ่งเป็นตัวเชื่อมระหว่างรายการค่าแพทย์กับผังบัญชีของ Oracle',
});
