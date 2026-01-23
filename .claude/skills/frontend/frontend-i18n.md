# Frontend i18n Skill (next-intl)

## Overview
Core patterns and guidelines for internationalization (i18n) in the TimeManagement frontend application using next-intl.

**Related Skills:**
- `frontend-i18n-advanced.md` - Pluralization, rich text, select/gender patterns
- `frontend-i18n-formatting.md` - Date, time, number, and list formatting
- `frontend-i18n-validation.md` - Translation validation and testing

## Supported Languages
- **English (en)** - `messages/en.json`
- **Romanian (ro)** - `messages/ro.json` (Default)

## Configuration

### Routing Configuration
Location: `i18n/routing.ts`
```typescript
import { defineRouting } from "next-intl/routing";
import { createNavigation } from "next-intl/navigation";

export const routing = defineRouting({
  locales: ["en", "ro"],
  defaultLocale: "ro",
  localePrefix: "as-needed",
});

export const { Link, redirect, usePathname, useRouter } = createNavigation(routing);
```

### Type Safety
Location: `types/i18n.d.ts`
```typescript
import en from "@/messages/en.json";

type Messages = typeof en;

declare global {
  interface IntlMessages extends Messages {}
}
```

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

### With Parameters
```json
{
  "detailsModal": {
    "title": "Request #{id}",
    "remaining": "{hours}h remaining"
  }
}
```

```typescript
t("title", { id: request.id })
t("remaining", { hours: 5 })
```

## Message File Structure

```json
{
  "common": {
    "cancel": "Cancel",
    "save": "Save",
    "submit": "Submit",
    "delete": "Delete",
    "loading": "Loading..."
  },
  "feature": {
    "section": {
      "title": "Section Title",
      "validation": {
        "fieldRequired": "This field is required"
      },
      "errors": {
        "failedToLoad": "Failed to load data"
      }
    }
  }
}
```

## Naming Conventions

### Structure Pattern
```
feature.section.element
```

### Common Sections
| Section | Purpose |
|---------|---------|
| `title` | Page/section titles |
| `form` | Form labels/placeholders |
| `validation` | Validation messages |
| `errors` | Error messages |
| `table` | Table headers/content |
| `status` | Status labels |

### Rules
1. Use camelCase for keys
2. Be descriptive but concise
3. Group related translations together

```json
// Good
{ "createTaskModal": { "title": "Create New Task" } }

// Bad
{ "create_task_modal": { "Title": "Create New Task" } }
```

## Common Patterns

### Button States
```json
{
  "form": {
    "submit": "Submit",
    "submitting": "Submitting...",
    "save": "Save",
    "saving": "Saving..."
  }
}
```

### Status Badges
```json
{
  "status": {
    "active": "Active",
    "pending": "Pending",
    "approved": "Approved",
    "denied": "Denied"
  }
}
```

### Empty States
```json
{
  "table": {
    "noData": "No data found",
    "noResults": "No results match your search"
  }
}
```

## Localized Navigation

```typescript
import { Link, useRouter } from "@/i18n/routing";

// Automatically handles locale prefixes
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

## Feature Namespaces

```typescript
// Employee
const t = useTranslations("employee.overtimeRequests");

// Time Tracking
const t = useTranslations("timeTracking.createTaskModal");

// Project Manager
const t = useTranslations("pm.projects");

// Admin
const t = useTranslations("admin.users");
```

## Checklist

When creating a component, ensure these are translated:

- [ ] Page/section title
- [ ] Form field labels and placeholders
- [ ] Button labels (including loading states)
- [ ] Validation and error messages
- [ ] Empty state messages
- [ ] Table column headers
- [ ] Status labels
