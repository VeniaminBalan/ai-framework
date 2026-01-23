# Forms & Validation Reference

Detailed rules and conventions for form handling and validation.

## Required Libraries

- **react-hook-form**: Form state management
- **zod**: Schema validation with TypeScript inference
- **@hookform/resolvers**: Zod integration

## Basic Pattern

```typescript
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

// 1. Define schema
const schema = z.object({
  email: z.string().email('Invalid email'),
  password: z.string().min(8, 'Min 8 characters'),
});

// 2. Infer type
type FormData = z.infer<typeof schema>;

// 3. Use in component
const { register, handleSubmit, formState: { errors, isSubmitting } } =
  useForm<FormData>({ resolver: zodResolver(schema) });
```

## Zod Validation Patterns

### Common Validations

```typescript
const schema = z.object({
  // Strings
  required: z.string().min(1, 'Required'),
  email: z.string().email('Invalid email'),
  url: z.string().url('Invalid URL'),
  pattern: z.string().regex(/^[A-Z0-9]+$/, 'Only uppercase and numbers'),

  // Numbers
  age: z.number().min(18, 'Must be 18+').max(100),
  count: z.number().int('Must be integer').positive(),

  // Dates
  birthDate: z.date().max(new Date(), 'Cannot be future'),

  // Arrays
  tags: z.array(z.string()).min(1, 'At least one tag'),

  // Optional/Nullable
  optional: z.string().optional(),
  nullable: z.string().nullable(),
  withDefault: z.string().default('default'),

  // Enum
  role: z.enum(['admin', 'user', 'guest']),

  // Boolean
  terms: z.boolean().refine(v => v === true, 'Must accept'),
});
```

### Conditional Validation

```typescript
const conditionalSchema = z.object({
  hasAddress: z.boolean(),
  address: z.string().optional(),
}).refine((d) => !d.hasAddress || !!d.address, {
  message: 'Address required when hasAddress is true',
  path: ['address'],
});
```

### Cross-field Validation

```typescript
const schema = z.object({
  password: z.string().min(8),
  confirmPassword: z.string(),
}).refine((d) => d.password === d.confirmPassword, {
  message: "Passwords don't match",
  path: ['confirmPassword'],
});
```

### Async Validation

```typescript
const asyncSchema = z.object({
  username: z.string().refine(
    async (val) => !(await checkExists(val)),
    'Username taken'
  ),
});
```

### Transform Data

```typescript
const transformSchema = z.object({
  email: z.string().email().transform(v => v.toLowerCase()),
  age: z.string().transform(v => parseInt(v)),
});
```

## Centralized Schemas

```typescript
// schemas/userSchema.ts
export const userSchema = z.object({
  firstName: z.string().min(1, 'Required'),
  lastName: z.string().min(1, 'Required'),
  email: z.string().email('Invalid email'),
  role: z.enum(['admin', 'user', 'guest']),
});

export const createUserSchema = userSchema.extend({
  password: z.string().min(8),
  confirmPassword: z.string(),
}).refine((d) => d.password === d.confirmPassword, {
  message: "Passwords don't match",
  path: ['confirmPassword'],
});

export const updateUserSchema = userSchema.partial().extend({
  id: z.string().uuid(),
});

export type User = z.infer<typeof userSchema>;
export type CreateUserInput = z.infer<typeof createUserSchema>;
export type UpdateUserInput = z.infer<typeof updateUserSchema>;
```

## Best Practices

1. **Centralize schemas** in `schemas/` folder
2. **Type inference** with `z.infer<typeof schema>`
3. **User-friendly messages** - clear and actionable
4. **Validate on blur** - `mode: 'onBlur'` for better UX
5. **Reuse schemas** between create/update forms
6. **Server validation** - always validate on backend
7. **Accessible errors** - use ARIA attributes
8. **Loading states** - disable submit during submission
9. **Success feedback** - toast or redirect after submit
10. **Error recovery** - allow easy correction
