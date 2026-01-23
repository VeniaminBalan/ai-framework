# Middleware Reference

Detailed rules and conventions for ASP.NET Core middleware.

## Middleware Pipeline Order

Order matters! Configure in Program.cs in this sequence:

1. **ExceptionHandlingMiddleware** - First, catches all exceptions
2. **RequestLoggingMiddleware** - Log all requests
3. **UseRouting()** - Built-in routing
4. **UseCors()** - CORS policy
5. **UseAuthentication()** - Authentication
6. **UseAuthorization()** - Authorization
7. **UserContextMiddleware** - After authentication
8. **MapControllers()** - Endpoint mapping

## Middleware Registration

```csharp
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

## Exception to Status Code Mapping

| Exception Type | Status Code |
|----------------|-------------|
| NotFoundException | 404 Not Found |
| BusinessException | 400 Bad Request |
| ValidationException | 400 Bad Request |
| UnauthorizedException | 401 Unauthorized |
| ForbiddenException | 403 Forbidden |
| Any other exception | 500 Internal Server Error |

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

## Error Response Format

```csharp
public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
}
```

## UserContext Service

- Must be registered as **Scoped**
- Populated by middleware after authentication
- Available for injection in services

```csharp
// Registration in Program.cs
builder.Services.AddScoped<IUserContext, UserContext>();
```

## CORS Configuration

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
```
