import { z } from 'zod';
import { createCrudApi } from '../../../api/crud';
import type { RecordStatus } from '../../../api/types';
import { REMARK_FIELD, STATUS_COLUMN, STATUS_FIELD, omitFields, } from '../descriptor';
import type { ScreenDescriptor } from '../descriptor';
export interface HospitalListItem {
    id: string;
    nameTh: string;
    nameEn: string | null;
    shortName: string | null;
    branchNo: string | null;
    doctorCodePrefix: string | null;
    isHeadOffice: boolean;
    status: RecordStatus;
}
export interface HospitalDetail extends HospitalListItem {
    glPostCode: string | null;
    setOfBooks: string | null;
    ekgTreatmentPrefix: string | null;
    taxId: string | null;
    taxAddrNo: string | null;
    taxAddrBuilding: string | null;
    taxAddrSoi: string | null;
    taxAddrRoad: string | null;
    taxAddrSubdistrict: string | null;
    taxAddrDistrict: string | null;
    taxAddrProvince: string | null;
    taxAddrPostcode: string | null;
    taxAddrCountry: string | null;
    contactEmail: string | null;
    contactPhone: string | null;
    remark: string | null;
    rowVersion: string;
}
export type HospitalInput = Omit<HospitalDetail, 'rowVersion'>;
const optionalText = z.string().trim().nullable();
const hospitalSchema = z.object({
    id: z
        .string()
        .trim()
        .min(1, 'โปรดระบุรหัสโรงพยาบาล')
        .max(20, 'รหัสโรงพยาบาลต้องไม่เกิน 20 ตัวอักษร'),
    nameTh: z.string().trim().min(1, 'โปรดระบุชื่อสาขาโรงพยาบาล (ภาษาไทย)'),
    nameEn: optionalText,
    shortName: optionalText,
    branchNo: optionalText,
    doctorCodePrefix: optionalText,
    glPostCode: optionalText,
    setOfBooks: optionalText,
    ekgTreatmentPrefix: optionalText,
    taxId: optionalText.refine((v) => !v || /^\d{13}$/.test(v), 'เลขประจำตัวผู้เสียภาษีต้องเป็นตัวเลข 13 หลัก'),
    taxAddrNo: optionalText,
    taxAddrBuilding: optionalText,
    taxAddrSoi: optionalText,
    taxAddrRoad: optionalText,
    taxAddrSubdistrict: optionalText,
    taxAddrDistrict: optionalText,
    taxAddrProvince: optionalText,
    taxAddrPostcode: optionalText.refine((v) => !v || /^\d{5}$/.test(v), 'รหัสไปรษณีย์ต้องเป็นตัวเลข 5 หลัก'),
    taxAddrCountry: optionalText,
    contactEmail: optionalText.refine((v) => !v || v.includes('@'), 'รูปแบบอีเมลไม่ถูกต้อง'),
    contactPhone: optionalText,
    isHeadOffice: z.boolean(),
    status: z.enum(['ACTIVE', 'INACTIVE']),
    remark: optionalText,
});
export const hospitalScreen: ScreenDescriptor<HospitalListItem, HospitalDetail, HospitalInput> = {
    id: 'hospital',
    resource: 'hospitals',
    path: '/master-data/general/hospital',
    titleTh: 'สาขาโรงพยาบาล',
    breadcrumb: [{ label: 'ข้อมูลหลัก' }, { label: 'ข้อมูลหลักทั่วไป' }],
    api: createCrudApi<HospitalListItem, HospitalDetail, HospitalInput>('hospitals'),
    columns: [
        { key: 'id', header: 'รหัสโรงพยาบาล', sortable: true, width: '160px' },
        { key: 'nameTh', header: 'ชื่อสาขาโรงพยาบาล (ภาษาไทย)', sortable: true },
        { key: 'nameEn', header: 'ชื่อสาขาโรงพยาบาล (ภาษาอังกฤษ)', sortable: true },
        { key: 'branchNo', header: 'เลขสาขา', sortable: true, width: '120px' },
        { key: 'doctorCodePrefix', header: 'คำนำหน้ารหัสแพทย์', width: '160px' },
        STATUS_COLUMN,
    ],
    filters: [{ kind: 'status', name: 'status', label: 'สถานะ' }],
    searchHint: 'ค้นหารหัสหรือชื่อสาขาโรงพยาบาล',
    defaultSort: 'id',
    rowKey: (row) => row.id,
    emptyHint: 'ทะเบียนโรงพยาบาลในเครือ เป็นตัวกำหนดขอบเขตข้อมูลของผู้ใช้ทุกคน',
    sections: [
        {
            title: 'ข้อมูลสาขา',
            fields: [
                {
                    kind: 'text',
                    name: 'id',
                    label: 'รหัสโรงพยาบาล',
                    required: true,
                    maxLength: 20,
                    immutableOnEdit: true,
                    hint: 'ใช้อ้างอิงในทุกตารางของสาขานี้ จึงแก้ภายหลังไม่ได้',
                },
                { kind: 'text', name: 'nameTh', label: 'ชื่อสาขาโรงพยาบาล (ภาษาไทย)', required: true },
                { kind: 'text', name: 'nameEn', label: 'ชื่อสาขาโรงพยาบาล (ภาษาอังกฤษ)' },
                { kind: 'text', name: 'shortName', label: 'ชื่อย่อ', width: 'sm' },
                { kind: 'text', name: 'branchNo', label: 'เลขสาขาโรงพยาบาล', width: 'sm' },
                {
                    kind: 'bool',
                    name: 'isHeadOffice',
                    label: 'สำนักงานใหญ่',
                    trueLabel: 'ใช่',
                    falseLabel: 'ไม่ใช่',
                },
                { kind: 'text', name: 'contactPhone', label: 'เบอร์ติดต่อ', width: 'sm' },
                { kind: 'text', name: 'contactEmail', label: 'อีเมลสำหรับติดต่อ' },
                STATUS_FIELD,
            ],
        },
        {
            title: 'การเชื่อมต่อระบบบัญชีและ HIS',
            description: 'ค่าที่ Oracle และ HIS ใช้อ้างถึงสาขานี้',
            fields: [
                {
                    kind: 'text',
                    name: 'doctorCodePrefix',
                    label: 'คำนำหน้ารหัสแพทย์',
                    width: 'sm',
                    hint: 'ต้องไม่ซ้ำกับสาขาอื่นในเครือ',
                },
                { kind: 'text', name: 'glPostCode', label: 'GL POST CODE', width: 'sm' },
                { kind: 'text', name: 'setOfBooks', label: 'Set Of Books', width: 'sm' },
                {
                    kind: 'text',
                    name: 'ekgTreatmentPrefix',
                    label: 'รหัสขึ้นต้น Treatment EKG',
                    width: 'sm',
                },
            ],
        },
        {
            title: 'ที่อยู่สำหรับยื่นภาษี',
            description: 'ที่อยู่ที่พิมพ์ลงหนังสือรับรองการหักภาษี ณ ที่จ่ายของแพทย์ในสาขานี้',
            fields: [
                { kind: 'text', name: 'taxId', label: 'เลขประจำตัวผู้เสียภาษี', width: 'sm' },
                { kind: 'text', name: 'taxAddrNo', label: 'เลขที่', width: 'sm' },
                { kind: 'text', name: 'taxAddrBuilding', label: 'อาคาร/หมู่บ้าน' },
                { kind: 'text', name: 'taxAddrSoi', label: 'ซอย/ตรอก', width: 'sm' },
                { kind: 'text', name: 'taxAddrRoad', label: 'ถนน', width: 'sm' },
                {
                    kind: 'text',
                    name: 'taxAddrSubdistrict',
                    label: 'ตำบล/แขวง',
                    width: 'sm',
                    autoFilled: true,
                },
                {
                    kind: 'text',
                    name: 'taxAddrDistrict',
                    label: 'อำเภอ/เขต',
                    width: 'sm',
                    autoFilled: true,
                },
                { kind: 'text', name: 'taxAddrProvince', label: 'จังหวัด', width: 'sm', autoFilled: true },
                {
                    kind: 'postcode',
                    name: 'taxAddrPostcode',
                    label: 'รหัสไปรษณีย์',
                    hint: 'เลือกแล้วระบบเติมตำบล อำเภอ จังหวัด และประเทศให้',
                    fill: {
                        subdistrict: 'taxAddrSubdistrict',
                        district: 'taxAddrDistrict',
                        province: 'taxAddrProvince',
                        country: 'taxAddrCountry',
                    },
                },
                { kind: 'text', name: 'taxAddrCountry', label: 'ประเทศ', width: 'sm', autoFilled: true },
                REMARK_FIELD,
            ],
        },
    ],
    schema: hospitalSchema,
    defaultValues: {
        id: '',
        nameTh: '',
        nameEn: null,
        shortName: null,
        branchNo: null,
        doctorCodePrefix: null,
        glPostCode: null,
        setOfBooks: null,
        ekgTreatmentPrefix: null,
        taxId: null,
        taxAddrNo: null,
        taxAddrBuilding: null,
        taxAddrSoi: null,
        taxAddrRoad: null,
        taxAddrSubdistrict: null,
        taxAddrDistrict: null,
        taxAddrProvince: null,
        taxAddrPostcode: null,
        taxAddrCountry: 'ไทย',
        contactEmail: null,
        contactPhone: null,
        isHeadOffice: false,
        status: 'ACTIVE',
        remark: null,
    },
    toInput: (detail) => omitFields(detail, 'rowVersion'),
};
