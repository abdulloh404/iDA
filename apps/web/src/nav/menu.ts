import { MASTER_DATA_SCREENS } from '../features/master-data/registry';
export interface MenuItem {
    label: string;
    labelEn?: string;
    figure: string;
    addition?: true;
    screenId?: string;
    path?: string;
    requires?: string;
    allowedRoles?: readonly string[];
}
export interface MenuGroup {
    label: string;
    labelEn?: string;
    addition?: true;
    items: readonly MenuItem[];
}
export const MENU: readonly MenuGroup[] = [
    {
        label: 'หน้าหลัก',
        items: [{ label: 'หน้าหลัก', figure: '1', path: '/' }],
    },
    {
        label: 'Dashboard',
        items: [{ label: 'Dashboard', figure: '2' }],
    },
    {
        label: 'คำขอและการอนุมัติ',
        items: [
            {
                label: 'คำขอของฉัน',
                figure: '3',
                path: '/approvals/mine',
                requires: 'approvals.read',
            },
            {
                label: 'รายการรอดำเนินการ',
                figure: '4',
                path: '/approvals/pending',
                requires: 'approvals.approve',
            },
            {
                label: 'ประวัติคำขอ',
                figure: '5',
                path: '/approvals/history',
                requires: 'approvals.read',
            },
        ],
    },
    {
        label: 'ข้อมูลหลักทั่วไป',
        items: [
            { label: 'ความเชี่ยวชาญ', figure: '6', screenId: 'specialty' },
            { label: 'ความเชี่ยวชาญเฉพาะทาง', figure: '7', screenId: 'sub-specialty' },
            { label: 'สาขาโรงพยาบาล', figure: '8', screenId: 'hospital' },
            { label: 'แผนกตามศูนย์รายได้ค่าใช้จ่าย', figure: '9', screenId: 'department' },
            { label: 'คลินิก', figure: '10', screenId: 'clinic' },
            { label: 'ประเภทแพทย์', figure: '11', screenId: 'doctor-type' },
            { label: 'ข้อมูลกลุ่มแพทย์', figure: '12', screenId: 'doctor-group' },
            { label: 'ข้อมูล Status Privilege', figure: '13', screenId: 'status-privilege' },
            { label: 'ข้อมูล Privilege Type', figure: '14', screenId: 'privilege-type' },
            { label: 'ข้อมูล Privilege SubType', figure: '15', screenId: 'privilege-subtype' },
            { label: 'คำนำหน้าชื่อ', figure: '16', screenId: 'title' },
        ],
    },
    {
        label: 'ข้อมูลหลักทางบัญชี',
        items: [
            { label: 'ข้อมูลธนาคาร', figure: '17', screenId: 'bank' },
            { label: 'ข้อมูลสาขาธนาคาร', figure: '18', screenId: 'bank-branch' },
            { label: 'ข้อมูลรายได้และรายการหัก', figure: '19', screenId: 'income-deduction-item' },
            { label: 'ข้อมูล Treatment', figure: '20', screenId: 'treatment' },
            { label: 'ข้อมูล Treatment Category', figure: '21', screenId: 'treatment-category' },
            { label: 'ข้อมูลประเภทการจ่ายเงิน', figure: '22', screenId: 'payment-type' },
            { label: 'ข้อมูลประเภทการรับเงิน', figure: '23', screenId: 'receipt-type' },
            { label: 'AR Code', figure: '24', screenId: 'ar-code' },
            { label: 'ข้อมูล Category', figure: '25', screenId: 'share-category' },
            { label: 'ข้อมูลตั้งค่าบันทึกบัญชี', figure: '26', screenId: 'gl-posting-setup' },
            { label: 'ตั้งค่ารายการไม่รอรับชำระ', figure: '27', screenId: 'no-wait-payment-rule' },
        ],
    },
    {
        label: 'ข้อมูลหลัก 40(2)',
        items: [
            { label: 'ประเภทเงินได้', figure: '28', screenId: 'income-type-402' },
            { label: 'ประเภทรายการปรับปรุง', figure: '29', screenId: 'adjustment-type' },
            { label: 'ประเภทรายการค่าใช้จ่าย', figure: '30', screenId: 'expense-type' },
            { label: 'เงื่อนไขภาษีเงินได้บุคคลธรรมดา', figure: '31', screenId: 'pit-tax-bracket' },
            { label: 'ตั้งค่าประเภทลดหย่อน', figure: '32', screenId: 'tax-allowance-type' },
        ],
    },
    {
        label: 'ข้อมูลหลัก 40(6)',
        items: [
            { label: 'ข้อมูล Import Invoice', figure: '33', screenId: 'invoice-prefix-rule' },
            { label: 'ข้อมูล Invoice AR/Cash', figure: '34', screenId: 'invoice-ar-cash-rule' },
        ],
    },
    {
        label: 'จัดการข้อมูลแพทย์',
        items: [
            { label: 'ข้อมูลประวัติแพทย์', figure: '35', screenId: 'doctor' },
            { label: 'ข้อมูลรหัสแพทย์', figure: '36', screenId: 'doctor-code' },
            { label: 'สวัสดิการแพทย์', figure: '37', screenId: 'doctor-welfare' },
            { label: 'แผนสวัสดิการแพทย์', figure: '37a', addition: true, screenId: 'welfare-plan' },
        ],
    },
    {
        label: 'ส่วนแบ่งค่าแพทย์ · Premium',
        items: [
            { label: 'ระดับ Private Case', figure: '38', screenId: 'premium-private-case' },
            { label: 'ระดับ สิทธิ์ & AR Code', figure: '39', screenId: 'premium-patient-right' },
            { label: 'ระดับ Package', figure: '40', screenId: 'premium-package' },
            {
                label: 'ระดับ Doctor Treatment Department',
                figure: '41',
                screenId: 'premium-doctor-treatment-dept',
            },
            { label: 'ระดับ Doctor Treatment', figure: '42', screenId: 'premium-doctor-treatment' },
            { label: 'ระดับ Category Treatment', figure: '43', screenId: 'premium-category-treatment' },
            { label: 'ระดับ Treatment', figure: '44', screenId: 'premium-treatment' },
            { label: 'ระดับ Category', figure: '45', screenId: 'premium-category' },
        ],
    },
    {
        label: 'ส่วนแบ่งค่าแพทย์ · ประกันสังคม',
        items: [
            { label: 'ระดับ AR Code', figure: '46', screenId: 'social-ar-code' },
            { label: 'ระดับ Doctor Treatment', figure: '47', screenId: 'social-doctor-treatment' },
            {
                label: 'ระดับ Department Treatment',
                figure: '48',
                screenId: 'social-department-treatment',
            },
            { label: 'ระดับ Doctor Activity', figure: '49', screenId: 'social-doctor-activity' },
            { label: 'ระดับ Treatment', figure: '50', screenId: 'social-treatment' },
            { label: 'ระดับ Activity', figure: '51', screenId: 'social-activity' },
            { label: 'ส่วนแบ่งหลัก', figure: '52', screenId: 'social-base' },
        ],
    },
    {
        label: 'อัตราค่าเวรและประกันรายได้',
        items: [
            {
                label: 'อัตราค่าเวรวันหยุดเทศกาล',
                figure: '53',
                screenId: 'holiday-duty-rate',
            },
            { label: 'อัตราค่าแพทย์เวร', figure: '54', screenId: 'duty-rate' },
            { label: 'อัตราค่าแพทย์เหมาจ่าย', figure: '55', screenId: 'guarantee-lump-sum' },
            { label: 'อัตราค่าแพทย์ Surplus', figure: '56', screenId: 'guarantee-surplus' },
            {
                label: 'อัตราประกันรายได้ รายชั่วโมง',
                figure: '57',
                screenId: 'guarantee-hourly',
            },
            {
                label: 'อัตราประกันรายได้ รายคาบ',
                figure: '58',
                screenId: 'guarantee-per-session',
            },
            {
                label: 'อัตราประกันรายได้ รายเดือน',
                figure: '59',
                screenId: 'guarantee-monthly',
            },
        ],
    },
    {
        label: 'ตารางเวรและการลงชื่อเข้าเวร',
        items: [
            { label: 'ลงชื่อเข้าเวร', figure: '60', screenId: 'duty-checkin' },
            {
                label: 'ลงชื่อทำงานประกันรายได้รายชั่วโมง',
                figure: '61',
                screenId: 'duty-checkin-hourly',
            },
            {
                label: 'ลงชื่อทำงานประกันรายได้รายคาบ',
                figure: '62',
                screenId: 'duty-checkin-session',
            },
            {
                label: 'ลงชื่อทำงานประกันรายได้รายเดือน',
                figure: '63',
                screenId: 'duty-checkin-monthly',
            },
        ],
    },
    {
        label: 'จัดการค่าแพทย์ 40(2)',
        items: [
            { label: 'ค่าบริหาร / ตำแหน่ง', figure: '64', screenId: 'position-fee' },
            { label: 'ค่าแพทย์ออกหน่วยเหมาจ่าย', figure: '65', screenId: 'lump-sum-unit-fee' },
            { label: 'ค่าแพทย์ Out Clinic', figure: '66', screenId: 'out-clinic-fee' },
            { label: 'รายการค่าแพทย์', figure: '67', screenId: 'fee-item' },
            { label: 'ตั้งค่าภาษีโรงพยาบาลออกให้', figure: '68', screenId: 'hospital-paid-tax' },
            { label: 'ตั้งค่าภาษีลดหย่อน', figure: '69', screenId: 'tax-deduction' },
            { label: 'ข้อมูลยกเว้นภาษี', figure: '70', screenId: 'tax-exemption' },
        ],
    },
    {
        label: 'จัดการค่าแพทย์ 40(6)',
        items: [
            { label: 'ตรวจสอบรายการอ่านผล', figure: '71' },
            { label: 'ตรวจสอบรายการ', figure: '72' },
            { label: 'ปรับปรุงรายการ Invoice', figure: '73' },
            { label: 'ปรับปรุง Invoice หลายรายการ', figure: '74' },
            { label: 'ตั้งค่าขั้นบันไดหนี้สูญ', figure: '75', screenId: 'bad-debt-tier' },
            { label: 'ตัดหนี้สูญ', figure: '76' },
            { label: 'ตัดรับชำระ', figure: '77' },
            { label: 'รายการรอตัดรับชำระ', figure: '78' },
            { label: 'ยกเลิกตัดรับชำระ', figure: '79' },
            { label: 'คำนวณ Accrual No Invoice', figure: '80' },
            { label: 'คำนวณรายวัน', figure: '81' },
            { label: 'รายได้ค่าแพทย์ภาษี 40(6)', figure: '82' },
        ],
    },
    {
        label: 'การนำเข้าข้อมูล',
        labelEn: 'Data ingestion',
        addition: true,
        items: [
            {
                label: 'ตั้งค่าโดเมนและรอบดึงข้อมูล',
                labelEn: 'Ingest configuration',
                figure: 'IE1',
                addition: true,
                path: '/ingest/config',
                requires: 'ingest-config.read',
                allowedRoles: ['GROUP_ADMIN', 'HOSPITAL_ADMIN'],
            },
            {
                label: 'ประวัติและตรวจสอบการนำเข้า',
                figure: 'F-05/F-06',
                addition: true,
                path: '/ingest/batches',
                requires: 'ingest.read',
            },
        ],
    },
    {
        label: 'จัดการค่าแพทย์สิทธิ์ร่วม',
        items: [
            { label: 'คำนวณค่าแพทย์ประกันสิทธิ์ร่วม IPD', figure: '83' },
            { label: 'คำนวณค่าแพทย์ประกันสิทธิ์ร่วม OPD', figure: '84' },
            { label: 'แก้ไขรายการ (Operative Note) IPD', figure: '85' },
            { label: 'แก้ไขรายการ (Operative Note) OPD', figure: '86' },
        ],
    },
    {
        label: 'การปรับปรุงรายการ',
        items: [{ label: 'รายการปรับปรุง', figure: '87' }],
    },
    {
        label: 'คำนวณค่าแพทย์รายเดือน',
        items: [{ label: 'คำนวณค่าแพทย์รายเดือน', figure: '88' }],
    },
    {
        label: 'เอกสารรายได้',
        items: [
            { label: 'ตั้งค่าการออกสลิป', figure: '89', screenId: 'slip-setting' },
            { label: 'สลิปเงินเดือน', figure: '90' },
            { label: 'หนังสือรับรองภาษีเงินได้ 40(6)', figure: '91' },
            { label: 'หนังสือรับรอง 50 ทวิ (ภ.ง.ด.1ก)', figure: '92' },
        ],
    },
    {
        label: 'สรรพากร',
        items: [
            { label: 'รายการภาษีเงินได้หัก ณ ที่จ่าย (ภ.ง.ด.1)', figure: '93' },
            { label: 'รายการภาษีเงินได้หัก ณ ที่จ่าย (ภ.ง.ด.1ก)', figure: '94' },
        ],
    },
    {
        label: 'จัดการผู้ใช้',
        items: [
            { label: 'จัดการผู้ใช้งาน', figure: '95', screenId: 'user' },
            { label: 'จัดการสิทธิ์', figure: '96', screenId: 'role' },
        ],
    },
    {
        label: 'ตั้งค่าระบบ',
        items: [
            { label: 'ตั้งค่ารูปแบบอีเมล', figure: '97', screenId: 'email-template' },
            { label: 'ตั้งค่าอีเมล HIS', figure: '98', screenId: 'his-notify-email' },
            { label: 'ตั้งค่าการส่งเอกสารรายได้', figure: '99', screenId: 'income-doc-setting' },
            { label: 'ตั้งค่ารหัสแพทย์ไปเป็นแพทย์กลาง', figure: '100', screenId: 'his-doctor-code-map' },
            { label: 'ตั้งค่าแจ้งเตือนรายการใกล้หมดอายุ', figure: '102', screenId: 'expiry-alert-setting' },
            { label: 'ตั้งค่าการรีเซ็ตรหัสผ่าน', figure: '103', screenId: 'password-policy' },
            { label: 'ตั้งค่าพื้นที่การลงชื่อเข้าเวร', figure: '104', screenId: 'checkin-area' },
            { label: 'ตั้งค่าข้อกำหนดและเงื่อนไข', figure: '105', screenId: 'terms' },
        ],
    },
];
export interface ResolvedMenuItem extends MenuItem {
    to?: string;
}
const SCREENS_BY_ID = new Map(MASTER_DATA_SCREENS.map((s) => [s.id, s]));
export function resolveMenu(): {
    label: string;
    labelEn?: string;
    items: ResolvedMenuItem[];
}[] {
    return MENU.map((group) => ({
        label: group.label,
        labelEn: group.labelEn,
        items: group.items.map((item) => {
            if (!item.screenId)
                return { ...item, to: item.path };
            const screen = SCREENS_BY_ID.get(item.screenId);
            if (!screen) {
                throw new Error(`เมนู "${item.label}" (รูป ${item.figure}) อ้าง screenId "${item.screenId}" ` +
                    'ซึ่งไม่มีใน MASTER_DATA_SCREENS');
            }
            return { ...item, to: screen.path, requires: `${screen.resource}.read` };
        }),
    }));
}
