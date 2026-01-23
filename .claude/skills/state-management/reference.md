# State Management Reference

Detailed rules and conventions for state management in React applications.

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

## When to Use What

### When to Use Local State (useState)
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

## Context Pattern

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
```

## Best Practices

1. **Keep state minimal**: Only store what's necessary
2. **Derive computed values**: Don't store values that can be calculated
3. **Avoid state duplication**: Single source of truth for each piece of state
4. **Colocate state**: Keep state as close to where it's used as possible
5. **Use proper TypeScript types**: Strongly type all state and actions
6. **Handle loading and error states**: Always account for async operations
7. **Provide default values**: Ensure contexts have sensible defaults
