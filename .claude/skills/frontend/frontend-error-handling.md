# Frontend Error Handling

## Overview
Comprehensive error handling strategies for React applications, including API errors, UI errors, and graceful degradation.

## Error Types
1. API/Network Errors
2. React Component Errors (caught by Error Boundaries)
3. Form Validation Errors
4. Authentication/Authorization Errors
5. Business Logic Errors

## Custom Error Classes

```typescript
// lib/errors.ts
export class AppError extends Error {
  constructor(
    message: string,
    public code?: string,
    public statusCode?: number,
    public details?: Record<string, any>
  ) {
    super(message);
    this.name = 'AppError';
    Object.setPrototypeOf(this, AppError.prototype);
  }
}

export class ApiError extends AppError {
  constructor(message: string, statusCode: number, code?: string, details?: Record<string, any>) {
    super(message, code, statusCode, details);
    this.name = 'ApiError';
  }
}

export class ValidationError extends AppError {
  constructor(message: string, public fields: Record<string, string[]>) {
    super(message, 'VALIDATION_ERROR', 400);
    this.name = 'ValidationError';
  }
}

export class AuthenticationError extends AppError {
  constructor(message = 'Authentication required') {
    super(message, 'AUTH_ERROR', 401);
    this.name = 'AuthenticationError';
  }
}

export class AuthorizationError extends AppError {
  constructor(message = 'Access denied') {
    super(message, 'FORBIDDEN', 403);
    this.name = 'AuthorizationError';
  }
}

export class NotFoundError extends AppError {
  constructor(message = 'Resource not found') {
    super(message, 'NOT_FOUND', 404);
    this.name = 'NotFoundError';
  }
}
```

## Error Handler Utility

```typescript
// lib/errorHandler.ts
import { AxiosError } from 'axios';
import { AppError } from './errors';

export const handleApiError = (error: unknown): string => {
  if (error instanceof AxiosError) {
    if (error.response) {
      const { status, data } = error.response;
      const message = data?.message || data?.error || data?.errors?.[0]?.message || error.message;
      return message || getDefaultErrorMessage(status);
    }
    if (error.request) return 'No response from server. Check your connection.';
    return error.message || 'Request error occurred.';
  }
  if (error instanceof AppError) return error.message;
  if (error instanceof Error) return error.message;
  return 'An unexpected error occurred. Please try again.';
};

const getDefaultErrorMessage = (status: number): string => {
  const messages: Record<number, string> = {
    400: 'Invalid request. Check your input.',
    401: 'Please log in to continue.',
    403: 'You don\'t have permission for this action.',
    404: 'Resource not found.',
    408: 'Request timeout. Try again.',
    409: 'Conflict occurred. Resource may be modified.',
    422: 'Validation failed. Check your input.',
    429: 'Too many requests. Slow down.',
    500: 'Server error. Try again later.',
    502: 'Bad gateway. Server temporarily unavailable.',
    503: 'Service unavailable. Try again later.',
    504: 'Gateway timeout. Server took too long.',
  };
  return messages[status] || 'An error occurred. Please try again.';
};

export const logError = (error: unknown, context?: string) => {
  if (import.meta.env.DEV) {
    console.error(`Error ${context ? `in ${context}` : ''}:`, error);
  }
  // In production: Sentry.captureException(error);
};

export const formatValidationErrors = (errors: Record<string, string[]>): string =>
  Object.entries(errors)
    .map(([field, messages]) => `${field}: ${messages.join(', ')}`)
    .join('\n');
```

## Error Boundaries

```typescript
// components/ErrorBoundary.tsx
import { Component, ReactNode, ErrorInfo } from 'react';
import { logError } from '@/lib/errorHandler';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
  onError?: (error: Error, errorInfo: ErrorInfo) => void;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

export class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false, error: null };

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    logError(error, 'ErrorBoundary');
    console.error('Error details:', errorInfo.componentStack);
    this.props.onError?.(error, errorInfo);
  }

  handleReset = () => this.setState({ hasError: false, error: null });

  render() {
    if (this.state.hasError) {
      if (this.props.fallback) return this.props.fallback;

      return (
        <div className="flex items-center justify-center min-h-screen p-4">
          <div className="max-w-md bg-white rounded-lg shadow-lg p-6">
            <h1 className="text-2xl font-bold text-red-600 mb-4">Something went wrong</h1>
            <p className="text-gray-700 mb-4">
              We're sorry, something unexpected happened. Please try refreshing.
            </p>
            {import.meta.env.DEV && this.state.error && (
              <details className="mb-4">
                <summary className="cursor-pointer text-sm text-gray-600">Error details (dev)</summary>
                <pre className="mt-2 text-xs bg-gray-100 p-2 rounded overflow-auto">
                  {this.state.error.toString()}
                </pre>
              </details>
            )}
            <div className="flex gap-2">
              <button onClick={() => window.location.reload()} className="px-4 py-2 bg-blue-600 text-white rounded">
                Refresh Page
              </button>
              <button onClick={this.handleReset} className="px-4 py-2 bg-gray-200 rounded">
                Try Again
              </button>
            </div>
          </div>
        </div>
      );
    }
    return this.props.children;
  }
}
```

// Feature-specific error boundary
export const FeatureErrorBoundary = ({ children, featureName }: { children: ReactNode; featureName: string }) => {
  const { t } = useTranslation();
  return (
    <ErrorBoundary
      fallback={
        <div className="p-4 bg-red-50 border border-red-200 rounded">
          <h3 className="text-red-800 font-medium">{t('errors.feature.title', { feature: featureName })}</h3>
          <p className="text-red-600 text-sm mt-1">{t('errors.feature.message')}</p>
        </div>
      }
      onError={(error) => logError(error, `${featureName} Feature`)}
    >
      {children}
    </ErrorBoundary>
  );
};

// Usage: <FeatureErrorBoundary featureName="Users"><UserManagement /></FeatureErrorBoundary>
```

## API Error Handling with React Query

```typescript
// Query with retry logic
export const useUsers = () => useQuery({
  queryKey: queryKeys.users,
  queryFn: userService.getAll,
  retry: (failureCount, error) => {
    if (error instanceof AxiosError && error.response?.status < 500) return false;
    return failureCount < 2;
  },
  onError: (error) => logError(error, 'useUsers'),
});

// Component with error handling
const UserList = () => {
  const { data: users, isLoading, error, refetch } = useUsers();
  const { t } = useTranslation();

  if (isLoading) return <LoadingSpinner />;
  if (error) return <ErrorMessage title={t('errors.loadFailed')} message={handleApiError(error)} onRetry={refetch} />;
  if (!users?.length) return <EmptyState message={t('users.empty')} />;

  return (
    <ul>{users.map((user) => <UserListItem key={user.id} user={user} />)}</ul>
  );
};
```

// Mutation with error handling
export const useCreateUser = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: userService.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.users });
      toast.success('User created');
    },
    onError: (error) => {
      toast.error(handleApiError(error));
      logError(error, 'useCreateUser');
    },
  });
};

// Component
const CreateUserForm = () => {
  const createUser = useCreateUser();
  const { handleSubmit } = useForm<CreateUserInput>();

  return (
    <form onSubmit={handleSubmit((data) => createUser.mutate(data))}>
      {/* form fields */}
      {createUser.isError && (
        <div className="text-red-600 text-sm" role="alert">
          {handleApiError(createUser.error)}
        </div>
      )}
      <button type="submit" disabled={createUser.isPending}>
        {createUser.isPending ? 'Creating...' : 'Create User'}
      </button>
    </form>
  );
};
```

## Reusable Error Components

```typescript
// Error message component
export const ErrorMessage = ({ title, message, onRetry, className = '' }: {
  title?: string;
  message: string;
  onRetry?: () => void;
  className?: string;
}) => {
  const { t } = useTranslation();
  return (
    <div className={`p-4 bg-red-50 border border-red-200 rounded ${className}`} role="alert">
      {title && <h3 className="text-red-800 font-medium mb-2">{title}</h3>}
      <p className="text-red-700 text-sm">{message}</p>
      {onRetry && (
        <button onClick={onRetry} className="mt-3 px-4 py-2 bg-red-600 text-white text-sm rounded">
          {t('common.retry')}
        </button>
      )}
    </div>
  );
};

// Inline error for forms
export const InlineError = ({ message, id }: { message?: string; id?: string }) => {
  if (!message) return null;
  return <p id={id} className="text-red-600 text-sm mt-1" role="alert">{message}</p>;
};

// Usage
<input
  id="email"
  aria-invalid={!!errors.email}
  aria-describedby={errors.email ? 'email-error' : undefined}
  {...register('email')}
/>
<InlineError id="email-error" message={errors.email?.message} />
```

## Toast Notifications

```typescript
// Setup
import { Toaster } from 'react-hot-toast';

export const App = () => (
  <>
    <AppRoutes />
    <Toaster position="top-right" toastOptions={{ duration: 4000 }} />
  </>
);

// Usage
import { toast } from 'react-hot-toast';

toast.success('Operation completed');
toast.error('Something went wrong');
toast.promise(saveData(), {
  loading: 'Saving...',
  success: 'Saved',
  error: (err) => handleApiError(err),
});
```

## Network Status Detection

```typescript
export const useOnlineStatus = () => {
  const [isOnline, setIsOnline] = useState(navigator.onLine);
  useEffect(() => {
    const handleOnline = () => setIsOnline(true);
    const handleOffline = () => setIsOnline(false);
    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);
    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, []);
  return isOnline;
};

export const NetworkStatus = () => {
  const isOnline = useOnlineStatus();
  const { t } = useTranslation();
  if (isOnline) return null;
  return <div className="fixed bottom-0 left-0 right-0 bg-red-600 text-white p-3 text-center">{t('errors.offline')}</div>;
};
```

## Best Practices

1. **Sanitize errors** - Never expose raw errors to users
2. **Log appropriately** - Use error tracking (Sentry) in production
3. **Provide context** - Include where error occurred
4. **Offer recovery** - Show retry buttons, navigation
5. **Use error boundaries** - Catch component errors
6. **Handle specific cases** - Different messages for 401, 403, 404, 500
7. **Show loading states** - Before error states
8. **Validate early** - Catch form errors before submission
9. **Graceful degradation** - Fallbacks for failed features
10. **Test error states** - Ensure error UI works

## Checklist
- [ ] Centralized error handling utility
- [ ] Custom error classes (AppError, ApiError, etc.)
- [ ] Global error boundary
- [ ] Feature-specific error boundaries
- [ ] API errors caught and displayed
- [ ] User-friendly messages (no stack traces)
- [ ] Loading states before errors
- [ ] Retry mechanisms
- [ ] Network status detection
- [ ] Toast notifications for mutations
- [ ] Inline errors for forms
- [ ] Error logging configured
- [ ] 404 and error pages
- [ ] Graceful degradation implemented
