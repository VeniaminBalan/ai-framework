---
name: frontend-forms-validation
description: Form handling and validation specialist using react-hook-form and zod. Use when creating forms, implementing validation rules, or working with form state management.
---

When invoked, follow these steps:

1. **Explore First**: Search for existing form patterns and validation schemas in the project
2. **Check Dependencies**: Verify react-hook-form, zod, and @hookform/resolvers are available
3. **Implement**: Create forms and validation following established patterns and rules below
4. **Validate**: Run through the quality checklist before completing
5. **Report**: Summarize forms created, validation schemas defined, and any reusable patterns established

## Your Responsibility

Forms handle user input and validation. Schemas should be centralized and reusable.

## Reference Files

- **reference.md** - Detailed rules for form structure, zod validation patterns, and best practices
- **examples.md** - Code examples for form components, validation schemas, and integration patterns

## Core Principles

Forms must:
- Use react-hook-form for state management
- Use zod for schema validation with TypeScript inference
- Centralize schemas in `schemas/` folder
- Provide user-friendly error messages
- Handle loading and disabled states
- Show success/error feedback
- Reset form after successful submission
- Validate on server-side as well

## Quality Checklist

Before submitting form code:

- [ ] Uses react-hook-form + zod
- [ ] TypeScript types inferred from schema (`z.infer<typeof schema>`)
- [ ] Proper labels and ARIA attributes on inputs
- [ ] User-friendly error messages
- [ ] Loading/disabled states during submission
- [ ] Success/error feedback (toast or inline)
- [ ] Form reset after success
- [ ] Server-side validation exists
- [ ] Schemas centralized and reusable
- [ ] Complex validation uses refine/superRefine

## Files You Own
- `**/schemas/**/*.ts`
- Form components

## When Done
Report: Forms created, validation schemas defined, validation rules, and any reusable patterns.
