export interface PostcodeEntry {
    postcode: string;
    subdistrict: string;
    district: string;
    province: string;
}
interface PostcodeFile {
    p: string[];
    d: [
        string,
        number
    ][];
    r: [
        string,
        string,
        number
    ][];
}
let loading: Promise<PostcodeEntry[]> | null = null;
export function loadPostcodes(): Promise<PostcodeEntry[]> {
    loading ??= import('./thai-postcodes.json')
        .then((m) => expand(m.default as PostcodeFile))
        .catch((error: unknown) => {
        loading = null;
        throw error;
    });
    return loading;
}
function expand(file: PostcodeFile): PostcodeEntry[] {
    return file.r.map(([postcode, subdistrict, districtIndex]) => {
        const [district, provinceIndex] = file.d[districtIndex];
        return { postcode, subdistrict, district, province: file.p[provinceIndex] };
    });
}
export function searchPostcodes(entries: readonly PostcodeEntry[], query: string, limit = 50): PostcodeEntry[] {
    const text = query.trim();
    if (text === '')
        return [];
    const byCode = /^\d+$/.test(text);
    const results: PostcodeEntry[] = [];
    for (const entry of entries) {
        const hit = byCode
            ? entry.postcode.startsWith(text)
            : entry.subdistrict.includes(text) ||
                entry.district.includes(text) ||
                entry.province.includes(text);
        if (hit) {
            results.push(entry);
            if (results.length === limit)
                break;
        }
    }
    return results;
}
