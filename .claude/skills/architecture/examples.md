# Architecture Examples

## Code Organization Pattern

### Good: Single responsibility, clear naming

```typescript
export const UserProfile = ({ userId }: { userId: string }) => {
  const { data: user, isLoading } = useUser(userId);
  const { t } = useTranslation();

  if (isLoading) return <LoadingSpinner />;
  if (!user) return <NotFound />;

  return (
    <div>
      <UserAvatar src={user.avatar} />
      <UserInfo user={user} />
      <UserActions userId={userId} />
    </div>
  );
};
```

### Bad: Too much responsibility, hard to maintain

```typescript
export const UserProfile = ({ userId }: { userId: string }) => {
  // 300 lines of mixed logic and JSX
};
```

## Feature Folder Structure

```
components/
├── employee/      # Employee-specific components
├── pm/            # Project Manager components
├── gm/            # General Manager components
├── admin/         # Admin components
├── time-tracking/ # Time tracking feature
├── workload/      # Workload feature
├── common/        # Shared/reusable components
├── shared/        # Cross-feature shared components
└── auth/          # Authentication components

hooks/
├── useFormState.ts    # Form state management hook
├── useModal.ts        # Modal state management hook
├── useDebounce.ts     # Debounce hook
└── useAuth.ts         # Authentication hook
```

## File Naming Convention

| Type | Pattern | Example |
|------|---------|---------|
| Component | PascalCase | `CreateTaskModal.tsx` |
| Types | kebab-case | `time-tracking.ts` |
| API | kebab-case | `overtime-requests.ts` |
| Hooks | camelCase with use prefix | `useAuth.ts` |
| Tests | ComponentName.test.tsx | `CreateTaskModal.test.tsx` |

## TypeScript Configuration Example

```json
{
  "compilerOptions": {
    "strict": true,
    "noImplicitAny": true,
    "strictNullChecks": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true
  }
}
```

## Common Mistakes to Avoid

### Prop drilling beyond 2-3 levels

```typescript
// Wrong - prop drilling
<Grandparent>
  <Parent user={user}>
    <Child user={user}>
      <GrandChild user={user} />
    </Child>
  </Parent>
</Grandparent>

// Correct - use context
<UserProvider value={user}>
  <Grandparent>
    <Parent>
      <Child>
        <GrandChild /> {/* Uses useUser() hook */}
      </Child>
    </Parent>
  </Grandparent>
</UserProvider>
```

### Mixed concerns in a single file

```typescript
// Wrong - API call, state management, and UI in one component
const UserPage = () => {
  const [users, setUsers] = useState([]);

  useEffect(() => {
    fetch('/api/users')
      .then(res => res.json())
      .then(setUsers);
  }, []);

  return <ul>{users.map(u => <li>{u.name}</li>)}</ul>;
};

// Correct - separated concerns
// services/userService.ts
export const userService = {
  getAll: async () => {
    const res = await fetch('/api/users');
    return res.json();
  }
};

// hooks/useUsers.ts
export const useUsers = () => {
  return useQuery({
    queryKey: ['users'],
    queryFn: userService.getAll
  });
};

// components/UserPage.tsx
const UserPage = () => {
  const { data: users, isLoading } = useUsers();
  if (isLoading) return <Loading />;
  return <UserList users={users} />;
};
```
