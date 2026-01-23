# i18n Reference

Detailed rules and conventions for internationalization using next-intl.

## Supported Languages

- **English (en)** - `messages/en.json`
- **Romanian (ro)** - `messages/ro.json` (Default)

## Configuration

### Routing Configuration

```typescript
// i18n/routing.ts
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

```typescript
// types/i18n.d.ts
import en from "@/messages/en.json";

type Messages = typeof en;

declare global {
  interface IntlMessages extends Messages {}
}
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

## Common Translation Patterns

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
