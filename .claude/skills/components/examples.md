# Component Examples

## Standard Component Template

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

  if (!isOpen) return null;

  return (
    <div className="modal modal-open" role="dialog" aria-modal="true" aria-labelledby="modal-title">
      <div className="modal-box max-w-2xl">
        <h3 id="modal-title" className="font-bold text-2xl mb-6">
          {t("title")}
        </h3>

        {error && (
          <div className="alert alert-error mb-4" role="alert">
            <span>{error}</span>
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <div className="space-y-4">
            {/* Form fields */}
          </div>

          <div className="modal-action">
            <button type="button" onClick={onClose} className="btn btn-ghost" disabled={isSubmitting}>
              {tCommon("cancel")}
            </button>
            <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
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
          {isLoading ? (
            Array.from({ length: 5 }).map((_, index) => (
              <tr key={`skeleton-${index}`}>
                <td><div className="skeleton h-4 w-32"></div></td>
                <td><div className="skeleton h-4 w-24"></div></td>
                <td><div className="skeleton h-8 w-20"></div></td>
              </tr>
            ))
          ) : sortedData.length === 0 ? (
            <tr>
              <td colSpan={3} className="text-center py-12">
                <p className="text-gray-500">{t("noData")}</p>
              </td>
            </tr>
          ) : (
            sortedData.map((item) => (
              <tr key={item.id} className="hover cursor-pointer" onClick={() => onRowClick(item)}>
                <td>{item.field1}</td>
                <td>{item.field2}</td>
                <td>
                  <button
                    className="btn btn-primary btn-sm"
                    onClick={(e) => {
                      e.stopPropagation();
                      onAction(item.id);
                    }}
                  >
                    {t("actionLabel")}
                  </button>
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
