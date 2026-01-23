# i18n Examples

## Basic Usage

### Client Components

```typescript
"use client";

import { useTranslations } from "next-intl";

export default function MyComponent() {
  const t = useTranslations("feature.section");
  const tCommon = useTranslations("common");

  return (
    <div>
      <h1>{t("title")}</h1>
      <button>{tCommon("cancel")}</button>
    </div>
  );
}
```

### Server Components

```typescript
import { getTranslations } from "next-intl/server";

export default async function ServerPage() {
  const t = await getTranslations("dashboard");

  return <h1>{t("title")}</h1>;
}
```

## With Parameters

### Message Definition

```json
{
  "detailsModal": {
    "title": "Request #{id}",
    "remaining": "{hours}h remaining",
    "greeting": "Hello, {name}!"
  }
}
```

### Usage

```typescript
t("title", { id: request.id })
t("remaining", { hours: 5 })
t("greeting", { name: user.firstName })
```

## Localized Navigation

```typescript
import { Link, useRouter } from "@/i18n/routing";

// Link automatically handles locale prefixes
<Link href="/dashboard">Dashboard</Link>

// Programmatic navigation
const router = useRouter();
router.push("/dashboard");
```

## Error Translation

```typescript
import { translateBackendError } from "@/lib/utils/translateError";

try {
  await apiCall();
} catch (err) {
  const errorMessage = err instanceof Error ? err.message : t("errors.default");
  setError(translateBackendError(errorMessage, tErrors));
}
```

## Complete Component Example

```typescript
"use client";

import { useState, useCallback } from "react";
import { useTranslations } from "next-intl";

export default function CreateUserModal({
  isOpen,
  onClose,
  onSuccess,
}: CreateUserModalProps) {
  const t = useTranslations("admin.users.createModal");
  const tCommon = useTranslations("common");
  const tErrors = useTranslations("errors");

  const [formData, setFormData] = useState(initialFormData);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = useCallback(async () => {
    // Validation with translated messages
    const validationErrors: Record<string, string> = {};
    if (!formData.name.trim()) {
      validationErrors.name = t("validation.nameRequired");
    }
    if (!formData.email.trim()) {
      validationErrors.email = t("validation.emailRequired");
    }
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    try {
      setIsSubmitting(true);
      await createUser(formData);
      onSuccess();
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : tErrors("default"));
    } finally {
      setIsSubmitting(false);
    }
  }, [formData, t, tErrors, onSuccess, onClose]);

  if (!isOpen) return null;

  return (
    <div className="modal modal-open">
      <div className="modal-box">
        <h3 className="font-bold text-2xl mb-6">{t("title")}</h3>

        <form onSubmit={handleSubmit}>
          <div className="form-control">
            <label className="label">
              <span className="label-text">{t("form.name")}</span>
            </label>
            <input
              type="text"
              placeholder={t("form.namePlaceholder")}
              className={`input input-bordered ${errors.name ? "input-error" : ""}`}
              value={formData.name}
              onChange={(e) => handleChange("name", e.target.value)}
            />
            {errors.name && (
              <label className="label">
                <span className="label-text-alt text-error">{errors.name}</span>
              </label>
            )}
          </div>

          <div className="modal-action">
            <button type="button" onClick={onClose} className="btn btn-ghost">
              {tCommon("cancel")}
            </button>
            <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
              {isSubmitting ? t("submitting") : tCommon("submit")}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
```

## Message File Structure Example

```json
{
  "common": {
    "cancel": "Cancel",
    "save": "Save",
    "submit": "Submit",
    "delete": "Delete",
    "edit": "Edit",
    "create": "Create",
    "loading": "Loading...",
    "retry": "Try Again",
    "yes": "Yes",
    "no": "No"
  },
  "errors": {
    "default": "An error occurred. Please try again.",
    "network": "Network error. Check your connection.",
    "unauthorized": "Please log in to continue.",
    "forbidden": "You don't have permission for this action.",
    "notFound": "Resource not found."
  },
  "admin": {
    "users": {
      "title": "User Management",
      "table": {
        "name": "Name",
        "email": "Email",
        "role": "Role",
        "status": "Status",
        "actions": "Actions",
        "noData": "No users found"
      },
      "createModal": {
        "title": "Create New User",
        "form": {
          "name": "Full Name",
          "namePlaceholder": "Enter full name",
          "email": "Email Address",
          "emailPlaceholder": "Enter email address",
          "role": "Role",
          "selectRole": "Select a role"
        },
        "validation": {
          "nameRequired": "Name is required",
          "emailRequired": "Email is required",
          "emailInvalid": "Please enter a valid email",
          "roleRequired": "Please select a role"
        },
        "submitting": "Creating user..."
      },
      "status": {
        "active": "Active",
        "inactive": "Inactive",
        "pending": "Pending"
      }
    }
  }
}
```

## Multiple Translation Hooks

```typescript
export default function ComplexComponent() {
  // Feature-specific translations
  const t = useTranslations("feature.section");

  // Common translations (buttons, labels)
  const tCommon = useTranslations("common");

  // Error messages
  const tErrors = useTranslations("errors");

  // Validation messages
  const tValidation = useTranslations("feature.section.validation");

  return (
    <div>
      <h1>{t("title")}</h1>
      <button>{tCommon("submit")}</button>
      {error && <p>{tErrors("default")}</p>}
      {validationError && <p>{tValidation("fieldRequired")}</p>}
    </div>
  );
}
```
