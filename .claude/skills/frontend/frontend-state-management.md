# Frontend State Management

## Overview
This skill covers state management patterns and best practices for React applications.

## State Types

### Local State
- Use `useState` for simple component-local state
- Use `useReducer` for complex state logic with multiple sub-values
- Keep state as local as possible
- Lift state up only when needed by multiple components

### Global State
- Use **React Context** combined with custom hooks for shared/global state
- Avoid prop drilling beyond 2-3 levels
- Create context providers with clear, focused responsibilities

## Context Pattern

### Creating a Context with Custom Hook

```typescript
// contexts/AuthContext.tsx
import { createContext, useContext, useState, ReactNode } from 'react';

interface AuthContextType {
  user: User | null;
  login: (credentials: Credentials) => Promise<void>;
  logout: () => void;
  isAuthenticated: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [user, setUser] = useState<User | null>(null);

  const login = async (credentials: Credentials) => {
    // Login logic
    const user = await authService.login(credentials);
    setUser(user);
  };

  const logout = () => {
    setUser(null);
  };

  const value = {
    user,
    login,
    logout,
    isAuthenticated: !!user,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

// hooks/useAuth.ts
export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
};
```

### Using the Context

```typescript
// In a component
import { useAuth } from '@/hooks/useAuth';

const MyComponent = () => {
  const { user, isAuthenticated, logout } = useAuth();
  
  if (!isAuthenticated) {
    return <LoginPrompt />;
  }
  
  return (
    <div>
      <p>Welcome, {user.name}</p>
      <button onClick={logout}>Logout</button>
    </div>
  );
};
```

## State Management Rules

### When to Use Local State
- State is only used within a single component
- State doesn't need to persist across unmounts
- State is simple and doesn't require complex updates

### When to Use Context
- State is needed by multiple components at different nesting levels
- You want to avoid prop drilling
- State represents application-wide concerns (auth, theme, language)

### When to Use useReducer
- State has complex update logic
- State has multiple sub-values
- Next state depends on the previous state
- You want to centralize state update logic

## useReducer Pattern

```typescript
interface State {
  items: Item[];
  loading: boolean;
  error: string | null;
  filters: Filters;
}

type Action =
  | { type: 'FETCH_START' }
  | { type: 'FETCH_SUCCESS'; payload: Item[] }
  | { type: 'FETCH_ERROR'; payload: string }
  | { type: 'UPDATE_FILTERS'; payload: Partial<Filters> }
  | { type: 'RESET' };

const initialState: State = {
  items: [],
  loading: false,
  error: null,
  filters: { search: '', category: 'all' },
};

function reducer(state: State, action: Action): State {
  switch (action.type) {
    case 'FETCH_START':
      return { ...state, loading: true, error: null };
    case 'FETCH_SUCCESS':
      return { ...state, loading: false, items: action.payload };
    case 'FETCH_ERROR':
      return { ...state, loading: false, error: action.payload };
    case 'UPDATE_FILTERS':
      return { ...state, filters: { ...state.filters, ...action.payload } };
    case 'RESET':
      return initialState;
    default:
      return state;
  }
}

// Usage in component
const [state, dispatch] = useReducer(reducer, initialState);

// Dispatching actions
dispatch({ type: 'FETCH_START' });
dispatch({ type: 'FETCH_SUCCESS', payload: data });
dispatch({ type: 'UPDATE_FILTERS', payload: { search: 'query' } });
```

## Multiple Contexts Pattern

```typescript
// App.tsx - Compose multiple providers
export const App = () => {
  return (
    <AuthProvider>
      <ThemeProvider>
        <LanguageProvider>
          <Router>
            <AppRoutes />
          </Router>
        </LanguageProvider>
      </ThemeProvider>
    </AuthProvider>
  );
};

// Or create a composite provider
export const AppProviders = ({ children }: { children: ReactNode }) => {
  return (
    <AuthProvider>
      <ThemeProvider>
        <LanguageProvider>
          {children}
        </LanguageProvider>
      </ThemeProvider>
    </AuthProvider>
  );
};
```

## State Management Best Practices

1. **Keep state minimal**: Only store what's necessary
2. **Derive computed values**: Don't store values that can be calculated
3. **Avoid state duplication**: Single source of truth for each piece of state
4. **Colocate state**: Keep state as close to where it's used as possible
5. **Use proper TypeScript types**: Strongly type all state and actions
6. **Handle loading and error states**: Always account for async operations
7. **Provide default values**: Ensure contexts have sensible defaults

## Common Patterns

### Derived State
```typescript
// Good: Derive from existing state
const { data: users } = useUsers();
const activeUsers = users?.filter(u => u.isActive) || [];

// Bad: Store derived state separately
const [users, setUsers] = useState([]);
const [activeUsers, setActiveUsers] = useState([]);
```

### State Updates with Previous State
```typescript
// Good: Use functional update
setCount(prev => prev + 1);

// Bad: Use current state value
setCount(count + 1);
```

### Complex State Updates
```typescript
// Good: Use useReducer for complex logic
const [state, dispatch] = useReducer(reducer, initialState);
dispatch({ type: 'UPDATE_USER', payload: newUser });

// Bad: Multiple useState calls with interdependent logic
const [user, setUser] = useState(null);
const [loading, setLoading] = useState(false);
const [error, setError] = useState(null);
// Complex update logic scattered throughout component
```

## Checklist for State Management
- [ ] State is at the appropriate level (local vs global)
- [ ] No unnecessary state duplication
- [ ] Derived values are computed, not stored
- [ ] Context providers are properly typed
- [ ] Custom hooks are used to consume contexts
- [ ] Error boundaries protect context providers
- [ ] State updates use functional form when depending on previous state
- [ ] Complex state logic uses useReducer
