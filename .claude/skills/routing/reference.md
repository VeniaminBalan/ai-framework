# Routing Reference

Detailed rules and conventions for routing and navigation using react-router-dom (v6+).

## Route Constants - NO MAGIC STRINGS

**NEVER use magic strings**. Always centralize routes:

```typescript
// constants/routes.ts
export const ROUTES = {
  HOME: '/',
  LOGIN: '/login',
  DASHBOARD: '/dashboard',
  USERS: '/users',
  USER_DETAIL: '/users/:id',
  USER_EDIT: '/users/:id/edit',
  PROJECTS: '/projects',
  PROJECT_DETAIL: '/projects/:id',
  SETTINGS: '/settings',
  NOT_FOUND: '*',
  UNAUTHORIZED: '/unauthorized',
} as const;

// Helper functions for parameterized routes
export const getRoute = {
  userDetail: (id: string) => `/users/${id}`,
  userEdit: (id: string) => `/users/${id}/edit`,
  projectDetail: (id: string) => `/projects/${id}`,
};

export type RouteParams = {
  userId?: string;
  projectId?: string;
  id?: string;
};
```

## Layout Routes

Use layout routes to share common UI:

```typescript
import { Outlet } from 'react-router-dom';

const AppLayout = () => (
  <div>
    <Header />
    <Sidebar />
    <main><Outlet /></main> {/* Child routes render here */}
    <Footer />
  </div>
);
```

## Protected Routes

```typescript
import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '@/hooks/useAuth';
import { ROUTES } from '@/constants/routes';

export const ProtectedRoute = ({ allowedRoles }: { allowedRoles?: string[] }) => {
  const { user, isAuthenticated } = useAuth();

  if (!isAuthenticated) {
    return <Navigate to={ROUTES.LOGIN} replace />;
  }

  if (allowedRoles && !allowedRoles.includes(user.role)) {
    return <Navigate to={ROUTES.UNAUTHORIZED} replace />;
  }

  return <Outlet />;
};
```

## URL Parameters

### Route Parameters
```typescript
const { id } = useParams<RouteParams>();
```

### Search Parameters (Query Strings)
```typescript
const [searchParams, setSearchParams] = useSearchParams();

// Read
const search = searchParams.get('search') || '';
const page = parseInt(searchParams.get('page') || '1');

// Update
setSearchParams(prev => {
  prev.set('search', value);
  prev.set('page', '1');
  return prev;
});
```

## Navigation

### Link Component
```typescript
import { Link, NavLink } from 'react-router-dom';
import { ROUTES, getRoute } from '@/constants/routes';

<Link to={ROUTES.USERS}>Users</Link>
<Link to={getRoute.userDetail(user.id)}>View User</Link>

<NavLink
  to={ROUTES.DASHBOARD}
  className={({ isActive }) => isActive ? 'nav-active' : 'nav-link'}
>
  Dashboard
</NavLink>
```

### Programmatic Navigation
```typescript
const navigate = useNavigate();

navigate(getRoute.userDetail(user.id));
navigate(-1); // Go back
navigate(ROUTES.LOGIN, { replace: true });
```

## Best Practices

1. **Centralize routes** - Use constants, never magic strings
2. **Layout routes** - Share common UI
3. **Route guards** - Protect authenticated routes
4. **Lazy load** - Split code for performance
5. **Handle 404** - Clear not found pages
6. **URL state** - Store filters/pagination in URL
7. **Type params** - Use TypeScript
8. **Loading states** - Show spinners during lazy loads
9. **Breadcrumbs** - Help navigation (when needed)
10. **Scroll restoration** - Reset scroll on route change
