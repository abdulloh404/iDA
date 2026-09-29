import { guaranteeScreen } from './guarantee';
export const lumpSumRateScreen = guaranteeScreen({
    id: 'guarantee-lump-sum',
    kind: 'LUMP_SUM',
    path: '/duty-rates/lump-sum',
    titleTh: 'อัตราค่าแพทย์เหมาจ่าย',
    emptyHint: 'ค่าเวรที่ตกลงเป็นจำนวนตายตัวรายวัน ไม่ได้เทียบกับรายได้จริง',
    incomeInDays: true,
    dayVariant: 'income',
});
export const surplusRateScreen = guaranteeScreen({
    id: 'guarantee-surplus',
    kind: 'SURPLUS',
    path: '/duty-rates/surplus',
    titleTh: 'อัตราค่าแพทย์ Surplus',
    emptyHint: 'ส่วนที่จ่ายเพิ่มเมื่อรายได้จริงเกินเรทเริ่มต้นที่ตกลงไว้',
    incomeLabel: 'รายได้ (บาท/ชั่วโมง)',
    withSurplusRate: true,
    withCompareBlock: true,
    withTreatments: true,
});
export const hourlyGuaranteeScreen = guaranteeScreen({
    id: 'guarantee-hourly',
    kind: 'HOURLY',
    path: '/duty-rates/hourly',
    titleTh: 'อัตราประกันรายได้ รายชั่วโมง',
    emptyHint: 'ยอดขั้นต่ำต่อชั่วโมงที่โรงพยาบาลรับประกันให้แพทย์',
    incomeLabel: 'รายได้ (บาท/ชั่วโมง)',
    withWholeMonth406: true,
    withCompareBlock: true,
    withTreatments: true,
    dayVariant: 'schedule',
});
export const perSessionGuaranteeScreen = guaranteeScreen({
    id: 'guarantee-per-session',
    kind: 'PER_SESSION',
    path: '/duty-rates/per-session',
    titleTh: 'อัตราประกันรายได้ รายคาบ',
    emptyHint: 'ยอดขั้นต่ำต่อคาบออกตรวจ ไม่ได้คิดตามจำนวนชั่วโมง',
    incomeLabel: 'รายได้ (บาท/คาบ)',
    withCompareBlock: true,
    withTreatments: true,
    dayVariant: 'schedule',
});
export const monthlyGuaranteeScreen = guaranteeScreen({
    id: 'guarantee-monthly',
    kind: 'MONTHLY',
    path: '/duty-rates/monthly',
    titleTh: 'อัตราประกันรายได้ รายเดือน',
    emptyHint: 'ยอดขั้นต่ำต่อเดือน — แบบสะสมจะยกยอดขาด/เกินไปเดือนถัดไป',
    incomeLabel: 'รายได้ (บาท/เดือน)',
    withCalcMode: true,
    withCompareBlock: true,
    withTreatments: true,
    dayVariant: 'exclusion',
});
