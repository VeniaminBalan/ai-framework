# Frontend Routing & Navigation

## Overview
Routing and navigation using react-router-dom with type-safe route management.

**Required:** react-router-dom (v6+)

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

## Basic Router Setup

```typescript
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { ROUTES } from '@/constants/routes';

export const App = () => (
  <BrowserRouter>
    <Routes>
      <Route path={ROUTES.HOME} element={<HomePage />} />
      <Route path={ROUTES.LOGIN} element={<LoginPage />} />
      <Route path={ROUTES.USERS} element={<UsersPage />} />
      <Route path={ROUTES.USER_DETAIL} element={<UserDetailPage />} />
      <Route path={ROUTES.NOT_FOUND} element={<NotFoundPage />} />
    </Routes>
  </BrowserRouter>
);
```

## Layout Routes

```typescript
import { Outlet } from 'react-router-dom';

// Layout component
const AppLayout = () => (
  <div>
    <Header />
    <Sidebar />
    <main><Outlet /></main> {/* Child routes render here */}
    <Footer />
  </div>
);

const DashboardLayout = () => (
  <div>
    <DashboardNav />
    <Outlet />
  </div>
);

export const App = () => (
  <BrowserRouter>
    <Routes>
      {/* Public routes */}
      <Route path={ROUTES.LOGIN} element={<LoginPage />} />

      {/* Routes with layout */}
      <Route element={<AppLayout />}>
        <Route path={ROUTES.HOME} element={<HomePage />} />
        <Route path={ROUTES.USERS} element={<UsersPage />} />
        
        {/* Nested dashboard */}
        <Route path={ROUTES.DASHBOARD} element={<DashboardLayout />}>
          <Route index element={<DashboardOverview />} />
          <Route path="analytics" element={<DashboardAnalytics />} />
        </Route>
      </Route>

      <Route path={ROUTES.NOT_FOUND} element={<NotFoundPage />} />
    </Routes>
  </BrowserRouter>
);
```

## Protected Routes

```typescript
import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '@/hooks/useAuth';
import { ROUTES } from '@/constants/routes';

export const ProtectedRoute = ({ allowedRoles }: { allowedRoles?: string[] }) => {
  const { user, isAuthenticated } = useAuth();

  if (!isAuthenticated) return <Navigate to={ROUTES.LOGIN} replace />;
  if (allowedRoles && !allowedRoles.includes(user.role)) return <Navigate to={ROUTES.UNAUTHORIZED} replace />;

  return <Outlet />;
};

// Usage
<Route element={<ProtectedRoute />}>
  <Route path={ROUTES.DASHBOARD} element={<DashboardPage />} />
</Route>

<Route element={<ProtectedRoute allowedRoles={['admin']} />}>
  <Route path="/admin" element={<AdminPage />} />
</Route>
```

## Navigation

```typescript
import { Link, NavLink, useNavigate } from 'react-router-dom';
import { ROUTES, getRoute } from '@/constants/routes';

// Link - static and dynamic routes
<Link to={ROUTES.USERS}>Users</Link>
<Link to={getRoute.userDetail(user.id)}>View User</Link>
<Link to={ROUTES.LOGIN} state={{ from: location }}>Login</Link>

// NavLink - with active styling
<NavLink to={ROUTES.DASHBOARD} className={({ isActive }) => isActive ? 'nav-active' : 'nav-link'}>
  Dashboard
</NavLink>

<NavLink to={ROUTES.USERS} style={({ isActive }) => ({ color: isActive ? 'blue' : 'black' })}>
  Users
</NavLink>

// Programmatic navigation
const MyComponent = () => {
  const navigate = useNavigate();

  const handleSubmit = async (data: FormData) => {
    const user = await createUser(data);
    navigate(getRoute.userDetail(user.id));
  };

  const handleCancel = () => navigate(-1); // Go back
  const handleLogin = () => navigate(ROUTES.LOGIN, { replace: true });

  return <button onClick={handleSubmit}>Submit</button>;
};
```

## URL Parameters

```typescript
import { useParams, useSearchParams } from 'react-router-dom';
import { RouteParams } from '@/constants/routes';

// Route parameters
const UserDetailPage = () => {
  const { id } = useParams<RouteParams>();
  const { data: user, isLoading } = useUser(id!);

  if (isLoading) return <LoadingSpinner />;
  if (!user) return <NotFound />;
  return <UserProfile user={user} />;
};

// Search parameters (query strings)
const UsersPage = () => {
  const [searchParams, setSearchParams] = useSearchParams();

  // Read
  const search = searchParams.get('search') || '';
  const page = parseInt(searchParams.get('page') || '1');
  const sort = searchParams.get('sort') || 'name';

  // Update single parameter
  const updateSearch = (value: string) => {
    setSearchParams(prev => {
      if (value) prev.set('search', value);
      else prev.delete('search');
      prev.set('page', '1');
      return prev;
    });
  };

  // Update multiple
  const updateFilters = (filters: Record<string, string>) => setSearchParams(filters);

  return (
    <div>
      <input value={search} onChange={(e) => updateSearch(e.target.value)} />
      <UserList filters={{ search, page, sort }} />
    </div>
  );
};
```

## Lazy Loading & Code Splitting

```typescript
import { lazy, Suspense } from 'react';
import { Routes, Route } from 'react-router-dom';

const UsersPage = lazy(() => import('@/pages/UsersPage'));
const DashboardPage = lazy(() => import('@/pages/DashboardPage'));

export const App = () => (
  <BrowserRouter>
    <Suspense fallback={<LoadingSpinner />}>
      <Routes>
        <Route path={ROUTES.HOME} element={<HomePage />} />
        <Route path={ROUTES.USERS} element={<UsersPage />} />
        <Route path={ROUTES.DASHBOARD} element={<DashboardPage />} />
      </Routes>
    </Suspense>
  </BrowserRouter>
);
```

## Location & Navigation State

```typescript
import { useLocation, useNavigate } from 'react-router-dom';

const LoginPage = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const from = location.state?.from?.pathname || '/dashboard';

  const handleLogin = async (credentials: Credentials) => {
    await login(credentials);
    navigate(from, { replace: true });
  };

  return <LoginForm onSubmit={handleLogin} />;
};

// Redirecting with state
if (!isAuthenticated) {
  navigate('/login', { state: { from: location }, replace: true });
}
```

## Breadcrumbs

```typescript
import { Link, useLocation } from 'react-router-dom';
import { ROUTES } from '@/constants/routes';

const routeNames: Record<string, string> = {
  [ROUTES.HOME]: 'Home',
  [ROUTES.USERS]: 'Users',
  [ROUTES.DASHBOARD]: 'Dashboard',
};

export const Breadcrumbs = () => {
  const location = useLocation();
  const pathnames = location.pathname.split('/').filter(x => x);

  return (
    <nav aria-label="Breadcrumb">
      <ol>
        <li><Link to={ROUTES.HOME}>Home</Link></li>
        {pathnames.map((value, index) => {
          const to = `/${pathnames.slice(0, index + 1).join('/')}`;
          const isLast = index === pathnames.length - 1;
          const name = routeNames[to] || value;
          return (
            <li key={to}>
              {isLast ? <span aria-current="page">{name}</span> : <Link to={to}>{name}</Link>}
            </li>
          );
        })}
      </ol>
    </nav>
  );
};
```

## 404 & Error Pages

```typescript
import { Link } from 'react-router-dom';
import { ROUTES } from '@/constants/routes';

export const NotFoundPage = () => {
  const { t } = useTranslation();
  return (
    <div>
      <h1>{t('errors.404.title')}</h1>
      <p>{t('errors.404.message')}</p>
      <Link to={ROUTES.HOME}>{t('errors.404.goHome')}</Link>
    </div>
  );
};

// Configure in routes
<Route path={ROUTES.NOT_FOUND} element={<NotFoundPage />} />
```

## Navigation Guards

```typescript
import { useEffect } from 'react';
import { useBlocker } from 'react-router-dom';

export const useNavigationGuard = (shouldBlock: boolean, message = 'Unsaved changes. Leave?') => {
  const blocker = useBlocker(shouldBlock);
  useEffect(() => {
    if (blocker.state === 'blocked') {
      if (window.confirm(message)) blocker.proceed();
      else blocker.reset();
    }
  }, [blocker, message]);
};

// Usage
const MyForm = () => {
  const [isDirty, setIsDirty] = useState(false);
  useNavigationGuard(isDirty, 'You have unsaved changes. Leave?');
  return <form>{/* content */}</form>;
};
```

## Scroll Restoration

```typescript
import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';

export const ScrollToTop = () => {
  const { pathname } = useLocation();
  useEffect(() => { window.scrollTo(0, 0); }, [pathname]);
  return null;
};

// Add to App
<BrowserRouter>
  <ScrollToTop />
  <Routes>{/* routes */}</Routes>
</BrowserRouter>
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

## Checklist
- [ ] Routes in constants file (no magic strings)
- [ ] Helper functions for parameterized routes
- [ ] Layout routes for shared UI
- [ ] Protected routes for authentication
- [ ] Role-based access control
- [ ] 404 page configured
- [ ] Lazy loading for code splitting
- [ ] Suspense fallback for loading
- [ ] URL state for filters/search/pagination
- [ ] Scroll restoration
- [ ] Navigation guards (if needed)
- [ ] Breadcrumbs (if needed)
