# Frontend Keycloak Integration Reference

Detailed rules and conventions for Keycloak authentication in React applications.

## Dependencies

### Required npm Packages

```json
{
  "oidc-client-ts": "^3.0.1",
  "react-oidc-context": "^3.1.0",
  "jwt-decode": "^4.0.0"
}
```

## Environment Configuration

### Environment Variables

Define Keycloak settings in `.env` files:

```env
VITE_KEYCLOAK_REALM=your-realm
VITE_KEYCLOAK_AUTH_SERVER_URL=https://your-keycloak-server/
VITE_KEYCLOAK_RESOURCE=your-client-id
VITE_APP_API_URL=https://your-api-url/
```

### Variable Descriptions

| Variable | Description | Example |
|----------|-------------|---------|
| `VITE_KEYCLOAK_REALM` | Keycloak realm name | `"sigl"` |
| `VITE_KEYCLOAK_AUTH_SERVER_URL` | Base URL of Keycloak server | `"https://auth.example.com/"` |
| `VITE_KEYCLOAK_RESOURCE` | Client ID registered in Keycloak | `"sigl-frontend"` |
| `VITE_APP_API_URL` | Backend API base URL | `"https://api.example.com/"` |

### Security Considerations

- **Never commit** `.env.production` with real values to source control
- Use `.env.example` as a template
- Different environments should have separate Keycloak realms/clients

## UserManager Configuration

### Authority URL Construction

The authority URL follows the pattern:

```
{AUTH_SERVER_URL}/realms/{REALM}
```

Example: `https://auth.example.com/realms/sigl`

### Configuration Options

| Option | Description | Recommended Value |
|--------|-------------|-------------------|
| `authority` | Full Keycloak realm URL | `${AUTH_SERVER_URL}/realms/${REALM}` |
| `client_id` | Keycloak client ID | From environment variable |
| `redirect_uri` | Post-login redirect URL | `window.location.origin + pathname` |
| `post_logout_redirect_uri` | Post-logout redirect URL | `window.location.origin` |
| `userStore` | Token storage mechanism | `WebStorageStateStore({ store: localStorage })` |
| `monitorSession` | Cross-tab session sync | `true` |
| `automaticSilentRenew` | Background token refresh | `true` |

### Storage Options

- **localStorage**: Persists across browser sessions
- **sessionStorage**: Cleared when tab closes
- **Memory**: No persistence (not recommended)

## AuthProvider Setup

### Root Level Integration

The AuthProvider must wrap the entire application:

```tsx
<AuthProvider userManager={userManager} onSigninCallback={onSigninCallback}>
  <App />
</AuthProvider>
```

### Signin Callback

After successful authentication, clean up the URL:

```typescript
const onSigninCallback = async (user: User) => {
  window.history.replaceState({}, document.title, window.location.pathname);
};
```

This removes OIDC callback parameters (`code`, `state`, etc.) from the URL.

## Route Protection

### Two-Level Protection Pattern

1. **PrivateRoutes**: Requires authentication (any logged-in user)
2. **RequireAuth**: Requires specific roles

### Protection Hierarchy

```
PublicRoutes (no auth required)
└── PrivateRoutes (authentication required)
    └── RequireAuth (specific roles required)
        └── Protected Component
```

### Redirect Behavior

- Unauthenticated users → Keycloak login page
- Authenticated but unauthorized → `/forbidden` page

## Role Extraction

### JWT Token Structure

Keycloak includes roles in the `realm_access` claim:

```json
{
  "realm_access": {
    "roles": ["responsible", "performer", "admin"]
  }
}
```

### Decoding Process

1. Get `access_token` from authenticated user
2. Decode JWT using `jwt-decode`
3. Extract `realm_access.roles` array
4. Compare against required roles

### Role Types

| Role | Description |
|------|-------------|
| `admin` | Administrative access |
| `responsible` | Responsible user role |
| `performer` | Performer user role |

## API Integration

### Axios Interceptors

#### Request Interceptor

Attach Bearer token to all requests:

```typescript
request.headers.Authorization = `Bearer ${user.access_token}`;
```

#### Response Interceptor

Handle authentication errors:

- **401 Unauthorized**: Redirect to login (`signinRedirect()`)
- **403 Forbidden**: Show forbidden page (don't redirect)

### Token Attachment Pattern

```
API Request → Request Interceptor → Get User → Attach Token → Send Request
```

## Authentication Flow

### Login Flow

```
1. User clicks Login
2. signinRedirect() called
3. Redirect to Keycloak login page
4. User authenticates
5. Keycloak redirects with authorization code
6. Library exchanges code for tokens
7. Tokens stored in localStorage
8. onSigninCallback cleans URL
9. User is authenticated
```

### Logout Flow

```
1. User clicks Logout
2. signoutRedirect() called
3. Local session cleared
4. Redirect to Keycloak logout
5. Keycloak logs out user
6. Redirect back to application
7. User is logged out
```

### Silent Renewal Flow

```
1. Token approaching expiry
2. automaticSilentRenew triggers
3. Hidden iframe loads Keycloak
4. New tokens obtained silently
5. Tokens updated in storage
6. Session continues uninterrupted
```

## useAuth Hook

### Available Properties

| Property | Type | Description |
|----------|------|-------------|
| `user` | `User \| null` | Authenticated user object |
| `isLoading` | `boolean` | Authentication state loading |
| `isAuthenticated` | `boolean` | Whether user is authenticated |
| `signinRedirect` | `() => Promise<void>` | Trigger login flow |
| `signoutRedirect` | `() => Promise<void>` | Trigger logout flow |

### User Object Properties

| Property | Description |
|----------|-------------|
| `user.access_token` | JWT access token for API calls |
| `user.profile` | User profile information |
| `user.profile.preferred_username` | Username |
| `user.profile.name` | Full name |
| `user.profile.email` | Email address |

## Keycloak Client Requirements

### Client Configuration

The Keycloak client must be configured with:

| Setting | Value |
|---------|-------|
| Client Protocol | `openid-connect` |
| Access Type | `public` (SPA) or `confidential` with PKCE |
| Standard Flow Enabled | `ON` |
| Valid Redirect URIs | Application URLs (e.g., `https://app.example.com/*`) |
| Web Origins | Application domains (for CORS) |

### Token Mappers

Ensure realm roles are included in tokens:

- Mapper Type: `User Realm Role`
- Token Claim Name: `realm_access.roles`
- Add to access token: `ON`

## Troubleshooting

### Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| Infinite redirect loop | Invalid redirect URI | Match `redirect_uri` with Keycloak config |
| 401 on API calls | Token expired/invalid | Check `automaticSilentRenew`, verify backend |
| Roles not detected | Missing in JWT | Configure Keycloak client mappers |
| CORS errors | Web origins not configured | Add domain to Keycloak client |
| Login fails | Wrong authority URL | Verify realm name and server URL |

### Debugging Tips

1. **Check localStorage**: Look for `oidc.user:*` keys
2. **Decode JWT**: Use jwt.io to inspect token claims
3. **Check Network**: Monitor Keycloak requests in DevTools
4. **Verify Config**: Log UserManager settings at startup
