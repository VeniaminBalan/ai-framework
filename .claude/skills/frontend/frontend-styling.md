# Frontend Styling

## Overview
This skill covers styling approaches and best practices for React applications.

## Styling Approaches

The project uses **Tailwind CSS** as the primary styling solution (as seen in the workspace).

### Tailwind CSS (Current Project)

```typescript
// Component with Tailwind classes
export const Button = ({ variant = 'primary', children, ...props }: ButtonProps) => {
  const baseClasses = 'px-4 py-2 rounded-md font-medium transition-colors focus:outline-none focus:ring-2';
  
  const variantClasses = {
    primary: 'bg-blue-600 text-white hover:bg-blue-700 focus:ring-blue-500',
    secondary: 'bg-gray-200 text-gray-900 hover:bg-gray-300 focus:ring-gray-500',
    danger: 'bg-red-600 text-white hover:bg-red-700 focus:ring-red-500',
  };

  return (
    <button
      className={`${baseClasses} ${variantClasses[variant]}`}
      {...props}
    >
      {children}
    </button>
  );
};
```

### Using clsx or classnames for Conditional Classes

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

### Tailwind with React Hook Form

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

## CSS Modules (Alternative Approach)

```typescript
// Button.module.css
.button {
  padding: 0.5rem 1rem;
  border-radius: 0.25rem;
  font-weight: 500;
  transition: background-color 0.2s;
  border: none;
  cursor: pointer;
}

.buttonPrimary {
  composes: button;
  background-color: var(--color-primary);
  color: white;
}

.buttonPrimary:hover {
  background-color: var(--color-primary-dark);
}

.buttonSecondary {
  composes: button;
  background-color: var(--color-secondary);
  color: var(--color-text);
}

.buttonDisabled {
  opacity: 0.5;
  cursor: not-allowed;
}

// Button.tsx
import styles from './Button.module.css';
import clsx from 'clsx';

export const Button = ({ variant = 'primary', disabled, children }: ButtonProps) => {
  return (
    <button
      className={clsx(
        styles[`button${variant.charAt(0).toUpperCase() + variant.slice(1)}`],
        disabled && styles.buttonDisabled
      )}
      disabled={disabled}
    >
      {children}
    </button>
  );
};
```

## Design Tokens / CSS Variables

```css
/* globals.css */
:root {
  /* Colors */
  --color-primary: #3b82f6;
  --color-primary-dark: #2563eb;
  --color-primary-light: #60a5fa;
  
  --color-secondary: #64748b;
  --color-danger: #ef4444;
  --color-success: #10b981;
  --color-warning: #f59e0b;
  
  /* Text */
  --color-text: #1f2937;
  --color-text-light: #6b7280;
  --color-text-lighter: #9ca3af;
  
  /* Background */
  --color-bg: #ffffff;
  --color-bg-secondary: #f9fafb;
  --color-bg-tertiary: #f3f4f6;
  
  /* Spacing */
  --spacing-xs: 0.25rem;
  --spacing-sm: 0.5rem;
  --spacing-md: 1rem;
  --spacing-lg: 1.5rem;
  --spacing-xl: 2rem;
  --spacing-2xl: 3rem;
  
  /* Border radius */
  --radius-sm: 0.25rem;
  --radius-md: 0.375rem;
  --radius-lg: 0.5rem;
  --radius-full: 9999px;
  
  /* Shadows */
  --shadow-sm: 0 1px 2px 0 rgb(0 0 0 / 0.05);
  --shadow-md: 0 4px 6px -1px rgb(0 0 0 / 0.1);
  --shadow-lg: 0 10px 15px -3px rgb(0 0 0 / 0.1);
  
  /* Typography */
  --font-sans: system-ui, -apple-system, sans-serif;
  --font-mono: 'Courier New', monospace;
  
  --text-xs: 0.75rem;
  --text-sm: 0.875rem;
  --text-base: 1rem;
  --text-lg: 1.125rem;
  --text-xl: 1.25rem;
  --text-2xl: 1.5rem;
  
  /* Transitions */
  --transition-fast: 150ms;
  --transition-base: 200ms;
  --transition-slow: 300ms;
}

[data-theme='dark'] {
  --color-text: #f9fafb;
  --color-text-light: #d1d5db;
  --color-bg: #1f2937;
  --color-bg-secondary: #111827;
}
```

## Responsive Design

### Tailwind Responsive Utilities

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

### CSS Media Queries

```css
/* Mobile first approach */
.container {
  padding: 1rem;
  width: 100%;
}

/* Tablet and up */
@media (min-width: 768px) {
  .container {
    padding: 2rem;
    max-width: 768px;
    margin: 0 auto;
  }
}

/* Desktop and up */
@media (min-width: 1024px) {
  .container {
    max-width: 1024px;
    padding: 3rem;
  }
}

/* Large desktop */
@media (min-width: 1280px) {
  .container {
    max-width: 1280px;
  }
}
```

### Responsive Hook

```typescript
// hooks/useMediaQuery.ts
import { useState, useEffect } from 'react';

export const useMediaQuery = (query: string): boolean => {
  const [matches, setMatches] = useState(() => {
    return window.matchMedia(query).matches;
  });

  useEffect(() => {
    const mediaQuery = window.matchMedia(query);
    const handler = (event: MediaQueryListEvent) => setMatches(event.matches);

    mediaQuery.addEventListener('change', handler);
    return () => mediaQuery.removeEventListener('change', handler);
  }, [query]);

  return matches;
};

// Predefined breakpoints
export const useBreakpoint = () => {
  const isMobile = useMediaQuery('(max-width: 767px)');
  const isTablet = useMediaQuery('(min-width: 768px) and (max-width: 1023px)');
  const isDesktop = useMediaQuery('(min-width: 1024px)');

  return { isMobile, isTablet, isDesktop };
};

// Usage
const MyComponent = () => {
  const { isMobile, isDesktop } = useBreakpoint();

  return (
    <div>
      {isMobile && <MobileMenu />}
      {isDesktop && <DesktopMenu />}
    </div>
  );
};
```

## Dark Mode Support

```typescript
// hooks/useTheme.ts
import { createContext, useContext, useEffect, useState } from 'react';

type Theme = 'light' | 'dark' | 'system';

interface ThemeContextType {
  theme: Theme;
  setTheme: (theme: Theme) => void;
  resolvedTheme: 'light' | 'dark';
}

const ThemeContext = createContext<ThemeContextType | undefined>(undefined);

export const ThemeProvider = ({ children }: { children: React.ReactNode }) => {
  const [theme, setTheme] = useState<Theme>(() => {
    return (localStorage.getItem('theme') as Theme) || 'system';
  });

  const [resolvedTheme, setResolvedTheme] = useState<'light' | 'dark'>('light');

  useEffect(() => {
    const root = window.document.documentElement;
    
    const systemTheme = window.matchMedia('(prefers-color-scheme: dark)').matches
      ? 'dark'
      : 'light';
    
    const effectiveTheme = theme === 'system' ? systemTheme : theme;
    
    setResolvedTheme(effectiveTheme);
    root.setAttribute('data-theme', effectiveTheme);
    localStorage.setItem('theme', theme);
  }, [theme]);

  return (
    <ThemeContext.Provider value={{ theme, setTheme, resolvedTheme }}>
      {children}
    </ThemeContext.Provider>
  );
};

export const useTheme = () => {
  const context = useContext(ThemeContext);
  if (!context) {
    throw new Error('useTheme must be used within ThemeProvider');
  }
  return context;
};

// ThemeToggle component
export const ThemeToggle = () => {
  const { theme, setTheme } = useTheme();

  return (
    <button
      onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
      className="p-2 rounded-md hover:bg-gray-100 dark:hover:bg-gray-800"
      aria-label="Toggle theme"
    >
      {theme === 'dark' ? '🌞' : '🌙'}
    </button>
  );
};
```

## Component Variants Pattern

```typescript
// Using cva (class-variance-authority) with Tailwind
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
    <button
      className={buttonVariants({ variant, size, className })}
      {...props}
    />
  );
};

// Usage
<Button variant="primary" size="lg">Click me</Button>
<Button variant="outline" size="sm">Cancel</Button>
```

## Animation and Transitions

```css
/* Smooth transitions */
.fade-in {
  animation: fadeIn 0.3s ease-in;
}

@keyframes fadeIn {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
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

/* Transition utilities */
.transition-all {
  transition: all 0.2s ease-in-out;
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

// List with stagger
export const StaggerList = ({ items }: { items: Item[] }) => {
  return (
    <motion.ul
      initial="hidden"
      animate="visible"
      variants={{
        visible: {
          transition: {
            staggerChildren: 0.1,
          },
        },
      }}
    >
      {items.map((item) => (
        <motion.li
          key={item.id}
          variants={{
            hidden: { opacity: 0, x: -20 },
            visible: { opacity: 1, x: 0 },
          }}
        >
          {item.name}
        </motion.li>
      ))}
    </motion.ul>
  );
};
```

## Styling Best Practices

1. **Consistent approach**: Use one primary styling method throughout the project
2. **Mobile-first**: Start with mobile styles, then add breakpoints
3. **Design tokens**: Use CSS variables for colors, spacing, typography
4. **Avoid inline styles**: Except for truly dynamic values
5. **BEM or utility-first**: Choose a naming convention and stick to it
6. **Color contrast**: Ensure WCAG AA compliance (4.5:1 for text)
7. **Semantic classes**: Name classes by purpose, not appearance
8. **Reusable components**: Extract common UI patterns
9. **Performance**: Minimize CSS bundle size, use code splitting
10. **Dark mode**: Support system preferences and manual toggle

## Accessibility in Styling

```css
/* Focus indicators */
button:focus,
a:focus {
  outline: 2px solid var(--color-primary);
  outline-offset: 2px;
}

/* Skip to content link */
.skip-link {
  position: absolute;
  top: -40px;
  left: 0;
  background: var(--color-primary);
  color: white;
  padding: 8px;
  text-decoration: none;
  z-index: 100;
}

.skip-link:focus {
  top: 0;
}

/* Reduced motion */
@media (prefers-reduced-motion: reduce) {
  *,
  *::before,
  *::after {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
  }
}

/* High contrast mode */
@media (prefers-contrast: high) {
  .card {
    border: 2px solid currentColor;
  }
}
```

## Checklist for Styling
- [ ] Consistent styling approach used throughout
- [ ] Mobile-first responsive design
- [ ] Design tokens/CSS variables for consistency
- [ ] Color contrast meets WCAG AA standards
- [ ] Focus indicators are visible
- [ ] Dark mode support (if applicable)
- [ ] Reduced motion preferences respected
- [ ] No inline styles (except dynamic values)
- [ ] Reusable component variants
- [ ] Proper spacing and typography scale
- [ ] Responsive images and media
- [ ] Performance optimized (minimal bundle size)
