# Frontend Custom Hooks

## Overview
Custom React hooks for reusable logic extraction. Always prefix with `use`.

## Required: useDebounce

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
  fetchResults(debouncedSearch); // Only after 500ms of no typing
}, [debouncedSearch]);
```

## Common Custom Hooks

### useLocalStorage

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

### useMediaQuery

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

### usePagination

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

### useClickOutside

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

### useToggle

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

### useAsync

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

### useKeyPress

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

## Hook Guidelines

### Do's ✅
- Name with `use` prefix
- Make reusable and generic
- Document with JSDoc
- Handle cleanup in useEffect
- Use TypeScript
- Single responsibility
- Return objects for 3+ values, tuples for 2

### Don'ts ❌
- Never include JSX/rendering
- Don't call conditionally
- Don't call in loops
- Don't call in regular functions
- Avoid component coupling

## Hook Structure Template

```typescript
// hooks/useMyHook.ts
import { useState, useEffect, useCallback } from 'react';

/**
 * Description
 * @param param1 - Description
 * @returns Description
 */
export function useMyHook(param1: Type) {
  const [state, setState] = useState(initialValue);

  useEffect(() => {
    // Effect logic
    return () => {
      // Cleanup
    };
  }, [/* dependencies */]);

  const callback = useCallback(() => {
    // Logic
  }, [/* dependencies */]);

  return { state, callback };
}
```

## Common Use Cases

1. Form handling logic
2. Data fetching (prefer React Query)
3. Event listeners (resize, scroll, keyboard)
4. Local storage sync
5. Window/document interactions
6. Authentication state
7. Pagination logic
8. Debouncing/throttling
9. Modal/dropdown state
10. Animation states

## Testing

```typescript
// hooks/__tests__/useDebounce.test.ts
import { renderHook, waitFor } from '@testing-library/react';
import { useDebounce } from '../useDebounce';

describe('useDebounce', () => {
  it('should debounce value changes', async () => {
    const { result, rerender } = renderHook(
      ({ value, delay }) => useDebounce(value, delay),
      { initialProps: { value: 'initial', delay: 500 } }
    );

    expect(result.current).toBe('initial');
    rerender({ value: 'updated', delay: 500 });
    expect(result.current).toBe('initial');

    await waitFor(() => expect(result.current).toBe('updated'), { timeout: 600 });
  });
});
```

## Checklist
- [ ] Hook name starts with `use`
- [ ] Reusable and generic
- [ ] No JSX or rendering
- [ ] Proper TypeScript typing
- [ ] JSDoc comments
- [ ] Cleanup functions
- [ ] Correct dependencies
- [ ] Appropriate return structure
- [ ] Single responsibility
- [ ] Tested
