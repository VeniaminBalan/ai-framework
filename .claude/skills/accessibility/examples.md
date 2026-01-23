# Accessibility Examples

## Semantic HTML Structure

```typescript
// Good - semantic elements
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

// Bad - divs for everything
<div>
  <div>
    <div>Article Title</div>
  </div>
</div>
```

## Accessible Form Field

```typescript
export const FormField = ({
  label,
  id,
  error,
  required,
  helpText,
  type = 'text',
  ...props
}: FormFieldProps) => (
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
      {...props}
    />
    {error && <span id={`${id}-error`} role="alert">{error}</span>}
  </div>
);
```

## Focus Trap Hook

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
```

## Accessible Modal

```typescript
const Modal = ({ isOpen, onClose, title, children }: ModalProps) => {
  const modalRef = useFocusTrap(isOpen);

  // Handle Escape key
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => e.key === 'Escape' && onClose();
    if (isOpen) {
      document.addEventListener('keydown', handleEscape);
      return () => document.removeEventListener('keydown', handleEscape);
    }
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  return (
    <>
      <div className="modal-backdrop" onClick={onClose} aria-hidden="true" />
      <div
        ref={modalRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby="modal-title"
      >
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
```

## Accessible Dropdown Menu

```typescript
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
              <button
                role="menuitem"
                onClick={() => {
                  item.onClick();
                  setIsOpen(false);
                }}
              >
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

## Skip Link

```typescript
export const SkipLink = () => (
  <a href="#main-content" className="skip-link">
    Skip to main content
  </a>
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

// Usage in layout
<Layout>
  <SkipLink />
  <Header />
  <main id="main-content" tabIndex={-1}>{/* content */}</main>
</Layout>
```

## Visually Hidden (Screen Reader Only)

```typescript
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
```

## Descriptive Links

```typescript
// Bad - not descriptive
<a href="/report.pdf">Click here</a>

// Good - descriptive
<a href="/report.pdf">Download 2024 Annual Report (PDF, 2MB)</a>
```

## Automated Accessibility Testing

```typescript
import { render } from '@testing-library/react';
import { axe, toHaveNoViolations } from 'jest-axe';

expect.extend(toHaveNoViolations);

describe('Button', () => {
  it('should be accessible', async () => {
    const { container } = render(<Button>Click me</Button>);
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});

describe('Form', () => {
  it('should have no accessibility violations', async () => {
    const { container } = render(
      <form>
        <label htmlFor="email">Email</label>
        <input id="email" type="email" />
        <button type="submit">Submit</button>
      </form>
    );
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
```
