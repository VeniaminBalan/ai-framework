# Keycloak Integration Reference

Detailed rules and conventions for Keycloak integration in ASP.NET Core applications.

## Dependencies

### Required NuGet Packages

```xml
<!-- Keycloak packages (version 2.7.0+) -->
<PackageReference Include="Keycloak.AuthServices.Authentication" />
<PackageReference Include="Keycloak.AuthServices.Authorization" />
<PackageReference Include="Keycloak.AuthServices.Common" />
<PackageReference Include="Keycloak.AuthServices.Sdk" />

<!-- Token management -->
<PackageReference Include="Duende.AccessTokenManagement" />

<!-- JWT Bearer -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
```

## Configuration

### appsettings.json Structure

```json
{
  "Keycloak": {
    "realm": "your-realm",
    "AuthServerUrl": "https://your-keycloak-server/",
    "ssl-required": "external",
    "resource": "your-client-id",
    "verify-token-audience": false,
    "credentials": {
      "secret": "your-client-secret"
    },
    "confidential-port": 0,
    "policy-enforcer": {
      "credentials": {}
    }
  }
}
```

### Configuration Parameters

| Parameter | Description | Example |
|-----------|-------------|---------|
| `realm` | Keycloak realm name | `"sigl-dev"` |
| `AuthServerUrl` | Base URL of Keycloak server | `"https://auth.example.com/"` |
| `ssl-required` | SSL requirement level | `"external"`, `"none"`, `"all"` |
| `resource` | Client ID for the backend | `"sigl-backend"` |
| `verify-token-audience` | Validate token audience | `true` or `false` |
| `credentials.secret` | Client secret | (environment-specific) |

### Security Considerations

- **Never commit secrets** to source control
- Use environment-specific `appsettings.{Environment}.json`
- Use User Secrets for local development
- Use environment variables or Azure Key Vault in production
- Set `ssl-required: "external"` or `"all"` in production
- Enable `verify-token-audience` in production

## Authentication Setup

### JWT Bearer Authentication

Configure JWT Bearer authentication using Keycloak middleware:

```csharp
services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddKeycloakWebApi(config);
```

This automatically:
- Sets up JWT token validation
- Configures issuer validation against Keycloak realm
- Retrieves signing keys from Keycloak's OIDC discovery endpoint
- Validates token expiration

### Client Credentials Flow

For backend-to-backend communication (e.g., Keycloak Admin API):

```csharp
services.AddDistributedMemoryCache();
services
    .AddClientCredentialsTokenManagement()
    .AddClient("keycloak", client =>
    {
        client.ClientId = ClientId.Parse(options.Resource);
        client.ClientSecret = ClientSecret.Parse(options.Credentials.Secret);
        client.TokenEndpoint = new Uri(options.KeycloakTokenEndpoint);
    });
```

## Authorization Policies

### Policy Definition

Define policies using realm roles:

```csharp
services
    .AddAuthorization()
    .AddKeycloakAuthorization()
    .AddAuthorizationBuilder()
    .AddPolicy("PolicyName", policy => policy.RequireRealmRoles("role1", "role2"));
```

### Standard Policy Patterns

| Policy Name | Purpose | Roles |
|-------------|---------|-------|
| `AdminOnly` | Administrative operations | `admin` |
| `Users` | Regular user operations | `responsible`, `performer` |
| `All` | Any authenticated user | (none - just authenticated) |

### Role Constants

Define roles as constants for consistency:

```csharp
public static class AppRoles
{
    public const string Admin = "admin";
    public const string Responsible = "responsible";
    public const string Performer = "performer";
}
```

### Policy Constants

Define policy names as constants:

```csharp
public static class Policies
{
    public const string AdminOnly = "AdminOnly";
    public const string Users = "Users";
    public const string All = "All";
}
```

## Claims Processing

### Keycloak Realm Access Claim

Keycloak stores roles in the `realm_access` claim as JSON:

```json
{
  "realm_access": {
    "roles": ["admin", "responsible"]
  }
}
```

### Extracting Roles

Parse the `realm_access` claim to extract roles:

```csharp
var realmRoles = claims.Claims
    .Where(c => c.Type == "realm_access")
    .SelectMany(c => JsonDocument.Parse(c.Value)
        .RootElement
        .GetProperty("roles")
        .EnumerateArray()
        .Select(r => r.GetString()))
    .ToList();
```

## User Context

### IUserContext Interface

Provide access to current user information:

```csharp
public interface IUserContext
{
    string? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    ClaimsPrincipal? User { get; }
    RoleType GetApplicationRole();
}
```

### Registration

```csharp
services.AddHttpContextAccessor();
services.AddScoped<IUserContext, UserContext>();
```

## Middleware Pipeline

### Correct Order

The order of middleware is critical:

```csharp
app.UseAuthentication();     // 1. Authenticate user from JWT
app.UseAuthorization();      // 2. Apply authorization policies

// Custom middleware after auth
app.UseUserSyncMiddleware();

app.MapControllers();

// SignalR with authorization
app.MapHub<NotificationsHub>("/notifications")
    .RequireAuthorization();
```

## Keycloak Admin API

### HTTP Client Registration

```csharp
services.AddKeycloakAdminHttpClient(config)
    .AddClientCredentialsTokenHandler(ClientCredentialsClientName.Parse("keycloak"));
```

This client automatically:
- Obtains access tokens using client credentials
- Caches and refreshes tokens automatically
- Includes tokens in Authorization header

## OIDC Endpoints

Standard Keycloak endpoints:

| Endpoint | URL Pattern |
|----------|-------------|
| Discovery | `{AuthServerUrl}/realms/{realm}/.well-known/openid-configuration` |
| Authorization | `{AuthServerUrl}/realms/{realm}/protocol/openid-connect/auth` |
| Token | `{AuthServerUrl}/realms/{realm}/protocol/openid-connect/token` |
| UserInfo | `{AuthServerUrl}/realms/{realm}/protocol/openid-connect/userinfo` |
| Logout | `{AuthServerUrl}/realms/{realm}/protocol/openid-connect/logout` |

## Troubleshooting

### Common Status Codes

| Code | Meaning | Common Cause |
|------|---------|--------------|
| 401 | Unauthorized | Missing/invalid/expired token |
| 403 | Forbidden | Valid auth but missing required role |

### Debugging Checklist

1. **401 Unauthorized**
   - Verify `Authorization: Bearer {token}` header
   - Check token expiration (`exp` claim)
   - Verify Keycloak server is accessible
   - Check realm and client ID configuration

2. **403 Forbidden**
   - Verify user has required roles in Keycloak
   - Check policy configuration matches role names
   - Verify `realm_access` claim parsing

3. **Token Validation Failures**
   - Verify realm name matches configuration
   - Check client ID (`resource`) is correct
   - Ensure server clocks are synchronized
