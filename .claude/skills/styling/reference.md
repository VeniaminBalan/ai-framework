# Styling Reference

Detailed rules and conventions for styling React applications with Tailwind CSS.

## Primary Styling Solution

The project uses **Tailwind CSS** as the primary styling solution.

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

  /* Border radius */
  --radius-sm: 0.25rem;
  --radius-md: 0.375rem;
  --radius-lg: 0.5rem;
  --radius-full: 9999px;

  /* Shadows */
  --shadow-sm: 0 1px 2px 0 rgb(0 0 0 / 0.05);
  --shadow-md: 0 4px 6px -1px rgb(0 0 0 / 0.1);
  --shadow-lg: 0 10px 15px -3px rgb(0 0 0 / 0.1);

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

### Tailwind Breakpoints
- `sm`: 640px
- `md`: 768px
- `lg`: 1024px
- `xl`: 1280px
- `2xl`: 1536px

### Mobile-First Approach
```css
/* Start with mobile styles, then add breakpoints */
.container {
  padding: 1rem;      /* Mobile */
}

@media (min-width: 768px) {
  .container {
    padding: 2rem;    /* Tablet and up */
  }
}

@media (min-width: 1024px) {
  .container {
    padding: 3rem;    /* Desktop and up */
  }
}
```

## Accessibility in Styling

### Focus Indicators
```css
button:focus,
a:focus {
  outline: 2px solid var(--color-primary);
  outline-offset: 2px;
}
```

### Skip to Content Link
```css
.skip-link {
  position: absolute;
  top: -40px;
  left: 0;
  background: var(--color-primary);
  color: white;
  padding: 8px;
  z-index: 100;
}

.skip-link:focus {
  top: 0;
}
```

### Reduced Motion
```css
@media (prefers-reduced-motion: reduce) {
  *,
  *::before,
  *::after {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
  }
}
```

### High Contrast Mode
```css
@media (prefers-contrast: high) {
  .card {
    border: 2px solid currentColor;
  }
}
```

## Color Contrast Requirements

**WCAG AA:** 4.5:1 normal text, 3:1 large text
**WCAG AAA:** 7:1 normal text, 4.5:1 large text

```css
/* Good contrast */
.text-primary { color: #1f2937; } /* 16:1 on white */
.button-primary { background: #2563eb; color: #ffffff; } /* 8.6:1 */

/* Bad - insufficient */
.text-light { color: #cbd5e0; } /* 1.6:1 - FAIL */

/* Fixed */
.text-light { color: #4a5568; } /* 7.5:1 - PASS */
```

## Best Practices

1. **Consistent approach**: Use one primary styling method throughout
2. **Mobile-first**: Start with mobile styles, then add breakpoints
3. **Design tokens**: Use CSS variables for colors, spacing, typography
4. **Avoid inline styles**: Except for truly dynamic values
5. **BEM or utility-first**: Choose a naming convention and stick to it
6. **Color contrast**: Ensure WCAG AA compliance (4.5:1 for text)
7. **Semantic classes**: Name classes by purpose, not appearance
8. **Reusable components**: Extract common UI patterns
9. **Performance**: Minimize CSS bundle size, use code splitting
10. **Dark mode**: Support system preferences and manual toggle
