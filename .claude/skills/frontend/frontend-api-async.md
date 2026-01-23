# Frontend API & Async Data Handling

## Overview
API integration using Axios and TanStack Query (React Query) for data fetching, caching, and state management.

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
  create: async (data: CreateUserDto): Promise<User> => {
    const { data: user } = await apiClient.post<User>('/users', data);
    return user;
  },
  update: async (id: string, data: UpdateUserDto): Promise<User> => {
    const { data: user } = await apiClient.put<User>(`/users/${id}`, data);
    return user;
  },
  delete: async (id: string): Promise<void> => {
    await apiClient.delete(`/users/${id}`);
  },
  search: async (params: { query: string; page: number }): Promise<User[]> => {
    const { data } = await apiClient.get<User[]>('/users/search', { params });
    return data;
  },
};
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

// App.tsx
import { QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { queryClient } from './lib/queryClient';

export const App = () => (
  <QueryClientProvider client={queryClient}>
    <AppRoutes />
    <ReactQueryDevtools initialIsOpen={false} />
  </QueryClientProvider>
);
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

## Custom Query Hooks

```typescript
// Basic query
export const useUsers = () => useQuery({
  queryKey: queryKeys.users,
  queryFn: userService.getAll,
});

// Usage: const { data: users, isLoading, error, refetch } = useUsers();

// Query with parameters
export const useUser = (id: string) => useQuery({
  queryKey: queryKeys.user(id),
  queryFn: () => userService.getById(id),
  enabled: !!id,
});

// Search with filters
export const useUserSearch = ({ query, page }: { query: string; page: number }) => useQuery({
  queryKey: ['users', 'search', query, page],
  queryFn: () => userService.search({ query, page }),
  enabled: query.length > 0,
  staleTime: 30 * 1000,
});
```

## Mutation Hooks

```typescript
// Create
export const useCreateUser = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateUserDto) => userService.create(data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.users }),
  });
};

// Usage
const createUser = useCreateUser();
createUser.mutate(userData, {
  onSuccess: (data) => toast.success('Created'),
  onError: (error) => toast.error(handleApiError(error)),
});

// Update
export const useUpdateUser = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateUserDto }) => userService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.user(id) });
      queryClient.invalidateQueries({ queryKey: queryKeys.users });
    },
  });
};

// Delete
export const useDeleteUser = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => userService.delete(id),
    onSuccess: (_, id) => {
      queryClient.removeQueries({ queryKey: queryKeys.user(id) });
      queryClient.invalidateQueries({ queryKey: queryKeys.users });
    },
  });
};
```

## Optimistic Updates

```typescript
export const useUpdateUserOptimistic = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateUserDto }) => userService.update(id, data),
    onMutate: async ({ id, data }) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.user(id) });
      const previousUser = queryClient.getQueryData<User>(queryKeys.user(id));
      if (previousUser) {
        queryClient.setQueryData<User>(queryKeys.user(id), { ...previousUser, ...data });
      }
      return { previousUser };
    },
    onError: (err, { id }, context) => {
      if (context?.previousUser) {
        queryClient.setQueryData(queryKeys.user(id), context.previousUser);
      }
    },
    onSettled: (_, __, { id }) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.user(id) });
    },
  });
};
```

## Advanced Patterns

```typescript
// Dependent queries - fetch projects only after user loads
export const useUserWithProjects = (userId: string) => {
  const userQuery = useQuery({
    queryKey: queryKeys.user(userId),
    queryFn: () => userService.getById(userId),
    enabled: !!userId,
  });

  const projectsQuery = useQuery({
    queryKey: queryKeys.projectsByUser(userId),
    queryFn: () => projectService.getByUserId(userId),
    enabled: !!userQuery.data?.id,
  });

  return {
    user: userQuery.data,
    projects: projectsQuery.data,
    isLoading: userQuery.isLoading || projectsQuery.isLoading,
    error: userQuery.error || projectsQuery.error,
  };
};

// Pagination with placeholder data
export const useUsersPaginated = ({ page, pageSize }: { page: number; pageSize: number }) => 
  useQuery({
    queryKey: ['users', 'paginated', page, pageSize],
    queryFn: () => userService.getPaginated({ page, pageSize }),
    placeholderData: (previousData) => previousData,
  });

// Infinite scroll
export const useUsersInfinite = () => useInfiniteQuery({
  queryKey: ['users', 'infinite'],
  queryFn: ({ pageParam = 1 }) => userService.getPaginated({ page: pageParam }),
  getNextPageParam: (lastPage, allPages) => lastPage.hasMore ? allPages.length + 1 : undefined,
  initialPageParam: 1,
});

// Usage: const { data, fetchNextPage, hasNextPage } = useUsersInfinite();
// const allUsers = data?.pages.flatMap(page => page.users) || [];
```

## Error Handling

```typescript
// lib/errorHandler.ts
import { AxiosError } from 'axios';

export class AppError extends Error {
  constructor(message: string, public code?: string, public statusCode?: number) {
    super(message);
    this.name = 'AppError';
  }
}

export const handleApiError = (error: unknown): string => {
  if (error instanceof AxiosError) {
    if (error.response) return error.response.data?.message || error.message;
    if (error.request) return 'No response from server. Check your connection.';
    return error.message;
  }
  if (error instanceof AppError) return error.message;
  if (error instanceof Error) return error.message;
  return 'An unexpected error occurred';
};
```

## Component Integration

```typescript
export const UserList = () => {
  const { t } = useTranslation();
  const { data: users, isLoading, error, refetch } = useUsers();
  const deleteUser = useDeleteUser();

  const handleDelete = (id: string) => {
    if (confirm(t('users.confirm.delete'))) {
      deleteUser.mutate(id, {
        onSuccess: () => toast.success(t('users.deleted')),
        onError: (error) => toast.error(handleApiError(error)),
      });
    }
  };

  if (isLoading) return <LoadingSpinner />;
  if (error) return (
    <ErrorMessage>
      {handleApiError(error)}
      <button onClick={() => refetch()}>{t('common.retry')}</button>
    </ErrorMessage>
  );
  if (!users?.length) return <EmptyState message={t('users.empty')} />;

  return (
    <ul>
      {users.map((user) => (
        <li key={user.id}>
          {user.name}
          <button onClick={() => handleDelete(user.id)} disabled={deleteUser.isPending}>
            {t('common.delete')}
          </button>
        </li>
      ))}
    </ul>
  );
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
8. **Conditional queries** - Use enabled flag
9. **Placeholder data** - Keep previous data while refetching
10. **DevTools in development** - Use React Query DevTools

## Checklist
- [ ] Axios client configured with interceptors
- [ ] Auth token in request interceptor
- [ ] Error handling in response interceptor
- [ ] API calls in service layer (not components)
- [ ] QueryClientProvider wraps app
- [ ] Query keys centralized and typed
- [ ] Custom hooks for all API calls
- [ ] Mutations invalidate relevant queries
- [ ] Loading/error states handled
- [ ] User-friendly error messages
- [ ] Optimistic updates where appropriate
- [ ] React Query DevTools enabled in dev
