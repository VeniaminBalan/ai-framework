# Frontend Component Development Skill

## Overview
This skill provides patterns and guidelines for developing React/Next.js components in the TimeManagement frontend application.

## Tech Stack
- **Framework**: Next.js 15.1.4 with App Router
- **UI Library**: React 19
- **Language**: TypeScript 5 (strict mode)
- **Styling**: Tailwind CSS 3.4.1 + DaisyUI 4.12.14
- **Icons**: Lucide React
- **i18n**: next-intl

## Component Structure Template

Every component MUST follow this structure:

```typescript
"use client";

import { useState, useEffect, useCallback, useMemo } from "react";
import { useTranslations } from "next-intl";
import { SomeType } from "@/types/feature-name";
import { apiFunction } from "@/lib/api/feature-name";

// Props Interface - always define explicitly
interface ComponentNameProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  data?: DataType;
  userId: number;
}

// Initial state constants (define outside component to avoid recreation)
const initialFormData: FormDataType = {
  field1: "",
  field2: 0,
};

// Component with default export
export default function ComponentName({
  isOpen,
  onClose,
  onSuccess,
  data,
  userId,
}: ComponentNameProps) {
  // 1. Translations
  const t = useTranslations("feature.section");
  const tCommon = useTranslations("common");
  const tErrors = useTranslations("errors");

  // 2. State declarations
  const [formData, setFormData] = useState<FormDataType>(initialFormData);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // 3. Effects - Reset form when modal opens/closes
  useEffect(() => {
    if (isOpen) {
      setFormData(data ?? initialFormData);
      setErrors({});
      setError(null);
    }
  }, [isOpen, data]);

  // 4. Memoized values (for derived/computed data)
  const isFormValid = useMemo(() => {
    return formData.field1.trim().length > 0;
  }, [formData.field1]);

  // 5. Event handlers - use useCallback for handlers passed to children
  const handleChange = useCallback((field: keyof FormDataType, value: string | number) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    setErrors((prev) => {
      if (prev[field]) {
        const newErrors = { ...prev };
        delete newErrors[field];
        return newErrors;
      }
      return prev;
    });
  }, []);

  const handleSubmit = useCallback(async (e?: React.FormEvent) => {
    e?.preventDefault();

    // Validation
    const validationErrors: Record<string, string> = {};
    if (!formData.field1.trim()) {
      validationErrors.field1 = t("validation.field1Required");
    }
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    try {
      setIsSubmitting(true);
      setError(null);
      await apiFunction(formData);
      onSuccess();
      onClose();
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : t("errors.default");
      setError(errorMessage);
    } finally {
      setIsSubmitting(false);
    }
  }, [formData, t, onSuccess, onClose]);

  // 6. Early return for closed state (modals)
  if (!isOpen) return null;

  // 7. JSX return
  return (
    // Component JSX
  );
}
```

## State Management Patterns

### Form State
```typescript
// Define initial state outside component
const initialFormData: CreateEntityType = {
  field1: "",
  field2: 0,
  optionalField: undefined,
};

// Inside component
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
const [workflowType, setWorkflowType] = useState<"standard" | "direct">("standard");
```

## Performance Patterns

### useCallback for Event Handlers
Always wrap event handlers with `useCallback` when they are passed to child components or used in dependency arrays:

```typescript
// Handlers passed to children - use useCallback
const handleChange = useCallback((field: keyof FormDataType, value: string | number) => {
  setFormData((prev) => ({ ...prev, [field]: value }));
}, []);

const handleDelete = useCallback((id: number) => {
  // delete logic
}, []);

// Handlers used only in the same component - useCallback optional but recommended
const handleSubmit = useCallback(async () => {
  // submit logic
}, [formData, onSuccess, onClose]);
```

### useMemo for Computed Values
Use `useMemo` for expensive computations or derived data:

```typescript
// Filtered/sorted data
const sortedItems = useMemo(() =>
  [...items].sort((a, b) => a.name.localeCompare(b.name)),
[items]);

// Computed validation state
const isFormValid = useMemo(() => {
  return formData.name.trim().length > 0 && formData.email.includes("@");
}, [formData.name, formData.email]);

// Grouped data
const groupedByCategory = useMemo(() =>
  items.reduce((acc, item) => {
    const key = item.category;
    acc[key] = acc[key] || [];
    acc[key].push(item);
    return acc;
  }, {} as Record<string, Item[]>),
[items]);
```

## Modal Component Pattern

```typescript
"use client";

import { useState, useEffect, useCallback } from "react";
import { useTranslations } from "next-intl";

interface ExampleModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  initialData?: FormDataType;
}

const initialFormData: FormDataType = {
  field1: "",
  field2: 0,
};

export default function ExampleModal({
  isOpen,
  onClose,
  onSuccess,
  initialData,
}: ExampleModalProps) {
  const t = useTranslations("feature.modal");
  const tCommon = useTranslations("common");

  const [formData, setFormData] = useState<FormDataType>(initialFormData);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Reset form when modal opens
  useEffect(() => {
    if (isOpen) {
      setFormData(initialData ?? initialFormData);
      setErrors({});
      setError(null);
    }
  }, [isOpen, initialData]);

  // Handle Escape key to close modal
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === "Escape" && isOpen && !isSubmitting) {
        onClose();
      }
    };

    document.addEventListener("keydown", handleEscape);
    return () => document.removeEventListener("keydown", handleEscape);
  }, [isOpen, isSubmitting, onClose]);

  const handleChange = useCallback((field: keyof FormDataType, value: string | number) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    setErrors((prev) => {
      if (prev[field]) {
        const newErrors = { ...prev };
        delete newErrors[field];
        return newErrors;
      }
      return prev;
    });
  }, []);

  const handleSubmit = useCallback(async (e: React.FormEvent) => {
    e.preventDefault();

    // Validation
    const validationErrors: Record<string, string> = {};
    if (!formData.field1.trim()) {
      validationErrors.field1 = t("validation.field1Required");
    }
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    try {
      setIsSubmitting(true);
      setError(null);
      await apiFunction(formData);
      onSuccess();
      onClose();
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : t("errors.default");
      setError(errorMessage);
    } finally {
      setIsSubmitting(false);
    }
  }, [formData, t, onSuccess, onClose]);

  if (!isOpen) return null;

  return (
    <div className="modal modal-open" role="dialog" aria-modal="true" aria-labelledby="modal-title">
      <div className="modal-box max-w-2xl">
        {/* Header */}
        <h3 id="modal-title" className="font-bold text-2xl mb-6">
          {t("title")}
        </h3>

        {/* Error Alert */}
        {error && (
          <div className="alert alert-error mb-4" role="alert">
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
                d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <span>{error}</span>
          </div>
        )}

        {/* Form - use form element for native validation support */}
        <form onSubmit={handleSubmit}>
          <div className="space-y-4">
            {/* Form fields */}
          </div>

          {/* Modal Actions */}
          <div className="modal-action">
            <button
              type="button"
              onClick={onClose}
              className="btn btn-ghost"
              disabled={isSubmitting}
            >
              {tCommon("cancel")}
            </button>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={isSubmitting}
            >
              {isSubmitting ? (
                <>
                  <span className="loading loading-spinner loading-sm"></span>
                  {t("submitting")}
                </>
              ) : (
                tCommon("submit")
              )}
            </button>
          </div>
        </form>
      </div>
      <div className="modal-backdrop" onClick={isSubmitting ? undefined : onClose}></div>
    </div>
  );
}
```

## Table Component Pattern

```typescript
"use client";

import { useMemo } from "react";
import { useTranslations } from "next-intl";

interface DataTableProps {
  data: DataItem[];
  isLoading?: boolean;
  onRowClick: (item: DataItem) => void;
  onAction: (id: number) => void;
}

export default function DataTable({
  data,
  isLoading = false,
  onRowClick,
  onAction
}: DataTableProps) {
  const t = useTranslations("feature.table");

  // Memoize sorted/filtered data if needed
  const sortedData = useMemo(() =>
    [...data].sort((a, b) => a.name.localeCompare(b.name)),
  [data]);

  return (
    <div className="overflow-x-auto">
      <table className="table table-zebra w-full">
        <thead>
          <tr>
            <th>{t("column1")}</th>
            <th>{t("column2")}</th>
            <th>{t("actions")}</th>
          </tr>
        </thead>
        <tbody>
          {/* Loading State */}
          {isLoading ? (
            Array.from({ length: 5 }).map((_, index) => (
              <tr key={`skeleton-${index}`}>
                <td><div className="skeleton h-4 w-32"></div></td>
                <td><div className="skeleton h-4 w-24"></div></td>
                <td><div className="skeleton h-8 w-20"></div></td>
              </tr>
            ))
          ) : sortedData.length === 0 ? (
            /* Empty State */
            <tr>
              <td colSpan={3} className="text-center py-12">
                <div className="flex flex-col items-center gap-2 text-gray-500">
                  <svg className="w-12 h-12" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5}
                      d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" />
                  </svg>
                  <p>{t("noData")}</p>
                </div>
              </td>
            </tr>
          ) : (
            /* Data Rows */
            sortedData.map((item) => (
              <tr
                key={item.id}
                className="hover cursor-pointer"
                onClick={() => onRowClick(item)}
              >
                <td>{item.field1}</td>
                <td>{item.field2}</td>
                <td>
                  <div className="flex gap-2">
                    <button
                      className="btn btn-primary btn-sm gap-1"
                      onClick={(e) => {
                        e.stopPropagation();
                        onAction(item.id);
                      }}
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                      </svg>
                      {t("actionLabel")}
                    </button>
                  </div>
                </td>
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  );
}
```

## Form Field Patterns

### Text Input with Error
```typescript
<div className="form-control">
  <label className="label" htmlFor="field-id">
    <span className="label-text font-semibold">
      {t("fieldLabel")} <span className="text-error">*</span>
    </span>
  </label>
  <input
    id="field-id"
    type="text"
    placeholder={t("fieldPlaceholder")}
    className={`input input-bordered ${errors.field ? "input-error" : ""}`}
    value={formData.field}
    onChange={(e) => handleChange("field", e.target.value)}
    disabled={isSubmitting}
    aria-invalid={!!errors.field}
    aria-describedby={errors.field ? "field-error" : undefined}
  />
  {errors.field && (
    <label className="label" id="field-error">
      <span className="label-text-alt text-error">{errors.field}</span>
    </label>
  )}
</div>
```

### Number Input
```typescript
<div className="form-control">
  <label className="label" htmlFor="hours-input">
    <span className="label-text font-semibold">{t("hours")}</span>
  </label>
  <input
    id="hours-input"
    type="number"
    step="0.5"
    min="0"
    placeholder={t("hoursPlaceholder")}
    className={`input input-bordered ${errors.hours ? "input-error" : ""}`}
    value={formData.hours || ""}
    onChange={(e) => handleChange("hours", parseFloat(e.target.value) || 0)}
    disabled={isSubmitting}
  />
</div>
```

### Textarea
```typescript
<div className="form-control">
  <label className="label" htmlFor="description-input">
    <span className="label-text font-semibold">{t("description")}</span>
  </label>
  <textarea
    id="description-input"
    placeholder={t("descriptionPlaceholder")}
    className={`textarea textarea-bordered h-32 ${errors.description ? "textarea-error" : ""}`}
    value={formData.description}
    onChange={(e) => handleChange("description", e.target.value)}
    disabled={isSubmitting}
  />
</div>
```

### Select Dropdown
```typescript
<div className="form-control">
  <label className="label" htmlFor="type-select">
    <span className="label-text font-semibold">{t("type")}</span>
  </label>
  <select
    id="type-select"
    className="select select-bordered"
    value={formData.type || ""}
    onChange={(e) => handleChange("type", e.target.value)}
    disabled={isSubmitting}
  >
    <option value="" disabled>{t("selectType")}</option>
    <option value="option1">{t("option1")}</option>
    <option value="option2">{t("option2")}</option>
  </select>
</div>
```

### Radio Button Group
```typescript
<div className="form-control">
  <label className="label">
    <span className="label-text font-semibold">{t("workflow")}</span>
  </label>
  <div className="space-y-2" role="radiogroup" aria-label={t("workflow")}>
    <label className="flex items-center cursor-pointer">
      <input
        type="radio"
        name="workflow"
        value="standard"
        checked={workflowType === "standard"}
        onChange={() => setWorkflowType("standard")}
        className="radio radio-primary mr-3"
        disabled={isSubmitting}
      />
      <div className="flex-1">
        <span className="font-medium">{t("workflowStandard")}</span>
        <p className="text-sm text-gray-500">{t("workflowStandardDesc")}</p>
      </div>
    </label>
  </div>
</div>
```

## Custom Hooks Pattern

Extract reusable logic into custom hooks:

### useFormState Hook
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

### useModal Hook
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

// Usage in component
const createModal = useModal<void>();
const editModal = useModal<EntityType>();

// Open modals
<button onClick={() => createModal.open()}>Create</button>
<button onClick={() => editModal.open(item)}>Edit</button>

// Render modals
<CreateModal isOpen={createModal.isOpen} onClose={createModal.close} />
<EditModal isOpen={editModal.isOpen} onClose={editModal.close} data={editModal.data} />
```

## Styling Reference

### Button Classes
| Type | Classes |
|------|---------|
| Primary | `btn btn-primary` |
| Secondary | `btn btn-ghost` |
| Success | `btn btn-success` |
| Error/Danger | `btn btn-error` |
| Outline | `btn btn-outline` |
| Small | `btn btn-sm` |
| With Icon | `btn btn-primary gap-2` |
| Loading | `<span className="loading loading-spinner loading-sm"></span>` |

### Badge Classes
| Type | Classes |
|------|---------|
| Success | `badge badge-success` |
| Error | `badge badge-error` |
| Warning | `badge badge-warning` |
| Info | `badge badge-info` |
| Ghost | `badge badge-ghost` |

### Alert Classes
```typescript
<div className="alert alert-error mb-4" role="alert">
  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
      d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
  </svg>
  <span>{errorMessage}</span>
</div>

<div className="alert alert-success mb-4" role="alert">
  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
      d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
  </svg>
  <span>{successMessage}</span>
</div>
```

### Card Classes
```typescript
<div className="card bg-base-200">
  <div className="card-body p-4">
    <h4 className="font-semibold text-gray-700">{title}</h4>
    <p>{content}</p>
  </div>
</div>

// Gradient card
<div className="card bg-gradient-to-br from-primary/5 to-primary/10">
  <div className="card-body p-4">
    {content}
  </div>
</div>
```

### Loading Skeleton Classes
```typescript
// Text skeleton
<div className="skeleton h-4 w-32"></div>

// Button skeleton
<div className="skeleton h-10 w-24"></div>

// Card skeleton
<div className="skeleton h-48 w-full"></div>

// Avatar skeleton
<div className="skeleton h-12 w-12 rounded-full"></div>
```

### Common Layout Classes
| Purpose | Classes |
|---------|---------|
| Spacing | `space-y-4`, `gap-2`, `gap-4` |
| Margin | `mb-4`, `mb-6`, `mt-2` |
| Padding | `p-4`, `px-4`, `py-12` |
| Flex | `flex items-center justify-between` |
| Grid | `grid grid-cols-2 gap-4` |
| Text | `text-gray-500`, `text-gray-700`, `font-semibold` |

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

```typescript
// Acceptable
console.error("Failed to fetch users:", error);

// NOT acceptable
console.log("Component mounted");
console.log("Form data:", formData);
```

## API Integration Pattern

Always use the centralized API client from `@/lib/api/`:

```typescript
import { entityApi } from "@/lib/api/entity-name";

// In component - with loading and error states
const loadData = useCallback(async () => {
  try {
    setIsLoading(true);
    setError(null);
    const data = await entityApi.getAll();
    setData(data);
  } catch (err) {
    setError(err instanceof Error ? err.message : "Failed to load data");
    console.error("Failed to fetch data:", err);
  } finally {
    setIsLoading(false);
  }
}, []);

// Call on mount
useEffect(() => {
  loadData();
}, [loadData]);
```

## Testing Patterns

### Component Testing Structure
```typescript
// ComponentName.test.tsx
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { NextIntlClientProvider } from "next-intl";
import ComponentName from "./ComponentName";

// Mock translations
const messages = {
  feature: {
    section: {
      title: "Test Title",
      submit: "Submit",
    },
  },
  common: {
    cancel: "Cancel",
  },
};

const renderWithProviders = (ui: React.ReactElement) => {
  return render(
    <NextIntlClientProvider locale="en" messages={messages}>
      {ui}
    </NextIntlClientProvider>
  );
};

describe("ComponentName", () => {
  const defaultProps = {
    isOpen: true,
    onClose: jest.fn(),
    onSuccess: jest.fn(),
  };

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it("renders when open", () => {
    renderWithProviders(<ComponentName {...defaultProps} />);
    expect(screen.getByRole("dialog")).toBeInTheDocument();
  });

  it("does not render when closed", () => {
    renderWithProviders(<ComponentName {...defaultProps} isOpen={false} />);
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("calls onClose when cancel is clicked", async () => {
    const user = userEvent.setup();
    renderWithProviders(<ComponentName {...defaultProps} />);

    await user.click(screen.getByText("Cancel"));
    expect(defaultProps.onClose).toHaveBeenCalled();
  });

  it("closes on Escape key press", async () => {
    const user = userEvent.setup();
    renderWithProviders(<ComponentName {...defaultProps} />);

    await user.keyboard("{Escape}");
    expect(defaultProps.onClose).toHaveBeenCalled();
  });

  it("shows validation errors for empty required fields", async () => {
    const user = userEvent.setup();
    renderWithProviders(<ComponentName {...defaultProps} />);

    await user.click(screen.getByText("Submit"));
    expect(screen.getByText(/required/i)).toBeInTheDocument();
  });

  it("submits form with valid data", async () => {
    const user = userEvent.setup();
    renderWithProviders(<ComponentName {...defaultProps} />);

    await user.type(screen.getByLabelText(/field/i), "Test value");
    await user.click(screen.getByText("Submit"));

    await waitFor(() => {
      expect(defaultProps.onSuccess).toHaveBeenCalled();
    });
  });
});
```

### What to Test
1. **Rendering** - Component renders correctly in different states
2. **User interactions** - Click, type, keyboard events work as expected
3. **Validation** - Form validation shows appropriate errors
4. **API calls** - Mock and verify API interactions
5. **Accessibility** - Keyboard navigation, ARIA attributes
6. **Edge cases** - Empty states, loading states, error states

## File Naming Convention

| Type | Pattern | Example |
|------|---------|---------|
| Component | PascalCase | `CreateTaskModal.tsx` |
| Types | kebab-case | `time-tracking.ts` |
| API | kebab-case | `overtime-requests.ts` |
| Hooks | camelCase with use prefix | `useAuth.ts` |
| Tests | ComponentName.test.tsx | `CreateTaskModal.test.tsx` |

## Component Location

Place components in the appropriate feature folder:

```
components/
├── employee/      # Employee-specific components
├── pm/            # Project Manager components
├── gm/            # General Manager components
├── admin/         # Admin components
├── time-tracking/ # Time tracking feature
├── workload/      # Workload feature
├── common/        # Shared/reusable components
├── shared/        # Cross-feature shared components
└── auth/          # Authentication components

hooks/
├── useFormState.ts    # Form state management hook
├── useModal.ts        # Modal state management hook
├── useDebounce.ts     # Debounce hook
└── useAuth.ts         # Authentication hook
```

## Accessibility Checklist

When creating components, ensure:

- [ ] All form inputs have associated `<label>` elements with `htmlFor`
- [ ] Interactive elements are keyboard accessible
- [ ] Modals trap focus and close on Escape
- [ ] Error messages are associated with inputs via `aria-describedby`
- [ ] Invalid inputs have `aria-invalid="true"`
- [ ] Loading states are announced to screen readers
- [ ] Color is not the only means of conveying information
- [ ] Focus is visible and follows logical order
