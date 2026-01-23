# Custom Hooks Reference

Detailed rules and conventions for creating custom React hooks.

## Naming Convention

- Always prefix with `use`
- Use camelCase: `useDebounce`, `useLocalStorage`, `usePagination`

## Hook Structure Template

```typescript
// hooks/useMyHook.ts
import { useState, useEffect, useCallback } from 'react';

/**
 * Description of what the hook does
 * @param param1 - Description of parameter
 * @returns Description of return value
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

## Guidelines

### Do's
- Name with `use` prefix
- Make reusable and generic
- Document with JSDoc
- Handle cleanup in useEffect
- Use TypeScript
- Single responsibility
- Return objects for 3+ values, tuples for 2

### Don'ts
- Never include JSX/rendering
- Don't call conditionally
- Don't call in loops
- Don't call in regular functions
- Avoid component coupling

## Return Value Patterns

### For 2 values - use tuple
```typescript
return [value, setValue] as const;
```

### For 3+ values - use object
```typescript
return { state, loading, error, refetch };
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

## Testing Hooks

```typescript
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
