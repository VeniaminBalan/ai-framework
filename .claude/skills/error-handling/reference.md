# Error Handling Reference

Detailed rules and conventions for comprehensive error handling in React applications.

## Error Types

1. **API/Network Errors** - Failed HTTP requests
2. **React Component Errors** - Caught by Error Boundaries
3. **Form Validation Errors** - User input issues
4. **Authentication/Authorization Errors** - 401/403
5. **Business Logic Errors** - Domain-specific failures

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
  constructor(message: string, statusCode: number, code?: string) {
    super(message, code, statusCode);
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
      const message = data?.message || data?.error || error.message;
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
    403: "You don't have permission for this action.",
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
