# Frontend Keycloak Integration Examples

## UserManager Configuration

### config.ts

```typescript
import { User, UserManager, WebStorageStateStore } from "oidc-client-ts";

const REALM = import.meta.env.VITE_KEYCLOAK_REALM as string;
const AUTH_SERVER_URL = import.meta.env.VITE_KEYCLOAK_AUTH_SERVER_URL as string;
const RESOURCE = import.meta.env.VITE_KEYCLOAK_RESOURCE as string;

export const userManager = new UserManager({
  authority: `${AUTH_SERVER_URL}/realms/${REALM}`,
  client_id: RESOURCE,
  redirect_uri: `${window.location.origin}${window.location.pathname}`,
  post_logout_redirect_uri: window.location.origin,
  userStore: new WebStorageStateStore({ store: window.localStorage }),
  monitorSession: true,
  automaticSilentRenew: true,
});

export const onSigninCallback = async (_user: User | void) => {
  window.history.replaceState({}, document.title, window.location.pathname);
};

export const getAccountUrl = () =>
  `${AUTH_SERVER_URL}realms/${REALM}/account`;
```

## Application Setup

### main.tsx

```tsx
import React from "react";
import ReactDOM from "react-dom/client";
import { AuthProvider } from "react-oidc-context";
import { onSigninCallback, userManager } from "./config";
import App from "./App";

const root = ReactDOM.createRoot(
  document.getElementById("root") as HTMLElement
);

root.render(
  <React.StrictMode>
    <AuthProvider
      userManager={userManager}
      onSigninCallback={onSigninCallback}
    >
      <App />
    </AuthProvider>
  </React.StrictMode>
);
```

### App.tsx with Loading State

```tsx
import { useAuth } from "react-oidc-context";
import { MainPageSkeleton } from "./Components/Skeletons";
import { RouterProvider } from "react-router-dom";
import { router } from "./Router";

function App() {
  const { isLoading } = useAuth();

  if (isLoading) {
    return <MainPageSkeleton />;
  }

  return <RouterProvider router={router} />;
}

export default App;
```

## Route Protection

### PrivateRoutes Component

```tsx
import { useEffect } from "react";
import { Outlet } from "react-router-dom";
import { useAuth } from "react-oidc-context";

const PrivateRoutes = () => {
  const { user, signinRedirect, isLoading } = useAuth();

  useEffect(() => {
    if (!user && !isLoading) {
      signinRedirect();
    }
  }, [user, isLoading, signinRedirect]);

  if (isLoading) {
    return null; // Or loading spinner
  }

  if (!user) {
    return null; // Will redirect via useEffect
  }

  return <Outlet />;
};

export default PrivateRoutes;
```

### RequireAuth Component (Role-Based)

```tsx
import { useEffect } from "react";
import { Outlet, useNavigate } from "react-router-dom";
import { useRoles } from "../hooks/useRoles";

type RequireAuthProps = {
  allowedRoles: string[];
};

const RequireAuth = ({ allowedRoles }: RequireAuthProps) => {
  const { roles } = useRoles();
  const navigate = useNavigate();

  useEffect(() => {
    const hasRequiredRole = roles.some((role) =>
      allowedRoles.includes(role)
    );

    if (roles.length > 0 && !hasRequiredRole) {
      navigate("/forbidden");
    }
  }, [roles, allowedRoles, navigate]);

  return <Outlet />;
};

export default RequireAuth;
```

### Router Configuration

```tsx
import { createBrowserRouter, Route, Routes } from "react-router-dom";
import PrivateRoutes from "./PrivateRoutes";
import RequireAuth from "./RequireAuth";

// Public pages
import HomePage from "../pages/HomePage";
import LoginPage from "../pages/LoginPage";
import ForbiddenPage from "../pages/ForbiddenPage";

// Protected pages
import Dashboard from "../pages/Dashboard";
import CreateRequestForm from "../pages/CreateRequestForm";
import AdminPanel from "../pages/AdminPanel";

export const router = createBrowserRouter([
  // Public routes
  { path: "/", element: <HomePage /> },
  { path: "/login", element: <LoginPage /> },
  { path: "/forbidden", element: <ForbiddenPage /> },

  // Protected routes (authentication required)
  {
    element: <PrivateRoutes />,
    children: [
      { path: "/dashboard", element: <Dashboard /> },

      // Role-restricted routes
      {
        element: <RequireAuth allowedRoles={["responsible", "performer"]} />,
        children: [
          { path: "/new-request", element: <CreateRequestForm /> },
        ],
      },
      {
        element: <RequireAuth allowedRoles={["admin"]} />,
        children: [
          { path: "/admin", element: <AdminPanel /> },
        ],
      },
    ],
  },
]);
```

## Role Extraction

### useRoles Hook

```tsx
import { useState, useEffect } from "react";
import { jwtDecode } from "jwt-decode";
import { useAuth } from "react-oidc-context";

interface DecodedToken {
  realm_access?: {
    roles: string[];
  };
}

export const useRoles = () => {
  const { user } = useAuth();

  const getRoles = (): string[] => {
    if (!user?.access_token) {
      return [];
    }

    try {
      const decodedToken = jwtDecode<DecodedToken>(user.access_token);
      return decodedToken.realm_access?.roles || [];
    } catch {
      console.error("Failed to decode token");
      return [];
    }
  };

  const [roles, setRoles] = useState<string[]>(getRoles());

  useEffect(() => {
    setRoles(getRoles());
  }, [user]);

  const hasRole = (role: string): boolean => roles.includes(role);

  const hasAnyRole = (requiredRoles: string[]): boolean =>
    requiredRoles.some((role) => roles.includes(role));

  const isAdmin = hasRole("admin");
  const isResponsible = hasRole("responsible");
  const isPerformer = hasRole("performer");

  return {
    roles,
    hasRole,
    hasAnyRole,
    isAdmin,
    isResponsible,
    isPerformer,
    user,
  };
};
```

## API Integration

### Axios Configuration with Interceptors

```typescript
import axios, { AxiosError, InternalAxiosRequestConfig } from "axios";
import { userManager } from "../config";

const authAxios = axios.create({
  baseURL: import.meta.env.VITE_APP_API_URL,
  headers: {
    "Content-Type": "application/json",
  },
});

// Request Interceptor - Attach Access Token
authAxios.interceptors.request.use(
  async (config: InternalAxiosRequestConfig) => {
    const user = await userManager.getUser();

    if (user?.access_token) {
      config.headers.Authorization = `Bearer ${user.access_token}`;
    }

    return config;
  },
  (error) => Promise.reject(error)
);

// Response Interceptor - Handle 401 Unauthorized
authAxios.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    if (error.response?.status === 401) {
      // Token expired or invalid - redirect to login
      await userManager.signinRedirect();
    }

    return Promise.reject(error);
  }
);

export default authAxios;
```

### API Service Example

```typescript
import authAxios from "./axios";

export interface User {
  id: string;
  name: string;
  email: string;
}

export const userService = {
  getProfile: async (): Promise<User> => {
    const response = await authAxios.get<User>("/api/v1/users/me");
    return response.data;
  },

  updateProfile: async (data: Partial<User>): Promise<User> => {
    const response = await authAxios.put<User>("/api/v1/users/me", data);
    return response.data;
  },
};
```

## UI Components

### NavBar with Authentication

```tsx
import { Link } from "react-router-dom";
import { useAuth } from "react-oidc-context";
import { useRoles } from "../hooks/useRoles";
import { getAccountUrl } from "../config";

const NavBar = () => {
  const { user, signinRedirect, signoutRedirect } = useAuth();
  const { isAdmin } = useRoles();

  return (
    <nav className="navbar">
      <div className="nav-brand">
        <Link to="/">App Name</Link>
      </div>

      <div className="nav-links">
        {user && (
          <>
            <Link to="/dashboard">Dashboard</Link>
            {isAdmin && <Link to="/admin">Admin</Link>}
          </>
        )}
      </div>

      <div className="nav-auth">
        {user ? (
          <>
            <span className="user-name">
              {user.profile?.name || user.profile?.preferred_username}
            </span>
            <a
              href={getAccountUrl()}
              target="_blank"
              rel="noopener noreferrer"
            >
              Account
            </a>
            <button onClick={() => signoutRedirect()}>
              Logout
            </button>
          </>
        ) : (
          <button onClick={() => signinRedirect()}>
            Login
          </button>
        )}
      </div>
    </nav>
  );
};

export default NavBar;
```

### Login Button Component

```tsx
import { useAuth } from "react-oidc-context";

const LoginButton = () => {
  const { signinRedirect, isLoading } = useAuth();

  return (
    <button
      onClick={() => signinRedirect()}
      disabled={isLoading}
      className="btn-login"
    >
      {isLoading ? "Loading..." : "Sign in with Keycloak"}
    </button>
  );
};

export default LoginButton;
```

### User Profile Component

```tsx
import { useAuth } from "react-oidc-context";
import { useRoles } from "../hooks/useRoles";
import { getAccountUrl } from "../config";

const UserProfile = () => {
  const { user } = useAuth();
  const { roles } = useRoles();

  if (!user) return null;

  return (
    <div className="user-profile">
      <h2>{user.profile?.name || "User"}</h2>
      <p>Username: {user.profile?.preferred_username}</p>
      <p>Email: {user.profile?.email}</p>

      <div className="roles">
        <h3>Roles</h3>
        <ul>
          {roles.map((role) => (
            <li key={role}>{role}</li>
          ))}
        </ul>
      </div>

      <a
        href={getAccountUrl()}
        target="_blank"
        rel="noopener noreferrer"
        className="btn-secondary"
      >
        Manage Account
      </a>
    </div>
  );
};

export default UserProfile;
```

## Common Mistakes to Avoid

### Wrong authority URL format

```typescript
// Wrong - missing /realms/ path
const userManager = new UserManager({
  authority: `${AUTH_SERVER_URL}/${REALM}`,
});

// Correct - include /realms/ in path
const userManager = new UserManager({
  authority: `${AUTH_SERVER_URL}/realms/${REALM}`,
});
```

### Not handling loading state

```tsx
// Wrong - no loading handling
const PrivateRoutes = () => {
  const { user, signinRedirect } = useAuth();

  if (!user) {
    signinRedirect(); // Called on every render!
    return null;
  }

  return <Outlet />;
};

// Correct - check loading state first
const PrivateRoutes = () => {
  const { user, signinRedirect, isLoading } = useAuth();

  useEffect(() => {
    if (!user && !isLoading) {
      signinRedirect();
    }
  }, [user, isLoading]);

  if (isLoading) return null;

  return <Outlet />;
};
```

### Hardcoding Keycloak URLs

```typescript
// Wrong - hardcoded values
const userManager = new UserManager({
  authority: "https://auth.example.com/realms/sigl",
  client_id: "sigl-frontend",
});

// Correct - use environment variables
const userManager = new UserManager({
  authority: `${import.meta.env.VITE_KEYCLOAK_AUTH_SERVER_URL}/realms/${import.meta.env.VITE_KEYCLOAK_REALM}`,
  client_id: import.meta.env.VITE_KEYCLOAK_RESOURCE,
});
```

### Not cleaning URL after signin

```typescript
// Wrong - OIDC params remain in URL
export const onSigninCallback = async (user: User) => {
  console.log("User signed in:", user);
  // URL still has ?code=xxx&state=xxx
};

// Correct - clean up URL
export const onSigninCallback = async (user: User) => {
  window.history.replaceState({}, document.title, window.location.pathname);
};
```

### Checking roles before they're loaded

```tsx
// Wrong - roles might not be loaded yet
const RequireAuth = ({ allowedRoles }: RequireAuthProps) => {
  const { roles } = useRoles();
  const navigate = useNavigate();

  // This runs immediately, roles might be empty
  if (!roles.some((r) => allowedRoles.includes(r))) {
    navigate("/forbidden");
  }

  return <Outlet />;
};

// Correct - use useEffect and check roles length
const RequireAuth = ({ allowedRoles }: RequireAuthProps) => {
  const { roles } = useRoles();
  const navigate = useNavigate();

  useEffect(() => {
    // Only check after roles are loaded
    if (roles.length > 0) {
      const hasRole = roles.some((r) => allowedRoles.includes(r));
      if (!hasRole) {
        navigate("/forbidden");
      }
    }
  }, [roles, allowedRoles]);

  return <Outlet />;
};
```

### Forgetting to handle 401 in interceptor

```typescript
// Wrong - 401 errors propagate without handling
authAxios.interceptors.response.use(
  (response) => response,
  (error) => Promise.reject(error)
);

// Correct - redirect on 401
authAxios.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      await userManager.signinRedirect();
    }
    return Promise.reject(error);
  }
);
```
