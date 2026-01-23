# API & Async Data Examples

## Custom Query Hooks

### Basic Query

```typescript
export const useUsers = () => useQuery({
  queryKey: queryKeys.users,
  queryFn: userService.getAll,
});

// Usage
const { data: users, isLoading, error, refetch } = useUsers();
```

### Query with Parameters

```typescript
export const useUser = (id: string) => useQuery({
  queryKey: queryKeys.user(id),
  queryFn: () => userService.getById(id),
  enabled: !!id,
});
```

### Search with Filters

```typescript
export const useUserSearch = ({ query, page }: { query: string; page: number }) => useQuery({
  queryKey: ['users', 'search', query, page],
  queryFn: () => userService.search({ query, page }),
  enabled: query.length > 0,
  staleTime: 30 * 1000,
});
```

## Mutation Hooks

### Create Mutation

```typescript
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
```

### Update Mutation

```typescript
export const useUpdateUser = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateUserDto }) =>
      userService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.user(id) });
      queryClient.invalidateQueries({ queryKey: queryKeys.users });
    },
  });
};
```

### Delete Mutation

```typescript
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
    mutationFn: ({ id, data }: { id: string; data: UpdateUserDto }) =>
      userService.update(id, data),
    onMutate: async ({ id, data }) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: queryKeys.user(id) });

      // Snapshot previous value
      const previousUser = queryClient.getQueryData<User>(queryKeys.user(id));

      // Optimistically update
      if (previousUser) {
        queryClient.setQueryData<User>(queryKeys.user(id), {
          ...previousUser,
          ...data
        });
      }

      return { previousUser };
    },
    onError: (err, { id }, context) => {
      // Rollback on error
      if (context?.previousUser) {
        queryClient.setQueryData(queryKeys.user(id), context.previousUser);
      }
    },
    onSettled: (_, __, { id }) => {
      // Always refetch after error or success
      queryClient.invalidateQueries({ queryKey: queryKeys.user(id) });
    },
  });
};
```

## Advanced Patterns

### Dependent Queries

```typescript
export const useUserWithProjects = (userId: string) => {
  const userQuery = useQuery({
    queryKey: queryKeys.user(userId),
    queryFn: () => userService.getById(userId),
    enabled: !!userId,
  });

  const projectsQuery = useQuery({
    queryKey: queryKeys.projectsByUser(userId),
    queryFn: () => projectService.getByUserId(userId),
    enabled: !!userQuery.data?.id, // Only fetch when user is loaded
  });

  return {
    user: userQuery.data,
    projects: projectsQuery.data,
    isLoading: userQuery.isLoading || projectsQuery.isLoading,
    error: userQuery.error || projectsQuery.error,
  };
};
```

### Pagination with Placeholder Data

```typescript
export const useUsersPaginated = ({ page, pageSize }: { page: number; pageSize: number }) =>
  useQuery({
    queryKey: ['users', 'paginated', page, pageSize],
    queryFn: () => userService.getPaginated({ page, pageSize }),
    placeholderData: (previousData) => previousData, // Keep showing previous data while loading
  });
```

### Infinite Scroll

```typescript
export const useUsersInfinite = () => useInfiniteQuery({
  queryKey: ['users', 'infinite'],
  queryFn: ({ pageParam = 1 }) => userService.getPaginated({ page: pageParam }),
  getNextPageParam: (lastPage, allPages) =>
    lastPage.hasMore ? allPages.length + 1 : undefined,
  initialPageParam: 1,
});

// Usage
const { data, fetchNextPage, hasNextPage, isFetchingNextPage } = useUsersInfinite();
const allUsers = data?.pages.flatMap(page => page.users) || [];
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
          <button
            onClick={() => handleDelete(user.id)}
            disabled={deleteUser.isPending}
          >
            {t('common.delete')}
          </button>
        </li>
      ))}
    </ul>
  );
};
```

## App Setup

```typescript
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
