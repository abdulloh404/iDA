import type { SelectOption } from '../../components/form/Select';
export const MONTHS_TH = [
    'มกราคม',
    'กุมภาพันธ์',
    'มีนาคม',
    'เมษายน',
    'พฤษภาคม',
    'มิถุนายน',
    'กรกฎาคม',
    'สิงหาคม',
    'กันยายน',
    'ตุลาคม',
    'พฤศจิกายน',
    'ธันวาคม',
];
export const monthLabel = (month: number) => MONTHS_TH[month - 1] ?? String(month);
export const periodLabel = (year: number, month: number) => `${monthLabel(month)} ${year}`;
export function selectableYears(today = new Date()): number[] {
    const year = today.getFullYear();
    return [year - 1, year, year + 1];
}
export const MONTH_OPTIONS: readonly SelectOption[] = MONTHS_TH.map((label, index) => ({
    value: String(index + 1),
    label,
}));
export const yearOptions = (today = new Date()): readonly SelectOption[] => selectableYears(today).map((y) => ({ value: String(y), label: String(y) }));
