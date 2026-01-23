# Forms & Validation Examples

## Basic Form Component

```typescript
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

const schema = z.object({
  email: z.string().email('Invalid email'),
  password: z.string().min(8, 'Min 8 characters'),
});

type FormData = z.infer<typeof schema>;

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

## Form with React Query

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

## Advanced Form Features

### Watch Values

```typescript
const email = watch('email');
const [email, password] = watch(['email', 'password']);
```

### Controlled Components with Controller

```typescript
<Controller
  name="role"
  control={control}
  render={({ field, fieldState }) => (
    <Select {...field} error={fieldState.error?.message} />
  )}
/>
```

### Dynamic Fields with useFieldArray

```typescript
const { fields, append, remove } = useFieldArray({ control, name: 'items' });

{fields.map((field, index) => (
  <div key={field.id}>
    <input {...register(`items.${index}.name`)} />
    <button onClick={() => remove(index)}>Remove</button>
  </div>
))}
<button onClick={() => append({ name: '' })}>Add Item</button>
```

### Reset Form

```typescript
reset(); // Reset to default values
reset({ email: 'new@email.com' }); // Reset to specific values
```

### Set Value Programmatically

```typescript
setValue('email', 'new@email.com', { shouldValidate: true });
```

### Custom Validation

```typescript
<input {...register('username', {
  validate: {
    notAdmin: v => v !== 'admin' || 'Cannot be admin',
    noSpaces: v => !/\s/.test(v) || 'No spaces',
  }
})} />
```

## Reusable Form Field Component

```typescript
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

## Complex Validation Schema Example

```typescript
const registrationSchema = z.object({
  // Basic fields
  firstName: z.string().min(1, 'First name is required'),
  lastName: z.string().min(1, 'Last name is required'),
  email: z.string().email('Invalid email address'),

  // Password with multiple rules
  password: z.string()
    .min(8, 'Password must be at least 8 characters')
    .regex(/[A-Z]/, 'Password must contain an uppercase letter')
    .regex(/[a-z]/, 'Password must contain a lowercase letter')
    .regex(/[0-9]/, 'Password must contain a number'),

  confirmPassword: z.string(),

  // Optional with conditional requirement
  phone: z.string().optional(),
  preferredContact: z.enum(['email', 'phone']),

  // Date validation
  birthDate: z.date()
    .max(new Date(), 'Birth date cannot be in the future')
    .refine(
      (date) => {
        const age = Math.floor((Date.now() - date.getTime()) / (365.25 * 24 * 60 * 60 * 1000));
        return age >= 18;
      },
      'You must be at least 18 years old'
    ),

  // Array validation
  interests: z.array(z.string()).min(1, 'Select at least one interest'),

  // Terms acceptance
  acceptTerms: z.boolean().refine(v => v === true, 'You must accept the terms'),
})
// Cross-field validation
.refine((data) => data.password === data.confirmPassword, {
  message: "Passwords don't match",
  path: ['confirmPassword'],
})
// Conditional validation
.refine((data) => data.preferredContact !== 'phone' || !!data.phone, {
  message: 'Phone is required when phone contact is preferred',
  path: ['phone'],
});

export type RegistrationInput = z.infer<typeof registrationSchema>;
```

## Form with All Features

```typescript
export const RegistrationForm = () => {
  const { t } = useTranslation();
  const {
    register,
    handleSubmit,
    control,
    watch,
    reset,
    formState: { errors, isSubmitting }
  } = useForm<RegistrationInput>({
    resolver: zodResolver(registrationSchema),
    mode: 'onBlur',
    defaultValues: {
      interests: [],
      preferredContact: 'email',
      acceptTerms: false,
    }
  });

  const preferredContact = watch('preferredContact');
  const registerMutation = useRegister();

  const onSubmit = async (data: RegistrationInput) => {
    try {
      await registerMutation.mutateAsync(data);
      toast.success(t('registration.success'));
      reset();
    } catch (error) {
      toast.error(handleApiError(error));
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate>
      {/* Form fields with proper labels, errors, and ARIA */}

      <button type="submit" disabled={isSubmitting}>
        {isSubmitting ? t('common.submitting') : t('registration.submit')}
      </button>
    </form>
  );
};
```
