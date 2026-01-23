# State Management Examples

## Context with Custom Hook

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
    const user = await authService.login(credentials);
    setUser(user);
  };

  const logout = () => setUser(null);

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

// Usage in component
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

## Derived State

```typescript
// Good: Derive from existing state
const { data: users } = useUsers();
const activeUsers = users?.filter(u => u.isActive) || [];

// Bad: Store derived state separately
const [users, setUsers] = useState([]);
const [activeUsers, setActiveUsers] = useState([]); // Don't do this
```

## State Updates with Previous State

```typescript
// Good: Use functional update
setCount(prev => prev + 1);

// Bad: Use current state value (can cause stale closure issues)
setCount(count + 1);
```

## Complex State Updates

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

## Form State Management

```typescript
// Local state for form data
const [formData, setFormData] = useState<FormType>(initialFormData);

// Update handler with functional update
const handleChange = useCallback((field: keyof FormType, value: string | number) => {
  setFormData(prev => ({ ...prev, [field]: value }));
}, []);

// Reset function
const resetForm = useCallback((data?: FormType) => {
  setFormData(data ?? initialFormData);
}, []);
```

## Loading and Error States

```typescript
interface AsyncState<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
}

const [state, setState] = useState<AsyncState<User[]>>({
  data: null,
  loading: false,
  error: null,
});

// Or use React Query which handles this automatically
const { data, isLoading, error } = useQuery({
  queryKey: ['users'],
  queryFn: fetchUsers,
});
```

## Theme Context Example

```typescript
type Theme = 'light' | 'dark' | 'system';

interface ThemeContextType {
  theme: Theme;
  setTheme: (theme: Theme) => void;
  resolvedTheme: 'light' | 'dark';
}

const ThemeContext = createContext<ThemeContextType | undefined>(undefined);

export const ThemeProvider = ({ children }: { children: ReactNode }) => {
  const [theme, setTheme] = useState<Theme>(() => {
    return (localStorage.getItem('theme') as Theme) || 'system';
  });

  const [resolvedTheme, setResolvedTheme] = useState<'light' | 'dark'>('light');

  useEffect(() => {
    const systemTheme = window.matchMedia('(prefers-color-scheme: dark)').matches
      ? 'dark'
      : 'light';
    const effectiveTheme = theme === 'system' ? systemTheme : theme;

    setResolvedTheme(effectiveTheme);
    document.documentElement.setAttribute('data-theme', effectiveTheme);
    localStorage.setItem('theme', theme);
  }, [theme]);

  return (
    <ThemeContext.Provider value={{ theme, setTheme, resolvedTheme }}>
      {children}
    </ThemeContext.Provider>
  );
};

export const useTheme = () => {
  const context = useContext(ThemeContext);
  if (!context) {
    throw new Error('useTheme must be used within ThemeProvider');
  }
  return context;
};
```
