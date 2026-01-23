# Error Handling Examples

## Global Error Boundary

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
            <h1 className="text-2xl font-bold text-red-600 mb-4">
              Something went wrong
            </h1>
            <p className="text-gray-700 mb-4">
              We're sorry, something unexpected happened.
            </p>
            <div className="flex gap-2">
              <button
                onClick={() => window.location.reload()}
                className="px-4 py-2 bg-blue-600 text-white rounded"
              >
                Refresh Page
              </button>
              <button
                onClick={this.handleReset}
                className="px-4 py-2 bg-gray-200 rounded"
              >
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

## Feature-Specific Error Boundary

```typescript
export const FeatureErrorBoundary = ({
  children,
  featureName
}: {
  children: ReactNode;
  featureName: string;
}) => {
  const { t } = useTranslation();
  return (
    <ErrorBoundary
      fallback={
        <div className="p-4 bg-red-50 border border-red-200 rounded">
          <h3 className="text-red-800 font-medium">
            {t('errors.feature.title', { feature: featureName })}
          </h3>
          <p className="text-red-600 text-sm mt-1">
            {t('errors.feature.message')}
          </p>
        </div>
      }
      onError={(error) => logError(error, `${featureName} Feature`)}
    >
      {children}
    </ErrorBoundary>
  );
};

// Usage
<FeatureErrorBoundary featureName="Users">
  <UserManagement />
</FeatureErrorBoundary>
```

## Reusable Error Components

### Error Message Component

```typescript
export const ErrorMessage = ({
  title,
  message,
  onRetry,
  className = ''
}: {
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
        <button
          onClick={onRetry}
          className="mt-3 px-4 py-2 bg-red-600 text-white text-sm rounded"
        >
          {t('common.retry')}
        </button>
      )}
    </div>
  );
};
```

### Inline Error for Forms

```typescript
export const InlineError = ({ message, id }: { message?: string; id?: string }) => {
  if (!message) return null;
  return (
    <p id={id} className="text-red-600 text-sm mt-1" role="alert">
      {message}
    </p>
  );
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

  return (
    <div className="fixed bottom-0 left-0 right-0 bg-red-600 text-white p-3 text-center">
      {t('errors.offline')}
    </div>
  );
};
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
});

// Component with error handling
const UserList = () => {
  const { data: users, isLoading, error, refetch } = useUsers();
  const { t } = useTranslation();

  if (isLoading) return <LoadingSpinner />;

  if (error) {
    return (
      <ErrorMessage
        title={t('errors.loadFailed')}
        message={handleApiError(error)}
        onRetry={refetch}
      />
    );
  }

  if (!users?.length) return <EmptyState message={t('users.empty')} />;

  return (
    <ul>
      {users.map((user) => (
        <UserListItem key={user.id} user={user} />
      ))}
    </ul>
  );
};
```

## Mutation with Error Handling

```typescript
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
