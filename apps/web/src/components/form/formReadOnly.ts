import { createContext, useContext } from 'react';
export const FormReadOnlyContext = createContext(false);
export function useFormReadOnly(): boolean {
    return useContext(FormReadOnlyContext);
}
