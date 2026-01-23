# Frontend Forms & Validation

## Overview
Form handling and validation using react-hook-form and zod for type-safe, accessible forms.

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
}).refine((d) => d.password === d.confirmPassword, {
  message: "Passwords don't match",
  path: ['confirmPassword'],
});

// 2. Infer type
type FormData = z.infer<typeof schema>;

// 3. Use in component
export const LoginForm = () => {
  const { register, handleSubmit, formState: { errors, isSubmitting } } = 
    useForm<FormData>({ resolver: zodResolver(schema) });

  return (
    <form onSubmit={handleSubmit(async (data) => await authService.login(data))}>
      <div>
        <label htmlFor="email">Email</label>
        <input id="email" {...register('email')} aria-invalid={!!errors.email} />
        {errors.email && <span role="alert">{errors.email.message}</span>}
      </div>
      <button type="submit" disabled={isSubmitting}>Submit</button>
    </form>
  );
};
```

## Zod Validation Patterns

```typescript
// Common validations
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

// Conditional validation
const conditionalSchema = z.object({
  hasAddress: z.boolean(),
  address: z.string().optional(),
}).refine((d) => !d.hasAddress || !!d.address, {
  message: 'Address required when hasAddress is true',
  path: ['address'],
});

// Async validation
const asyncSchema = z.object({
  username: z.string().refine(
    async (val) => !(await checkExists(val)),
    'Username taken'
  ),
});

// Transform data
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

## Advanced Features

```typescript
// Watch values
const email = watch('email');
const [email, password] = watch(['email', 'password']);

// Controlled components
<Controller
  name="role"
  control={control}
  render={({ field, fieldState }) => (
    <Select {...field} error={fieldState.error?.message} />
  )}
/>

// Dynamic fields
const { fields, append, remove } = useFieldArray({ control, name: 'items' });
{fields.map((field, index) => (
  <div key={field.id}>
    <input {...register(`items.${index}.name`)} />
    <button onClick={() => remove(index)}>Remove</button>
  </div>
))}

// Reset form
reset(); // Default values
reset({ email: 'new@email.com' }); // Specific values

// Set value programmatically
setValue('email', 'new@email.com', { shouldValidate: true });

// Custom validation
<input {...register('username', {
  validate: {
    notAdmin: v => v !== 'admin' || 'Cannot be admin',
    noSpaces: v => !/\s/.test(v) || 'No spaces',
  }
})} />
```

## With React Query

```typescript
export const CreateUserForm = () => {
  const { register, handleSubmit, formState: { errors }, reset } = 
    useForm<CreateUserInput>({ resolver: zodResolver(createUserSchema) });
  
  const createUser = useCreateUser();

  const onSubmit = (data: CreateUserInput) => {
    createUser.mutate(data, {
      onSuccess: () => { toast.success('Created'); reset(); },
      onError: (err) => toast.error(handleApiError(err)),
    });
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)}>
      {/* fields */}
      <button type="submit" disabled={createUser.isPending}>
        {createUser.isPending ? 'Creating...' : 'Create'}
      </button>
    </form>
  );
};
```

## Reusable Components

```typescript
// components/FormField.tsx
interface FormFieldProps<T> {
  name: Path<T>;
  label: string;
  register: UseFormRegister<T>;
  errors: FieldErrors<T>;
  type?: string;
}

export function FormField<T extends Record<string, any>>({
  name, label, type = 'text', register, errors
}: FormFieldProps<T>) {
  const error = errors[name];
  return (
    <div>
      <label htmlFor={name}>{label}</label>
      <input
        id={name}
        type={type}
        {...register(name)}
        aria-invalid={!!error}
        aria-describedby={error ? `${name}-error` : undefined}
      />
      {error && (
        <span id={`${name}-error`} role="alert">
          {error.message as string}
        </span>
      )}
    </div>
  );
}
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

## Checklist
- [ ] Using react-hook-form + zod
- [ ] TypeScript types from schema
- [ ] Proper labels and ARIA attributes
- [ ] User-friendly error messages
- [ ] Loading/disabled states
- [ ] Success/error feedback
- [ ] Form reset after success
- [ ] Server-side validation
