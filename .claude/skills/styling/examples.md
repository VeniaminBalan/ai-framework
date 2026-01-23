# Styling Examples

## Tailwind CSS Component

```typescript
export const Button = ({ variant = 'primary', children, ...props }: ButtonProps) => {
  const baseClasses = 'px-4 py-2 rounded-md font-medium transition-colors focus:outline-none focus:ring-2';

  const variantClasses = {
    primary: 'bg-blue-600 text-white hover:bg-blue-700 focus:ring-blue-500',
    secondary: 'bg-gray-200 text-gray-900 hover:bg-gray-300 focus:ring-gray-500',
    danger: 'bg-red-600 text-white hover:bg-red-700 focus:ring-red-500',
  };

  return (
    <button className={`${baseClasses} ${variantClasses[variant]}`} {...props}>
      {children}
    </button>
  );
};
```

## Conditional Classes with clsx

```typescript
import clsx from 'clsx';

export const Card = ({ children, isActive, hasError }: CardProps) => {
  return (
    <div
      className={clsx(
        'p-4 rounded-lg shadow-md',
        isActive && 'ring-2 ring-blue-500',
        hasError && 'border-red-500',
        !isActive && 'opacity-75'
      )}
    >
      {children}
    </div>
  );
};
```

## Form Input with Error State

```typescript
export const FormInput = ({ label, error, ...props }: FormInputProps) => {
  return (
    <div className="space-y-1">
      <label className="block text-sm font-medium text-gray-700">
        {label}
      </label>
      <input
        className={clsx(
          'w-full px-3 py-2 border rounded-md shadow-sm',
          'focus:outline-none focus:ring-2',
          error
            ? 'border-red-300 focus:border-red-500 focus:ring-red-500'
            : 'border-gray-300 focus:border-blue-500 focus:ring-blue-500'
        )}
        aria-invalid={!!error}
        {...props}
      />
      {error && (
        <p className="text-sm text-red-600" role="alert">
          {error}
        </p>
      )}
    </div>
  );
};
```

## Responsive Grid

```typescript
export const Grid = ({ children }: GridProps) => {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
      {children}
    </div>
  );
};

export const ResponsiveCard = () => {
  return (
    <div className="p-4 sm:p-6 md:p-8 text-sm sm:text-base md:text-lg">
      <h2 className="text-xl sm:text-2xl md:text-3xl font-bold">Title</h2>
      <p className="mt-2 sm:mt-4">Content</p>
    </div>
  );
};
```

## Component Variants with CVA

```typescript
import { cva, type VariantProps } from 'class-variance-authority';

const buttonVariants = cva(
  // Base styles
  'inline-flex items-center justify-center rounded-md font-medium transition-colors focus:outline-none focus:ring-2 disabled:opacity-50 disabled:pointer-events-none',
  {
    variants: {
      variant: {
        primary: 'bg-blue-600 text-white hover:bg-blue-700 focus:ring-blue-500',
        secondary: 'bg-gray-200 text-gray-900 hover:bg-gray-300 focus:ring-gray-500',
        outline: 'border-2 border-gray-300 hover:bg-gray-100 focus:ring-gray-500',
        ghost: 'hover:bg-gray-100 focus:ring-gray-500',
        danger: 'bg-red-600 text-white hover:bg-red-700 focus:ring-red-500',
      },
      size: {
        sm: 'px-3 py-1.5 text-sm',
        md: 'px-4 py-2 text-base',
        lg: 'px-6 py-3 text-lg',
      },
    },
    defaultVariants: {
      variant: 'primary',
      size: 'md',
    },
  }
);

interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {}

export const Button = ({ variant, size, className, ...props }: ButtonProps) => {
  return (
    <button className={buttonVariants({ variant, size, className })} {...props} />
  );
};

// Usage
<Button variant="primary" size="lg">Click me</Button>
<Button variant="outline" size="sm">Cancel</Button>
```

## Dark Mode Toggle

```typescript
// hooks/useTheme.ts
export const ThemeProvider = ({ children }: { children: React.ReactNode }) => {
  const [theme, setTheme] = useState<Theme>(() => {
    return (localStorage.getItem('theme') as Theme) || 'system';
  });

  useEffect(() => {
    const root = window.document.documentElement;
    const systemTheme = window.matchMedia('(prefers-color-scheme: dark)').matches
      ? 'dark'
      : 'light';
    const effectiveTheme = theme === 'system' ? systemTheme : theme;

    root.setAttribute('data-theme', effectiveTheme);
    localStorage.setItem('theme', theme);
  }, [theme]);

  return (
    <ThemeContext.Provider value={{ theme, setTheme }}>
      {children}
    </ThemeContext.Provider>
  );
};

export const ThemeToggle = () => {
  const { theme, setTheme } = useTheme();

  return (
    <button
      onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
      className="p-2 rounded-md hover:bg-gray-100 dark:hover:bg-gray-800"
      aria-label="Toggle theme"
    >
      {theme === 'dark' ? '☀️' : '🌙'}
    </button>
  );
};
```

## Animations

```css
/* Smooth transitions */
.fade-in {
  animation: fadeIn 0.3s ease-in;
}

@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}

.slide-in {
  animation: slideIn 0.3s ease-out;
}

@keyframes slideIn {
  from {
    transform: translateY(-10px);
    opacity: 0;
  }
  to {
    transform: translateY(0);
    opacity: 1;
  }
}
```

```typescript
// Framer Motion for complex animations
import { motion } from 'framer-motion';

export const FadeIn = ({ children }: { children: React.ReactNode }) => {
  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, y: -20 }}
      transition={{ duration: 0.3 }}
    >
      {children}
    </motion.div>
  );
};
```

## Loading Skeleton

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

## Alert Components

```typescript
// Error alert
<div className="alert alert-error mb-4" role="alert">
  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
      d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
  </svg>
  <span>{errorMessage}</span>
</div>

// Success alert
<div className="alert alert-success mb-4" role="alert">
  <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
      d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
  </svg>
  <span>{successMessage}</span>
</div>
```
