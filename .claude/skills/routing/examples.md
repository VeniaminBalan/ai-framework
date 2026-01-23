# Routing Examples

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

## Layout Routes with Nesting

```typescript
import { Outlet } from 'react-router-dom';

const AppLayout = () => (
  <div>
    <Header />
    <Sidebar />
    <main><Outlet /></main>
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

## Protected Routes with Roles

```typescript
// Usage in router
<Route element={<ProtectedRoute />}>
  <Route path={ROUTES.DASHBOARD} element={<DashboardPage />} />
</Route>

<Route element={<ProtectedRoute allowedRoles={['admin']} />}>
  <Route path="/admin" element={<AdminPage />} />
</Route>
```

## Navigation Examples

```typescript
import { Link, NavLink, useNavigate } from 'react-router-dom';
import { ROUTES, getRoute } from '@/constants/routes';

// Link with state
<Link to={ROUTES.LOGIN} state={{ from: location }}>Login</Link>

// NavLink with active styling
<NavLink
  to={ROUTES.DASHBOARD}
  className={({ isActive }) => isActive ? 'nav-active' : 'nav-link'}
>
  Dashboard
</NavLink>

// Programmatic navigation
const MyComponent = () => {
  const navigate = useNavigate();

  const handleSubmit = async (data: FormData) => {
    const user = await createUser(data);
    navigate(getRoute.userDetail(user.id));
  };

  const handleCancel = () => navigate(-1);
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

// Search parameters
const UsersPage = () => {
  const [searchParams, setSearchParams] = useSearchParams();

  const search = searchParams.get('search') || '';
  const page = parseInt(searchParams.get('page') || '1');
  const sort = searchParams.get('sort') || 'name';

  const updateSearch = (value: string) => {
    setSearchParams(prev => {
      if (value) prev.set('search', value);
      else prev.delete('search');
      prev.set('page', '1');
      return prev;
    });
  };

  return (
    <div>
      <input value={search} onChange={(e) => updateSearch(e.target.value)} />
      <UserList filters={{ search, page, sort }} />
    </div>
  );
};
```

## Lazy Loading

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

## Navigation Guards

```typescript
import { useBlocker } from 'react-router-dom';

export const useNavigationGuard = (
  shouldBlock: boolean,
  message = 'Unsaved changes. Leave?'
) => {
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

## 404 Page

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
```

## Login with Redirect

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
```
