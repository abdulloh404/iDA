export const chartSeries = {
    light: [
        '#00509E',
        '#EB6834',
        '#00A3E0',
        '#EDA100',
        '#E87BA4',
        '#299A50',
        '#4A3AA7',
        '#E34948',
    ],
    dark: [
        '#1589F9',
        '#D95926',
        '#0098D4',
        '#C98500',
        '#D55181',
        '#008300',
        '#9085E9',
        '#E66767',
    ],
} as const;
export const chartSeriesScatterSafe = {
    light: [chartSeries.light[0], chartSeries.light[1], chartSeries.light[2]],
    dark: [chartSeries.dark[0], chartSeries.dark[3], chartSeries.dark[5]],
} as const;
export const chartSequentialBlue = [
    '#D3E4F7',
    '#A8C8EE',
    '#7BAAE3',
    '#4B87D4',
    '#1C64B8',
    '#00509E',
    '#003A73',
    '#002952',
] as const;
export const chartOrdinalStart = 2;
export const chartDiverging = [
    '#B42318',
    '#D64545',
    '#EC8B85',
    '#EEF1F4',
    '#A8C8EE',
    '#4B87D4',
    '#00509E',
] as const;
export const chartLowContrastLight: readonly string[] = ['#00A3E0', '#EDA100', '#E87BA4'];
export type IdaTheme = 'light' | 'dark';
export function currentTheme(): IdaTheme {
    return document.documentElement.dataset.theme === 'dark' ? 'dark' : 'light';
}
export function readToken(name: string): string {
    return getComputedStyle(document.documentElement).getPropertyValue(name).trim();
}
const thb = new Intl.NumberFormat('th-TH', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
});
export function formatTHB(value: number): string {
    return value < 0 ? `(${thb.format(Math.abs(value))})` : thb.format(value);
}
export function amountClass(value: number): string {
    if (value > 0)
        return 'ida-numeric ida-numeric--positive';
    if (value < 0)
        return 'ida-numeric ida-numeric--negative';
    return 'ida-numeric';
}
