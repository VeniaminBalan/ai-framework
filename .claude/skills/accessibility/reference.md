# Accessibility Reference

Detailed rules and conventions for building inclusive React applications.

**Remember: Accessibility is NOT optional. WCAG AA compliance is required.**

## Semantic HTML

Use semantic HTML first. Only add ARIA when semantic HTML isn't sufficient.

### Element Selection
| Instead of | Use |
|------------|-----|
| `<div onClick>` | `<button>` |
| `<div>` for navigation | `<nav>` |
| `<div>` for main content | `<main>` |
| `<div>` for sections | `<section>`, `<article>` |
| `<span>` for headings | `<h1>` - `<h6>` |

## ARIA Attributes

### ARIA Rules
1. Use semantic HTML first
2. Only add ARIA when semantic HTML isn't sufficient
3. Never override semantic HTML with ARIA

### Common ARIA Patterns

```typescript
// Button with icon only
<button aria-label="Close dialog" onClick={handleClose}>
  <CloseIcon aria-hidden="true" />
</button>

// Loading/expanded states
<button aria-busy={isLoading} disabled={isLoading}>Submit</button>
<button aria-expanded={isOpen} aria-controls="menu">Menu</button>

// Live regions for dynamic content
<div aria-live="polite" aria-atomic="true">{statusMessage}</div>
<div role="alert" aria-live="assertive">{errorMessage}</div>

// Current page in navigation
<nav>
  <a href="/home" aria-current="page">Home</a>
  <a href="/about">About</a>
</nav>

// Required fields with errors
<input
  type="email"
  required
  aria-required="true"
  aria-invalid={!!error}
  aria-describedby={error ? 'email-error' : undefined}
/>
{error && <span id="email-error" role="alert">{error}</span>}
```

## Form Accessibility

### Always Use Labels

```typescript
// Preferred - explicit label
<label htmlFor="username">Username</label>
<input id="username" type="text" name="username" />

// Never do this
<input type="text" placeholder="Username" /> // No label!
```

### Form Field Pattern

```typescript
<div className="form-field">
  <label htmlFor={id}>
    {label}
    {required && <span aria-label="required"> *</span>}
  </label>
  {helpText && <p id={`${id}-help`}>{helpText}</p>}
  <input
    id={id}
    type={type}
    required={required}
    aria-required={required}
    aria-invalid={!!error}
    aria-describedby={[
      helpText && `${id}-help`,
      error && `${id}-error`
    ].filter(Boolean).join(' ') || undefined}
  />
  {error && <span id={`${id}-error`} role="alert">{error}</span>}
</div>
```

## Keyboard Navigation

### Focus Management
- All interactive elements must be keyboard accessible
- Focus order must be logical
- Focus must be trapped in modals
- Focus must return to trigger element when modal closes

### Key Bindings
| Key | Action |
|-----|--------|
| Tab | Move focus forward |
| Shift+Tab | Move focus backward |
| Enter/Space | Activate button/link |
| Escape | Close modal/dropdown |
| Arrow keys | Navigate within components |

## Color Contrast Requirements

**WCAG AA:** 4.5:1 normal text, 3:1 large text
**WCAG AAA:** 7:1 normal text, 4.5:1 large text

### Good Contrast Examples
```css
.text-primary { color: #1f2937; } /* 16:1 on white */
.button-primary { background: #2563eb; color: #ffffff; } /* 8.6:1 */
```

### Color-Only Indicators
Never use color as the only way to convey information:

```typescript
// Bad - color only
<span style={{ color: 'red' }}>Error</span>

// Good - icon + color + text
<span className="text-red-600">
  <ErrorIcon aria-hidden="true" />
  Error: Failed to save
</span>
```

## Images and Media

### Alt Text
```typescript
// Informative image
<img src="/avatar.jpg" alt="John Doe's profile picture" />

// Decorative image
<img src="/decorative-line.svg" alt="" role="presentation" />

// Complex image with description
<figure>
  <img src="/chart.png" alt="Sales by region" aria-describedby="chart-desc" />
  <figcaption id="chart-desc">The chart shows sales data...</figcaption>
</figure>
```

### Video Captions
```typescript
<video controls>
  <source src="/video.mp4" type="video/mp4" />
  <track kind="captions" src="/captions-en.vtt" srclang="en" label="English" default />
</video>
```

## Testing Accessibility

### Automated Testing with jest-axe

```typescript
import { render } from '@testing-library/react';
import { axe, toHaveNoViolations } from 'jest-axe';

expect.extend(toHaveNoViolations);

test('Component should be accessible', async () => {
  const { container } = render(<Button>Click me</Button>);
  const results = await axe(container);
  expect(results).toHaveNoViolations();
});
```

### Manual Testing Checklist

- [ ] Keyboard-only navigation (no mouse)
- [ ] Screen reader testing (NVDA, JAWS, VoiceOver)
- [ ] Color contrast check (DevTools)
- [ ] Automated audits (Lighthouse, axe DevTools)
- [ ] Browser zoom at 200%
- [ ] High contrast mode
- [ ] Visible focus indicators
- [ ] All interactive elements keyboard accessible
