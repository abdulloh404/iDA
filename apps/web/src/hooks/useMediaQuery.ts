import { useCallback, useSyncExternalStore } from 'react';
export function useMediaQuery(query: string): boolean {
    const subscribe = useCallback((onChange: () => void) => {
        if (!supported())
            return () => { };
        const list = window.matchMedia(query);
        list.addEventListener('change', onChange);
        return () => list.removeEventListener('change', onChange);
    }, [query]);
    const read = useCallback(() => supported() && window.matchMedia(query).matches, [query]);
    return useSyncExternalStore(subscribe, read, () => false);
}
function supported(): boolean {
    return typeof window !== 'undefined' && typeof window.matchMedia === 'function';
}
