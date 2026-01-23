# Frontend Accessibility (a11y)

## Overview
Accessibility requirements and best practices for inclusive React applications.

**Remember: Accessibility is NOT optional. WCAG AA compliance is required.**

## Semantic HTML

```typescript
// ❌ Bad - divs for everything
<div onClick={handleClick}>Click me</div>

// ✅ Good - semantic elements
<button onClick={handleClick}>Click me</button>

// ✅ Proper structure
<article>
  <header>
    <h1>Article Title</h1>
    <time dateTime="2024-01-15">January 15, 2024</time>
  </header>
  <main>
    <section>
      <h2>Section Title</h2>
      <p>Content...</p>
    </section>
  </main>
  <footer>Tags: React, Accessibility</footer>
</article>
```

## ARIA Attributes

**Rules:** Use semantic HTML first. Only add ARIA when semantic HTML isn't sufficient. Never override semantic HTML.

```typescript
// Button with icon
<button aria-label="Close dialog" onClick={handleClose}>
  <CloseIcon aria-hidden="true" />
</button>

// Loading/expanded states
<button aria-busy={isLoading} disabled={isLoading}>Submit</button>
<button aria-expanded={isOpen} aria-controls="menu">Menu</button>

// Live regions
<div aria-live="polite" aria-atomic="true">{statusMessage}</div>
<div role="alert" aria-live="assertive">{errorMessage}</div>

// Current page
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

```typescript
// ✅ Good - explicit label (preferred)
<label htmlFor="username">Username</label>
<input id="username" type="text" name="username" />

// ❌ Bad - no label
<input type="text" placeholder="Username" />
```

### Accessible Form Field Component

```typescript
export const FormField = ({ label, id, error, required, helpText, type = 'text', ...props }: FormFieldProps) => (
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
      aria-describedby={[helpText && `${id}-help`, error && `${id}-error`].filter(Boolean).join(' ') || undefined}
      {...props}
    />
    {error && <span id={`${id}-error`} role="alert">{error}</span>}
  </div>
);

// Select
<label htmlFor="country">Country</label>
<select id="country" aria-invalid={!!errors.country}>
  <option value="">Select a country</option>
  <option value="us">United States</option>
</select>

// Checkbox with description
<input id="terms" type="checkbox" aria-describedby="terms-desc" />
<label htmlFor="terms">I accept terms</label>
<p id="terms-desc">Please read carefully</p>

// Radio group
<fieldset>
  <legend>Choose plan</legend>
  <input type="radio" id="basic" name="plan" value="basic" />
  <label htmlFor="basic">Basic Plan</label>
  <input type="radio" id="premium" name="plan" value="premium" />
  <label htmlFor="premium">Premium Plan</label>
</fieldset>
```

## Keyboard Navigation

```typescript
// Focus management
const Modal = ({ isOpen }: { isOpen: boolean }) => {
  const closeButtonRef = useRef<HTMLButtonElement>(null);
  useEffect(() => { if (isOpen) closeButtonRef.current?.focus(); }, [isOpen]);
  return (
    <div role="dialog" aria-modal="true">
      <button ref={closeButtonRef} onClick={handleClose}>Close</button>
    </div>
  );
};
```

### Focus Trap Hook

```typescript
// hooks/useFocusTrap.ts
export const useFocusTrap = (isActive: boolean) => {
  const elementRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!isActive) return;
    const element = elementRef.current;
    if (!element) return;

    const focusableElements = element.querySelectorAll<HTMLElement>(
      'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
    );
    const firstElement = focusableElements[0];
    const lastElement = focusableElements[focusableElements.length - 1];

    const handleTabKey = (e: KeyboardEvent) => {
      if (e.key !== 'Tab') return;
      if (e.shiftKey && document.activeElement === firstElement) {
        e.preventDefault();
        lastElement.focus();
      } else if (!e.shiftKey && document.activeElement === lastElement) {
        e.preventDefault();
        firstElement.focus();
      }
    };

    element.addEventListener('keydown', handleTabKey);
    firstElement?.focus();
    return () => element.removeEventListener('keydown', handleTabKey);
  }, [isActive]);

  return elementRef;
};

// Keyboard handlers
const handleKeyDown = (e: React.KeyboardEvent, onClick: () => void) => {
  if (e.key === 'Enter' || e.key === ' ') {
    e.preventDefault();
    onClick();
  }
};

// Escape to close
const useEscapeKey = (onClose: () => void) => {
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => e.key === 'Escape' && onClose();
    document.addEventListener('keydown', handleEscape);
    return () => document.removeEventListener('keydown', handleEscape);
  }, [onClose]);
};
```

## Skip Links & Screen Readers

```typescript
// Skip link component
export const SkipLink = () => (
  <a href="#main-content" className="skip-link">Skip to main content</a>
);

// CSS
.skip-link {
  position: absolute;
  top: -40px;
  left: 0;
  background: var(--color-primary);
  color: white;
  padding: 8px;
  z-index: 100;
}
.skip-link:focus { top: 0; }

// Usage
<Layout>
  <SkipLink />
  <Header />
  <main id="main-content" tabIndex={-1}>{/* content */}</main>
</Layout>

// Visually hidden (screen reader only)
const VisuallyHidden = ({ children }: { children: React.ReactNode }) => (
  <span className="sr-only">{children}</span>
);

// CSS
.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border-width: 0;
}

// Usage
<button>
  <TrashIcon aria-hidden="true" />
  <VisuallyHidden>Delete item</VisuallyHidden>
</button>

// ❌ Bad - not descriptive
<a href="/report.pdf">Click here</a>

// ✅ Good - descriptive
<a href="/report.pdf">Download 2024 Annual Report (PDF, 2MB)</a>
```

## Color & Contrast

**WCAG AA:** 4.5:1 normal text, 3:1 large text | **WCAG AAA:** 7:1 normal text, 4.5:1 large text

```css
/* ✅ Good contrast */
.text-primary { color: #1f2937; } /* 16:1 on white */
.button-primary { background: #2563eb; color: #ffffff; } /* 8.6:1 */

/* ❌ Bad - insufficient */
.text-light { color: #cbd5e0; } /* 1.6:1 - FAIL */

/* ✅ Fixed */
.text-light { color: #4a5568; } /* 7.5:1 - PASS */
```

```typescript
// ❌ Bad - color only
<span style={{ color: 'red' }}>Error</span>

// ✅ Good - icon + color + text
<span className="text-red-600">
  <ErrorIcon aria-hidden="true" />
  Error: Failed to save
</span>
```

## Images & Media

```typescript
// ❌ Bad
<img src="/avatar.jpg" />
<img src="/chart.png" alt="chart" />

// ✅ Good - descriptive alt
<img src="/avatar.jpg" alt="John Doe's profile picture" />
<img src="/chart.png" alt="Bar chart showing sales growth 2020-2024" />

// Decorative
<img src="/decorative-line.svg" alt="" role="presentation" />

// Complex with description
<figure>
  <img src="/chart.png" alt="Sales by region" aria-describedby="chart-desc" />
  <figcaption id="chart-desc">The chart shows sales data...</figcaption>
</figure>

// Video with captions
<video controls>
  <source src="/video.mp4" type="video/mp4" />
  <track kind="captions" src="/captions-en.vtt" srclang="en" label="English" default />
  <track kind="captions" src="/captions-es.vtt" srclang="es" label="Spanish" />
</video>
```

## Accessible Components

```typescript
// Modal with focus trap
const Modal = ({ isOpen, onClose, title, children }: ModalProps) => {
  const modalRef = useFocusTrap(isOpen);
  if (!isOpen) return null;

  return (
    <>
      <div className="modal-backdrop" onClick={onClose} aria-hidden="true" />
      <div ref={modalRef} role="dialog" aria-modal="true" aria-labelledby="modal-title">
        <div className="modal-header">
          <h2 id="modal-title">{title}</h2>
          <button onClick={onClose} aria-label="Close dialog">
            <CloseIcon aria-hidden="true" />
          </button>
        </div>
        <div className="modal-content">{children}</div>
      </div>
    </>
  );
};

// Dropdown menu
const Dropdown = ({ trigger, items }: DropdownProps) => {
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  useClickOutside(dropdownRef, () => setIsOpen(false));

  return (
    <div ref={dropdownRef}>
      <button
        onClick={() => setIsOpen(!isOpen)}
        aria-expanded={isOpen}
        aria-haspopup="true"
        aria-controls="dropdown-menu"
      >
        {trigger}
      </button>
      {isOpen && (
        <ul id="dropdown-menu" role="menu">
          {items.map((item, i) => (
            <li key={i} role="none">
              <button role="menuitem" onClick={() => { item.onClick(); setIsOpen(false); }}>
                {item.label}
              </button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
};
```

## Testing Accessibility

```typescript
// Automated testing with jest-axe
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

## Accessibility Checklist

- [ ] Semantic HTML elements used
- [ ] All images have descriptive alt text
- [ ] Forms have proper labels and ARIA
- [ ] Keyboard navigation works completely
- [ ] Focus indicators visible (2px outline)
- [ ] Color contrast meets WCAG AA (4.5:1)
- [ ] ARIA used correctly (semantic HTML first)
- [ ] Skip links implemented
- [ ] Live regions for dynamic content
- [ ] Focus trap in modals
- [ ] Screen reader tested
- [ ] Automated tests pass
- [ ] No color-only indicators
- [ ] Video captions provided
- [ ] Error messages are accessible
- [ ] Interactive elements have labels

**Remember: Accessibility is a fundamental requirement, not optional. Build inclusive applications from the start.**

