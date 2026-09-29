import type { SelectOption } from '../../components/form/Select';
export const GENDER_OPTIONS: readonly SelectOption[] = [
    { value: 'M', label: 'ชาย' },
    { value: 'F', label: 'หญิง' },
    { value: 'U', label: 'ไม่ระบุ' },
];
export const ID_DOC_TYPE_OPTIONS: readonly SelectOption[] = [
    { value: 'NATIONAL_ID', label: 'บัตรประชาชน' },
    { value: 'PASSPORT', label: 'หนังสือเดินทาง' },
];
export const TAX_ENTITY_TYPE_OPTIONS: readonly SelectOption[] = [
    { value: 'INDIVIDUAL', label: 'บุคคลธรรมดา' },
    { value: 'JURISTIC', label: 'นิติบุคคล' },
];
export const APPROVAL_STATUS_OPTIONS: readonly SelectOption[] = [
    { value: 'DRAFT', label: 'ร่าง' },
    { value: 'PENDING', label: 'รออนุมัติ' },
    { value: 'APPROVED', label: 'อนุมัติแล้ว' },
    { value: 'RETURNED', label: 'ส่งกลับให้แก้ไข' },
    { value: 'REJECTED', label: 'ไม่อนุมัติ' },
    { value: 'CANCELLED', label: 'ยกเลิกคำขอ' },
];
export const RELATION_GROUP_OPTIONS: readonly SelectOption[] = [
    { value: 'FAMILY', label: 'ครอบครัว' },
    { value: 'RELATIVE', label: 'ญาติแพทย์' },
    { value: 'REFERENCE', label: 'บุคคลอ้างอิง' },
];
export const LICENSE_TYPE_OPTIONS: readonly SelectOption[] = [
    { value: 'MEDICAL', label: 'ใบประกอบวิชาชีพเวชกรรม' },
    { value: 'DENTAL', label: 'ใบประกอบวิชาชีพทันตกรรม' },
    { value: 'SPECIALTY_BOARD', label: 'วุฒิบัตร/อนุมัติบัตร' },
    { value: 'OTHER', label: 'อื่น ๆ' },
];
export const CONTACT_TYPE_OPTIONS: readonly SelectOption[] = [
    { value: 'MOBILE', label: 'เบอร์มือถือ' },
    { value: 'PHONE', label: 'เบอร์โทรศัพท์' },
    { value: 'OTHER_PHONE', label: 'เบอร์โทรอื่น ๆ' },
    { value: 'EMAIL', label: 'อีเมล' },
    { value: 'EMAIL_ALT', label: 'อีเมลสำรอง' },
    { value: 'LINE', label: 'LINE' },
];
export const ADDRESS_TYPE_OPTIONS: readonly SelectOption[] = [
    { value: 'HOME', label: 'ที่อยู่บ้าน' },
    { value: 'TAX', label: 'ที่อยู่สำหรับยื่นภาษี' },
    { value: 'MAILING', label: 'ที่อยู่สำหรับส่งเอกสาร' },
    { value: 'WORK', label: 'ที่อยู่ที่ทำงาน' },
];
export const EMPLOYMENT_STATUS_OPTIONS: readonly SelectOption[] = [
    { value: 'WORKING', label: 'ปฏิบัติงานอยู่' },
    { value: 'RESIGNED', label: 'ลาออก' },
    { value: 'SUSPENDED', label: 'พักงาน' },
];
export const WELFARE_SCOPE_OPTIONS: readonly SelectOption[] = [
    { value: 'NONE', label: 'ไม่มีสวัสดิการ' },
    { value: 'DOCTOR_ONLY', label: 'สำหรับแพทย์เท่านั้น' },
    { value: 'DOCTOR_AND_FAMILY', label: 'สำหรับแพทย์และครอบครัว' },
];
export const BOARD_STATUS_OPTIONS: readonly SelectOption[] = [
    { value: 'BOARD', label: 'จบ Board' },
    { value: 'NON_BOARD', label: 'ไม่จบ Board' },
];
export const WHT_FORM_OPTIONS: readonly SelectOption[] = [
    { value: 'PND3', label: 'ภ.ง.ด.3 หักภาษี ณ ที่จ่าย 3%' },
    { value: 'PND53', label: 'ภ.ง.ด.53 หักภาษี ณ ที่จ่าย 3%' },
];
export const ACCOUNT_TYPE_OPTIONS: readonly SelectOption[] = [
    { value: 'SAVING', label: 'ออมทรัพย์' },
    { value: 'CURRENT', label: 'กระแสรายวัน' },
    { value: 'FIXED', label: 'ฝากประจำ' },
];
export const CONTRACT_TYPE_OPTIONS: readonly SelectOption[] = [
    { value: 'PRACTICE_SPACE', label: 'สัญญาเช่าพื้นที่ประกอบวิชาชีพ' },
    { value: 'EMPLOYMENT', label: 'สัญญาจ้าง' },
    { value: 'GUARANTEE_INCOME', label: 'สัญญาประกันรายได้' },
    { value: 'SERVICE', label: 'สัญญาบริการ' },
    { value: 'OTHER', label: 'อื่น ๆ' },
];
export const CONTRACT_STATUS_OPTIONS: readonly SelectOption[] = [
    { value: 'ACTIVE', label: 'มีผลอยู่' },
    { value: 'EXPIRED', label: 'หมดอายุ' },
    { value: 'TERMINATED', label: 'ยกเลิกก่อนกำหนด' },
];
