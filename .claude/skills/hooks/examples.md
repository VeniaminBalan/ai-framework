# Custom Hooks Examples

## useDebounce

```typescript
// hooks/useDebounce.ts
import { useState, useEffect } from 'react';

/**
 * Debounces a value by delay
 * @param value - Value to debounce
 * @param delay - Delay in ms
 */
export function useDebounce<T>(value: T, delay: number): T {
  const [debouncedValue, setDebouncedValue] = useState<T>(value);

  useEffect(() => {
    const handler = setTimeout(() => setDebouncedValue(value), delay);
    return () => clearTimeout(handler);
  }, [value, delay]);

  return debouncedValue;
}

// Usage
const [searchTerm, setSearchTerm] = useState('');
const debouncedSearch = useDebounce(searchTerm, 500);

useEffect(() => {
  fetchResults(debouncedSearch);
}, [debouncedSearch]);
```

## useLocalStorage

```typescript
// hooks/useLocalStorage.ts
import { useState } from 'react';

export function useLocalStorage<T>(key: string, initialValue: T) {
  const [storedValue, setStoredValue] = useState<T>(() => {
    try {
      const item = window.localStorage.getItem(key);
      return item ? JSON.parse(item) : initialValue;
    } catch {
      return initialValue;
    }
  });

  const setValue = (value: T | ((val: T) => T)) => {
    try {
      const valueToStore = value instanceof Function ? value(storedValue) : value;
      setStoredValue(valueToStore);
      window.localStorage.setItem(key, JSON.stringify(valueToStore));
    } catch (error) {
      console.error(`Error saving ${key}:`, error);
    }
  };

  return [storedValue, setValue] as const;
}
```

## useMediaQuery

```typescript
// hooks/useMediaQuery.ts
import { useState, useEffect } from 'react';

export function useMediaQuery(query: string): boolean {
  const [matches, setMatches] = useState(() => window.matchMedia(query).matches);

  useEffect(() => {
    const mediaQuery = window.matchMedia(query);
    const handler = (e: MediaQueryListEvent) => setMatches(e.matches);
    mediaQuery.addEventListener('change', handler);
    return () => mediaQuery.removeEventListener('change', handler);
  }, [query]);

  return matches;
}

// Usage
const isMobile = useMediaQuery('(max-width: 768px)');
```

## usePagination

```typescript
// hooks/usePagination.ts
import { useState, useMemo } from 'react';

interface UsePaginationProps {
  totalItems: number;
  itemsPerPage: number;
  initialPage?: number;
}

export function usePagination({ totalItems, itemsPerPage, initialPage = 1 }: UsePaginationProps) {
  const [currentPage, setCurrentPage] = useState(initialPage);
  const totalPages = Math.ceil(totalItems / itemsPerPage);

  const goToPage = (page: number) => {
    setCurrentPage(Math.max(1, Math.min(page, totalPages)));
  };

  const paginationRange = useMemo(() => {
    if (totalPages <= 7) return Array.from({ length: totalPages }, (_, i) => i + 1);

    const range: (number | string)[] = [];
    if (currentPage <= 3) {
      for (let i = 1; i <= 5; i++) range.push(i);
      range.push('...', totalPages);
    } else if (currentPage >= totalPages - 2) {
      range.push(1, '...');
      for (let i = totalPages - 4; i <= totalPages; i++) range.push(i);
    } else {
      range.push(1, '...', currentPage - 1, currentPage, currentPage + 1, '...', totalPages);
    }
    return range;
  }, [currentPage, totalPages]);

  return {
    currentPage,
    totalPages,
    goToPage,
    nextPage: () => goToPage(currentPage + 1),
    prevPage: () => goToPage(currentPage - 1),
    canGoNext: currentPage < totalPages,
    canGoPrev: currentPage > 1,
    paginationRange,
  };
}
```

## useClickOutside

```typescript
// hooks/useClickOutside.ts
import { useEffect, RefObject } from 'react';

export function useClickOutside<T extends HTMLElement = HTMLElement>(
  ref: RefObject<T>,
  handler: (event: MouseEvent | TouchEvent) => void
) {
  useEffect(() => {
    const listener = (event: MouseEvent | TouchEvent) => {
      const el = ref?.current;
      if (!el || el.contains(event.target as Node)) return;
      handler(event);
    };

    document.addEventListener('mousedown', listener);
    document.addEventListener('touchstart', listener);
    return () => {
      document.removeEventListener('mousedown', listener);
      document.removeEventListener('touchstart', listener);
    };
  }, [ref, handler]);
}

// Usage
const dropdownRef = useRef<HTMLDivElement>(null);
useClickOutside(dropdownRef, () => setIsOpen(false));
```

## useToggle

```typescript
// hooks/useToggle.ts
import { useState, useCallback } from 'react';

export function useToggle(initialValue = false): [boolean, () => void, (value: boolean) => void] {
  const [value, setValue] = useState(initialValue);
  const toggle = useCallback(() => setValue(v => !v), []);
  return [value, toggle, setValue];
}

// Usage
const [isOpen, toggleOpen, setIsOpen] = useToggle();
```

## useAsync

```typescript
// hooks/useAsync.ts
import { useState, useEffect, useCallback } from 'react';

interface AsyncState<T> {
  data: T | null;
  error: Error | null;
  loading: boolean;
}

export function useAsync<T>(
  asyncFunction: () => Promise<T>,
  dependencies: unknown[] = []
) {
  const [state, setState] = useState<AsyncState<T>>({
    data: null,
    error: null,
    loading: true,
  });

  const execute = useCallback(async () => {
    setState({ data: null, error: null, loading: true });
    try {
      const data = await asyncFunction();
      setState({ data, error: null, loading: false });
    } catch (error) {
      setState({ data: null, error: error as Error, loading: false });
    }
  }, dependencies);

  useEffect(() => { execute(); }, [execute]);

  return { ...state, refetch: execute };
}
```

## useKeyPress

```typescript
// hooks/useKeyPress.ts
import { useState, useEffect } from 'react';

export function useKeyPress(targetKey: string): boolean {
  const [keyPressed, setKeyPressed] = useState(false);

  useEffect(() => {
    const downHandler = ({ key }: KeyboardEvent) => key === targetKey && setKeyPressed(true);
    const upHandler = ({ key }: KeyboardEvent) => key === targetKey && setKeyPressed(false);

    window.addEventListener('keydown', downHandler);
    window.addEventListener('keyup', upHandler);
    return () => {
      window.removeEventListener('keydown', downHandler);
      window.removeEventListener('keyup', upHandler);
    };
  }, [targetKey]);

  return keyPressed;
}

// Usage
const escapePressed = useKeyPress('Escape');
useEffect(() => { if (escapePressed) closeModal(); }, [escapePressed]);
```

## useFormState

```typescript
// hooks/useFormState.ts
import { useState, useCallback } from "react";

interface UseFormStateOptions<T> {
  initialData: T;
  onSubmit: (data: T) => Promise<void>;
  validate?: (data: T) => Record<string, string>;
}

export function useFormState<T extends Record<string, unknown>>({
  initialData,
  onSubmit,
  validate,
}: UseFormStateOptions<T>) {
  const [formData, setFormData] = useState<T>(initialData);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleChange = useCallback(<K extends keyof T>(field: K, value: T[K]) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    setErrors((prev) => {
      if (prev[field as string]) {
        const newErrors = { ...prev };
        delete newErrors[field as string];
        return newErrors;
      }
      return prev;
    });
  }, []);

  const handleSubmit = useCallback(async (e?: React.FormEvent) => {
    e?.preventDefault();

    if (validate) {
      const validationErrors = validate(formData);
      if (Object.keys(validationErrors).length > 0) {
        setErrors(validationErrors);
        return;
      }
    }

    try {
      setIsSubmitting(true);
      setError(null);
      await onSubmit(formData);
    } catch (err) {
      setError(err instanceof Error ? err.message : "An error occurred");
    } finally {
      setIsSubmitting(false);
    }
  }, [formData, validate, onSubmit]);

  const reset = useCallback((newData?: T) => {
    setFormData(newData ?? initialData);
    setErrors({});
    setError(null);
  }, [initialData]);

  return {
    formData,
    errors,
    error,
    isSubmitting,
    handleChange,
    handleSubmit,
    reset,
    setFormData,
    setErrors,
    setError,
  };
}
```

## useModal

```typescript
// hooks/useModal.ts
import { useState, useCallback } from "react";

export function useModal<T = undefined>() {
  const [isOpen, setIsOpen] = useState(false);
  const [data, setData] = useState<T | undefined>(undefined);

  const open = useCallback((modalData?: T) => {
    setData(modalData);
    setIsOpen(true);
  }, []);

  const close = useCallback(() => {
    setIsOpen(false);
    setData(undefined);
  }, []);

  return { isOpen, data, open, close };
}

// Usage
const createModal = useModal<void>();
const editModal = useModal<EntityType>();

<button onClick={() => createModal.open()}>Create</button>
<button onClick={() => editModal.open(item)}>Edit</button>

<CreateModal isOpen={createModal.isOpen} onClose={createModal.close} />
<EditModal isOpen={editModal.isOpen} onClose={editModal.close} data={editModal.data} />
```
