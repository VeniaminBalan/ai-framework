# Services Reference

Detailed rules and conventions for service layer implementation.

## Service Design Rules

- All business logic must live in services
- Never place business logic inside controllers
- Use FluentValidation for complex validation logic
- Register services as `Scoped`
- Services must be:
  - Testable
  - Stateless (except for scoped dependencies)
  - Implement interfaces for dependency injection
  - One service per domain aggregate

## When to Use Manual Validation vs FluentValidation

### Use FluentValidation for:
- Property-level validation (required, length, range)
- Format validation (email, regex patterns)
- Cross-property validation
- Async validation (database checks for uniqueness, existence)
- Complex validation rules

### Use service-level checks for:
- Authorization checks (access control)
- Business state validation (e.g., can't approve already-approved request)
- Domain-specific business rules that depend on current state

## Exception Handling

### Custom Exceptions

| Exception Type | When to Use |
|----------------|-------------|
| NotFoundException | Resource doesn't exist |
| BusinessException | Business rule violation |
| ForbiddenException | Authorization failure |
| ValidationException | Validation errors (from FluentValidation) |

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
```

## Dependency Injection

### Service Registration

```csharp
// Program.cs
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IOvertimeService, OvertimeService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
```

## Logging Guidelines

### Structured Logging

- Log important operations (create, update, delete)
- Include relevant context (IDs, user info)
- Log errors with exception details
- Use appropriate log levels

```csharp
_logger.LogInformation("User created with ID {UserId}", user.Id);
_logger.LogWarning("User with ID {UserId} not found", id);
_logger.LogError(ex, "Failed to create user with email {Email}", dto.Email);
```

## Common Mistakes to Avoid

### ❌ Manual validation instead of FluentValidation
Use FluentValidation for complex validation rules instead of manual if/throw checks.

### ❌ Business logic in repository
Keep repositories focused on data access only.

### ❌ Stateful service
Never store state in service fields. Use scoped dependencies like IUserContext.

### ❌ Returning entities instead of DTOs
Always convert entities to DTOs before returning from services.
