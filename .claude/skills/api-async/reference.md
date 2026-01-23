# API & Async Data Reference

Detailed rules and conventions for API integration using Axios and TanStack Query.

## Required Libraries

- **Axios**: HTTP client
- **@tanstack/react-query**: Async state management and caching

## Axios Setup

```typescript
// lib/api/axios.ts
import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  timeout: 10000,
  headers: { 'Content-Type': 'application/json' },
});

// Request interceptor - add auth token
apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = localStorage.getItem('access_token');
  if (token && config.headers) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Response interceptor - handle errors
apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    if (error.response?.status === 401) window.location.href = '/login';
    if (error.response?.status === 403) console.error('Access forbidden');
    if (error.response?.status >= 500) console.error('Server error');
    return Promise.reject(error);
  }
);
```

## React Query Setup

```typescript
// lib/queryClient.ts
import { QueryClient } from '@tanstack/react-query';

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000, // 5 minutes
      gcTime: 10 * 60 * 1000, // 10 minutes
      retry: 1,
      refetchOnWindowFocus: false,
    },
    mutations: { retry: 0 },
  },
});
```

## Centralized Query Keys

```typescript
// lib/api/queryKeys.ts
export const queryKeys = {
  users: ['users'] as const,
  user: (id: string) => ['users', id] as const,
  userByEmail: (email: string) => ['users', 'email', email] as const,
  projects: ['projects'] as const,
  project: (id: string) => ['projects', id] as const,
  projectsByUser: (userId: string) => ['projects', 'user', userId] as const,
  tasks: ['tasks'] as const,
  task: (id: string) => ['tasks', id] as const,
  tasksByProject: (projectId: string) => ['tasks', 'project', projectId] as const,
} as const;
```

## API Service Layer

```typescript
// services/userService.ts
import { apiClient } from '@/lib/api/axios';
import { User, CreateUserDto, UpdateUserDto } from '@/types/user';

export const userService = {
  getAll: async (): Promise<User[]> => {
    const { data } = await apiClient.get<User[]>('/users');
    return data;
  },
  getById: async (id: string): Promise<User> => {
    const { data } = await apiClient.get<User>(`/users/${id}`);
    return data;
  },
  create: async (dto: CreateUserDto): Promise<User> => {
    const { data } = await apiClient.post<User>('/users', dto);
    return data;
  },
  update: async (id: string, dto: UpdateUserDto): Promise<User> => {
    const { data } = await apiClient.put<User>(`/users/${id}`, dto);
    return data;
  },
  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/users/${id}`);
  },
};
```

## Error Handling

```typescript
// lib/errorHandler.ts
import { AxiosError } from 'axios';

export const handleApiError = (error: unknown): string => {
  if (error instanceof AxiosError) {
    if (error.response) return error.response.data?.message || error.message;
    if (error.request) return 'No response from server. Check your connection.';
    return error.message;
  }
  if (error instanceof Error) return error.message;
  return 'An unexpected error occurred';
};
```

## Best Practices

1. **Centralize query keys** - Single source of truth
2. **Appropriate stale times** - Balance freshness and performance
3. **User-friendly errors** - Show clear messages
4. **Leverage built-in states** - isLoading, isError, data
5. **Intelligent invalidation** - Refetch only what's needed
6. **Optimistic updates** - Better perceived performance
7. **Loading feedback** - Always show loading states
8. **Conditional queries** - Use `enabled` flag
9. **Placeholder data** - Keep previous data while refetching
10. **DevTools in development** - Use React Query DevTools
