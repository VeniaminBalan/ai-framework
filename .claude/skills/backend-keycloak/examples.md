# Keycloak Integration Examples

## Service Registration

### Complete Keycloak Setup

```csharp
public static class KeycloakExtensions
{
    private const string KeycloakClientName = "keycloak";

    public static void AddKeycloak(this IServiceCollection services, IConfiguration config)
    {
        // 1. Client credentials for Admin API
        var options = config.GetKeycloakOptions<KeycloakAdminClientOptions>();

        services.AddDistributedMemoryCache();
        services
            .AddClientCredentialsTokenManagement()
            .AddClient(
                KeycloakClientName,
                client =>
                {
                    client.ClientId = ClientId.Parse(options?.Resource!);
                    client.ClientSecret = ClientSecret.Parse(options?.Credentials.Secret!);
                    client.TokenEndpoint = new Uri(options?.KeycloakTokenEndpoint!);
                }
            );

        // 2. JWT Bearer authentication
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddKeycloakWebApi(config);

        // 3. Keycloak Admin HTTP client
        services.AddKeycloakAdminHttpClient(config)
            .AddClientCredentialsTokenHandler(ClientCredentialsClientName.Parse(KeycloakClientName));

        // 4. Authorization policies
        services
            .AddAuthorization()
            .AddKeycloakAuthorization()
            .AddAuthorizationBuilder()
            .AddPolicy(
                Policies.AdminOnly,
                policy => policy.RequireRealmRoles(AppRoles.Admin)
            )
            .AddPolicy(
                Policies.Users,
                policy => policy.RequireRealmRoles(AppRoles.Responsible, AppRoles.Performer)
            )
            .AddPolicy(
                Policies.All,
                policy => policy.RequireAuthenticatedUser()
            );
    }
}
```

## Role and Policy Constants

### AppRoles.cs

```csharp
namespace Presentation.API.AppConstants;

public static class AppRoles
{
    public const string Admin = "admin";
    public const string Responsible = "responsible";
    public const string Performer = "performer";
}
```

### Policies.cs

```csharp
namespace Presentation.API.AppConstants;

public static class Policies
{
    public const string AdminOnly = "AdminOnly";
    public const string Users = "Users";
    public const string All = "All";
}
```

## User Context Implementation

### IUserContext Interface

```csharp
public interface IUserContext
{
    string? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    ClaimsPrincipal? User { get; }
    string? ConnectionId { get; }
    RoleType GetApplicationRole();
}
```

### UserContext Implementation

```csharp
public class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public string? UserName =>
        _httpContextAccessor.HttpContext?.User?.FindFirst("preferred_username")?.Value;

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public bool IsAdmin =>
        _httpContextAccessor.HttpContext?.User?.HasRoles(AppRoles.Admin) ?? false;

    public ClaimsPrincipal? User =>
        _httpContextAccessor.HttpContext?.User;

    public string? ConnectionId =>
        _httpContextAccessor.HttpContext?.Request.Headers["X-Connection-Id"].FirstOrDefault();

    public RoleType GetApplicationRole()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return RoleType.None;

        if (user.HasRoles(AppRoles.Admin)) return RoleType.Admin;
        if (user.HasRoles(AppRoles.Responsible)) return RoleType.Responsible;
        if (user.HasRoles(AppRoles.Performer)) return RoleType.Performer;

        return RoleType.None;
    }
}
```

### Registration

```csharp
private static void AddUserContext(this IServiceCollection services)
{
    services.AddHttpContextAccessor();
    services.AddScoped<IUserContext, UserContext>();
}
```

## Claims Extensions

### ClaimsPrincipalExtensions

```csharp
public static class ClaimsPrincipalExtensions
{
    public static bool HasRoles(this ClaimsPrincipal claims, params string[] roles)
    {
        var realmRoles = claims.Claims
            .Where(c => c.Type == "realm_access")
            .SelectMany(c =>
            {
                var doc = JsonDocument.Parse(c.Value);
                return doc.RootElement
                    .GetProperty("roles")
                    .EnumerateArray()
                    .Select(r => r.GetString());
            })
            .ToList();

        return roles.Any(r => realmRoles.Contains(r, StringComparer.OrdinalIgnoreCase));
    }

    public static string? GetUserId(this ClaimsPrincipal claims)
    {
        return claims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public static string? GetUserName(this ClaimsPrincipal claims)
    {
        return claims.FindFirst("preferred_username")?.Value;
    }

    public static string? GetEmail(this ClaimsPrincipal claims)
    {
        return claims.FindFirst(ClaimTypes.Email)?.Value;
    }
}
```

## Controller Authorization

### Admin-Only Controller

```csharp
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IUserService _userService;

    public AccountsController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Gets all users (admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<UserDto>>> GetUsers(
        [FromQuery] SearchParams searchParams)
    {
        var users = await _userService.GetUsersAsync(searchParams);
        return Ok(users);
    }

    /// <summary>
    /// Registers a new user (admin only)
    /// </summary>
    [HttpPost("register-user")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserDto>> RegisterUser(
        [FromBody] RegisterUserDto registerUser)
    {
        var user = await _userService.RegisterUserAsync(registerUser);
        return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, user);
    }
}
```

### Mixed Authorization Controller

```csharp
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class RequestsController : ControllerBase
{
    private readonly IRequestService _requestService;
    private readonly IUserContext _userContext;

    public RequestsController(
        IRequestService requestService,
        IUserContext userContext)
    {
        _requestService = requestService;
        _userContext = userContext;
    }

    /// <summary>
    /// Gets requests for current user
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Policies.Users)]
    public async Task<ActionResult<PagedResult<RequestDto>>> GetMyRequests(
        [FromQuery] PaginationParameters parameters)
    {
        var userId = _userContext.UserId!;
        var requests = await _requestService.GetUserRequestsAsync(userId, parameters);
        return Ok(requests);
    }

    /// <summary>
    /// Gets all requests (admin only)
    /// </summary>
    [HttpGet("all")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<PagedResult<RequestDto>>> GetAllRequests(
        [FromQuery] PaginationParameters parameters)
    {
        var requests = await _requestService.GetAllRequestsAsync(parameters);
        return Ok(requests);
    }
}
```

## Swagger OAuth2 Integration

### Swagger Security Configuration

```csharp
private static void AddSwaggerSecurity(
    this IServiceCollection services,
    KeycloakInstallationOptions keycloakOptions)
{
    var url = $"{keycloakOptions.KeycloakUrlRealm}/protocol/openid-connect";

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        Type = SecuritySchemeType.OAuth2,
        In = ParameterLocation.Header,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri(url + "/auth"),
                TokenUrl = new Uri(url + "/token"),
                Scopes = new Dictionary<string, string>
                {
                    { "openid", "openid" },
                    { "profile", "profile" }
                }
            }
        }
    };

    services.AddSwaggerGen(option =>
    {
        option.AddSecurityDefinition("keycloak", securityScheme);
        option.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "keycloak"
                }
            }] = Array.Empty<string>()
        });
    });
}
```

## Integration Test Configuration

### Mock Keycloak Configuration

```csharp
builder.ConfigureAppConfiguration((context, config) =>
{
    config.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Keycloak:realm"] = "test-realm",
        ["Keycloak:auth-server-url"] = "http://localhost:8080/",
        ["Keycloak:AuthServerUrl"] = "http://localhost:8080/",
        ["Keycloak:ssl-required"] = "none",
        ["Keycloak:resource"] = "test-client",
        ["Keycloak:verify-token-audience"] = "false",
        ["Keycloak:credentials:secret"] = "test-secret",
        ["Keycloak:confidential-port"] = "0"
    });
});
```

### Test JWT Configuration

```csharp
services.Configure<JwtBearerOptions>(
    JwtBearerDefaults.AuthenticationScheme,
    options =>
    {
        var signingKey = "YourTestSigningKeyAtLeast32Characters";

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "TestAPI",
            ValidAudience = "TestClient",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(signingKey))
        };
    });
```

## Common Mistakes to Avoid

### Hardcoding secrets

```csharp
// Wrong - secrets in code
services.AddClient("keycloak", client =>
{
    client.ClientSecret = ClientSecret.Parse("hardcoded-secret-value");
});

// Correct - secrets from configuration
var options = config.GetKeycloakOptions<KeycloakAdminClientOptions>();
services.AddClient("keycloak", client =>
{
    client.ClientSecret = ClientSecret.Parse(options.Credentials.Secret);
});
```

### Wrong middleware order

```csharp
// Wrong - authorization before authentication
app.UseAuthorization();
app.UseAuthentication();

// Correct - authentication first
app.UseAuthentication();
app.UseAuthorization();
```

### Direct role checking instead of policies

```csharp
// Wrong - checking roles directly in controller
[HttpGet]
public async Task<IActionResult> GetUsers()
{
    if (!User.HasRoles("admin"))
        return Forbid();
    // ...
}

// Correct - use authorization policies
[HttpGet]
[Authorize(Policy = Policies.AdminOnly)]
public async Task<IActionResult> GetUsers()
{
    // ...
}
```

### Not using UserContext

```csharp
// Wrong - accessing HttpContext directly
[HttpGet]
public async Task<IActionResult> GetMyData()
{
    var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    // ...
}

// Correct - use IUserContext
[HttpGet]
public async Task<IActionResult> GetMyData([FromServices] IUserContext userContext)
{
    var userId = userContext.UserId;
    // ...
}
```

### Missing ProducesResponseType for auth errors

```csharp
// Wrong - missing 401/403 response types
[HttpGet]
[Authorize(Policy = Policies.AdminOnly)]
[ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
public async Task<ActionResult<UserDto>> GetUser(int id)

// Correct - include auth error responses
[HttpGet]
[Authorize(Policy = Policies.AdminOnly)]
[ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public async Task<ActionResult<UserDto>> GetUser(int id)
```
