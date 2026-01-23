---
name: middleware
description: Middleware and cross-cutting concerns specialist. Use when implementing global exception handling, authentication context, request logging, or other middleware.
---

When invoked, follow these steps:

1. **Explore First**: Search for existing middleware to understand the pipeline order and patterns in use
2. **Check Dependencies**: Identify where in the pipeline the new middleware should be registered
3. **Implement**: Create or modify middleware following established patterns and the rules below
4. **Validate**: Ensure middleware is properly registered in the correct order
5. **Report**: Summarize middleware created/modified and pipeline registration changes

## Your Responsibility

Implement cross-cutting concerns that apply globally across the application: exception handling, logging, authentication context, request/response manipulation.

## Middleware Patterns

### Global Exception Handling Middleware

```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            BusinessException => (StatusCodes.Status400BadRequest, exception.Message),
            ForbiddenException => (StatusCodes.Status403Forbidden, exception.Message),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, exception.Message),
            ValidationException => (StatusCodes.Status400BadRequest, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "An internal server error occurred")
        };

        context.Response.StatusCode = statusCode;

        var response = new ErrorResponse
        {
            StatusCode = statusCode,
            Message = message,
            Timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}

public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### User Context Middleware

```csharp
public class UserContextMiddleware
{
    private readonly RequestDelegate _next;

    public UserContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUserContext userContext)
    {
        // Extract user information from JWT or headers
        var userId = ExtractUserId(context);
        var organizationId = ExtractOrganizationId(context);
        var roles = ExtractRoles(context);

        // Populate UserContext (scoped service)
        userContext.UserId = userId;
        userContext.OrganizationId = organizationId;
        userContext.Roles = roles;
        userContext.IsAuthenticated = userId.HasValue;

        await _next(context);
    }

    private int? ExtractUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private int? ExtractOrganizationId(HttpContext context)
    {
        var orgIdClaim = context.User.FindFirst("OrganizationId")?.Value;
        return int.TryParse(orgIdClaim, out var orgId) ? orgId : null;
    }

    private List<string> ExtractRoles(HttpContext context)
    {
        return context.User.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();
    }
}

// UserContext service (scoped)
public interface IUserContext
{
    int? UserId { get; set; }
    int? OrganizationId { get; set; }
    List<string> Roles { get; set; }
    bool IsAuthenticated { get; set; }
    bool IsAdmin => Roles?.Contains("Admin") ?? false;
}

public class UserContext : IUserContext
{
    public int? UserId { get; set; }
    public int? OrganizationId { get; set; }
    public List<string> Roles { get; set; } = new();
    public bool IsAuthenticated { get; set; }
    public bool IsAdmin => Roles?.Contains("Admin") ?? false;
}
```

### Request Logging Middleware

```csharp
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;

        _logger.LogInformation(
            "Request {Method} {Path} started at {StartTime}",
            context.Request.Method,
            context.Request.Path,
            startTime);

        await _next(context);

        var duration = DateTime.UtcNow - startTime;

        _logger.LogInformation(
            "Request {Method} {Path} completed in {Duration}ms with status {StatusCode}",
            context.Request.Method,
            context.Request.Path,
            duration.TotalMilliseconds,
            context.Response.StatusCode);
    }
}
```

### Request Validation Middleware

```csharp
public class RequestValidationMiddleware
{
    private readonly RequestDelegate _next;

    public RequestValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Example: Validate required headers
        if (!context.Request.Headers.ContainsKey("X-Api-Version"))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new ErrorResponse
            {
                StatusCode = 400,
                Message = "X-Api-Version header is required",
                Timestamp = DateTime.UtcNow
            });
            return;
        }

        await _next(context);
    }
}
```

### CORS Middleware Configuration

```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://app.example.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Use in pipeline
app.UseCors("AllowFrontend");
```

## Middleware Registration

### Program.cs Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddScoped<IUserContext, UserContext>();

var app = builder.Build();

// Configure middleware pipeline (order matters!)
app.UseMiddleware<ExceptionHandlingMiddleware>(); // First - catch all exceptions
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseRouting();

app.UseCors("AllowFrontend");

app.UseAuthentication(); // Before UserContext
app.UseAuthorization();

app.UseMiddleware<UserContextMiddleware>(); // After authentication

app.MapControllers();

app.Run();
```

## Custom Exception Types

```csharp
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}
```

## Using UserContext in Services

```csharp
public class OvertimeService : IOvertimeService
{
    private readonly IOvertimeRepository _repository;
    private readonly IUserContext _userContext;

    public OvertimeService(
        IOvertimeRepository repository,
        IUserContext userContext)
    {
        _repository = repository;
        _userContext = userContext;
    }

    public async Task<OvertimeRequestDto> CreateAsync(CreateOvertimeRequestDto dto)
    {
        // Use current user from context
        var request = dto.ToEntity();
        request.CreatedBy = _userContext.UserId.Value;
        request.OrganizationId = _userContext.OrganizationId.Value;
        
        await _repository.AddAsync(request);
        return request.ToDto();
    }

    public async Task ApproveAsync(int id)
    {
        // Check permissions using context
        if (!_userContext.IsAdmin)
        {
            throw new ForbiddenException("Only admins can approve requests");
        }

        var request = await _repository.GetByIdAsync(id);
        request.ApprovedBy = _userContext.UserId.Value;
        request.Status = OvertimeStatus.Approved;
    }
}
```

## Quality Checklist

Before submitting middleware code:

- [ ] Middleware order is correct in Program.cs
- [ ] Exception handling is first in pipeline
- [ ] UserContext populated after authentication
- [ ] UserContext registered as Scoped
- [ ] All exceptions mapped to proper status codes
- [ ] Logging includes important context
- [ ] No business logic in middleware
- [ ] Middleware doesn't block request unnecessarily
- [ ] Error responses are consistent

## Files You Own
- `**/Middleware/**/*.cs`
- Context services (IUserContext, UserContext)
- Exception classes

## When Done
Report: Middleware implemented, error handling configured, context available, pipeline order verified.
