import { useEffect, useRef, useState } from 'react';
import type { RefObject } from 'react';
export function useOverflowTitle<T extends HTMLElement>(text: string): {
    ref: RefObject<T | null>;
    title: string | undefined;
} {
    const ref = useRef<T>(null);
    const [truncated, setTruncated] = useState(false);
    useEffect(() => {
        const element = ref.current;
        if (!element)
            return;
        const measure = () => setTruncated(element.scrollWidth > element.clientWidth + 1);
        measure();
        if (typeof ResizeObserver === 'undefined')
            return;
        const observer = new ResizeObserver(measure);
        observer.observe(element);
        return () => observer.disconnect();
    }, [text]);
    return { ref, title: truncated ? text : undefined };
}
