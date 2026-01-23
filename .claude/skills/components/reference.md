# Component Reference

Detailed rules and conventions for React component development.

## Tech Stack

- **Framework**: Next.js with App Router
- **UI Library**: React
- **Language**: TypeScript (strict mode)
- **Styling**: Tailwind CSS + DaisyUI
- **Icons**: Lucide React
- **i18n**: next-intl

## Component Structure Order

Every component MUST follow this order:

1. "use client" directive (if client component)
2. Imports
3. Props Interface
4. Initial state constants (outside component)
5. Component function with:
   - Translations
   - State declarations
   - Effects
   - Memoized values
   - Event handlers
   - Early returns (loading, error, closed)
   - JSX return

## State Management Patterns

### Form State
```typescript
const initialFormData: CreateEntityType = {
  field1: "",
  field2: 0,
  optionalField: undefined,
};

const [formData, setFormData] = useState<CreateEntityType>(initialFormData);
```

### Error State
```typescript
const [errors, setErrors] = useState<Record<string, string>>({});
```

### Loading State
```typescript
const [isSubmitting, setIsSubmitting] = useState(false);
const [isLoading, setIsLoading] = useState(false);
```

### Union Type State
```typescript
const [mode, setMode] = useState<"view" | "edit" | "create">("view");
```

## Performance Patterns

### useCallback for Event Handlers
Always wrap event handlers with `useCallback` when:
- Passed to child components
- Used in dependency arrays

### useMemo for Computed Values
Use `useMemo` for:
- Filtered/sorted data
- Computed validation state
- Grouped data
- Any expensive computation

## Error Handling Pattern

```typescript
try {
  setIsSubmitting(true);
  setError(null);
  await apiFunction(data);
  onSuccess();
  onClose();
} catch (err) {
  const errorMessage = err instanceof Error ? err.message : t("errors.default");
  setError(translateBackendError(errorMessage, tErrors));
  console.error("Failed to submit form:", err);
} finally {
  setIsSubmitting(false);
}
```

## Console Logging Guidelines

**DO NOT use console.log for:**
- Regular flow logging
- Debug output in production code
- User-facing information

**ONLY use console.error for:**
- Unexpected errors in catch blocks
- Failed API calls that need debugging

## Styling Classes Reference

### Button Classes
| Type | Classes |
|------|---------|
| Primary | `btn btn-primary` |
| Secondary | `btn btn-ghost` |
| Success | `btn btn-success` |
| Error/Danger | `btn btn-error` |
| Small | `btn btn-sm` |
| With Icon | `btn btn-primary gap-2` |

### Badge Classes
| Type | Classes |
|------|---------|
| Success | `badge badge-success` |
| Error | `badge badge-error` |
| Warning | `badge badge-warning` |
| Info | `badge badge-info` |

### Common Layout Classes
| Purpose | Classes |
|---------|---------|
| Spacing | `space-y-4`, `gap-2`, `gap-4` |
| Flex | `flex items-center justify-between` |
| Grid | `grid grid-cols-2 gap-4` |
| Text | `text-gray-500`, `font-semibold` |
