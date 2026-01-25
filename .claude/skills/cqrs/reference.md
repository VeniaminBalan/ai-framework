# CQRS Reference

Detailed rules and conventions for CQRS implementation using the Kommand mediator library.

## Commands

### Command Definition

Commands represent write operations that modify state. They implement `ICommand<TResponse>`.

```csharp
using Kommand.Abstractions;

// Command with response
public record CreateUserCommand(string Email, string Name) : ICommand<UserDto>;

// Command for update (uses ExternalId for public API)
public record UpdateUserCommand(string Id, string Email, string Name) : ICommand<UserDto>;

// Command that returns simple result
public record DeleteUserCommand(string Id) : ICommand<bool>;
```

### Command Naming Convention

- Use imperative verb: `Create`, `Update`, `Delete`, `Approve`, `Cancel`, `Submit`
- Suffix with `Command`
- Examples: `CreateUserCommand`, `ApproveOrderCommand`, `CancelSubscriptionCommand`

### Command Properties

- Use records for immutability
- Include only data needed for the operation
- Use `ExternalId` (string) for entity references in public API
- Avoid nesting complex objects (use flat structure)

## Queries

### Query Definition

Queries represent read operations that retrieve data. They implement `IQuery<TResponse>`.

```csharp
using Kommand.Abstractions;

// Single entity query (uses ExternalId)
public record GetUserByIdQuery(string Id) : IQuery<UserDto?>;

// Collection query with parameters
public record GetUsersQuery(int PageNumber, int PageSize, string? SearchTerm) : IQuery<PagedResult<UserDto>>;

// Filtered query
public record GetActiveOrdersQuery(string CustomerId) : IQuery<List<OrderDto>>;
```

### Query Naming Convention

- Use `Get` prefix for retrieval
- Suffix with `Query`
- Examples: `GetUserByIdQuery`, `GetActiveOrdersQuery`, `GetDashboardStatsQuery`

### Query Properties

- Include filtering, sorting, and pagination parameters
- Use `ExternalId` (string) for entity lookups
- Return DTOs, never entities

## Handlers

### Command Handler

```csharp
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, UserDto>
{
    private readonly IUserRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserCommandHandler(IUserRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserDto> HandleAsync(CreateUserCommand command, CancellationToken ct)
    {
        var user = new User
        {
            Email = command.Email,
            Name = command.Name
        };

        await _repository.AddAsync(user);

        await _unitOfWork.InTransactionAsync(async () =>
        {
            // Transaction commits here
        }, ct);

        return new UserDto
        {
            Id = user.ExternalId,
            Email = user.Email,
            Name = user.Name
        };
    }
}
```

### Query Handler

```csharp
public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDto?>
{
    private readonly IUserRepository _repository;

    public GetUserByIdQueryHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserDto?> HandleAsync(GetUserByIdQuery query, CancellationToken ct)
    {
        return await _repository.GetUserDtoByIdAsync(query.Id, ct);
    }
}
```

### Handler Rules

- **Scoped Lifetime**: Handlers are registered as Scoped by default (for DbContext injection)
- **Single Responsibility**: One handler per command/query
- **No Business Logic in Controller**: All logic belongs in handler or service
- **Use Repository Pattern**: Never inject DbContext directly
- **Always Use CancellationToken**: Pass `ct` to all async operations
- **Transactions**: Use `IUnitOfWork.InTransactionAsync()` for multi-step operations

## Validators

### Kommand Validation (Built-in)

Kommand has its own validation system with async support and database access.

```csharp
public class CreateUserCommandValidator : IValidator<CreateUserCommand>
{
    private readonly IUserRepository _repository;

    public CreateUserCommandValidator(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<ValidationResult> ValidateAsync(
        CreateUserCommand request,
        CancellationToken ct)
    {
        var errors = new List<ValidationError>();

        // Basic validation
        if (string.IsNullOrWhiteSpace(request.Email))
            errors.Add(new ValidationError(nameof(request.Email), "Email is required"));

        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add(new ValidationError(nameof(request.Name), "Name is required"));

        // Async database check
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            if (await _repository.EmailExistsAsync(request.Email, ct))
                errors.Add(new ValidationError(nameof(request.Email), "Email already exists"));
        }

        return errors.Count > 0
            ? ValidationResult.Failure(errors)
            : ValidationResult.Success();
    }
}
```

### Validator Rules

- **Auto-Discovery**: Validators are automatically registered from assemblies
- **Runs Before Handler**: Validation executes before handler is called
- **Async Database Access**: Can inject repositories for existence checks
- **Clear Error Messages**: Messages should be actionable
- **ValidationResult**: Return `Success()` or `Failure(errors)`

### Enable Validation

```csharp
builder.Services.AddKommand(config =>
{
    config.RegisterHandlersFromAssembly(typeof(Program).Assembly);
    config.WithValidation(); // Enable auto-discovery of validators
});
```

## Interceptors

### Interceptor Types

| Interface | Constraint | Use Case |
|-----------|------------|----------|
| `IInterceptor<TRequest, TResponse>` | `IRequest<TResponse>` | Both commands and queries |
| `ICommandInterceptor<TCommand, TResponse>` | `ICommand<TResponse>` | Commands only (write operations) |
| `IQueryInterceptor<TQuery, TResponse>` | `IQuery<TResponse>` | Queries only (read operations) |

### General Interceptor (All Requests)

```csharp
public class LoggingInterceptor<TRequest, TResponse> : IInterceptor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingInterceptor<TRequest, TResponse>> _logger;

    public LoggingInterceptor(ILogger<LoggingInterceptor<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing {RequestType}", typeof(TRequest).Name);
        var response = await next();
        _logger.LogInformation("Completed {RequestType}", typeof(TRequest).Name);
        return response;
    }
}
```

### Command-Only Interceptor (Audit/Transactions)

```csharp
public class CommandAuditInterceptor<TCommand, TResponse> : ICommandInterceptor<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    private readonly IAuditService _auditService;
    private readonly IUserContext _userContext;

    public CommandAuditInterceptor(IAuditService auditService, IUserContext userContext)
    {
        _auditService = auditService;
        _userContext = userContext;
    }

    public async Task<TResponse> HandleAsync(
        TCommand command,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var commandName = typeof(TCommand).Name;
        var userId = _userContext.GetCurrentUserId();

        await _auditService.LogCommandStartAsync(commandName, userId, cancellationToken);

        var response = await next();

        await _auditService.LogCommandSuccessAsync(commandName, userId, cancellationToken);

        return response;
    }
}
```

### Query-Only Interceptor (Caching)

```csharp
public class QueryCachingInterceptor<TQuery, TResponse> : IQueryInterceptor<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    private readonly IMemoryCache _cache;

    public QueryCachingInterceptor(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<TResponse> HandleAsync(
        TQuery query,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var cacheKey = GenerateCacheKey(query);

        if (_cache.TryGetValue(cacheKey, out TResponse? cached) && cached is not null)
            return cached;

        var response = await next();

        _cache.Set(cacheKey, response, TimeSpan.FromMinutes(5));

        return response;
    }

    private static string GenerateCacheKey(TQuery query)
    {
        return $"{typeof(TQuery).Name}:{JsonSerializer.Serialize(query)}";
    }
}
```

### Interceptor Registration

```csharp
builder.Services.AddKommand(config =>
{
    config.RegisterHandlersFromAssembly(typeof(Program).Assembly);

    // Applies to ALL requests (commands + queries)
    config.AddInterceptor(typeof(LoggingInterceptor<,>));

    // ONLY applies to commands
    config.AddInterceptor(typeof(CommandAuditInterceptor<,>));

    // ONLY applies to queries
    config.AddInterceptor(typeof(QueryCachingInterceptor<,>));

    config.WithValidation();
});
```

## Notifications (Domain Events)

### Notification Definition

Notifications allow publishing domain events to multiple handlers.

```csharp
public record UserCreatedNotification(string UserId, string Email) : INotification;

public record OrderCompletedNotification(string OrderId, decimal TotalAmount) : INotification;
```

### Notification Handlers

```csharp
public class SendWelcomeEmailHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly IEmailService _emailService;

    public SendWelcomeEmailHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task HandleAsync(UserCreatedNotification notification, CancellationToken ct)
    {
        await _emailService.SendWelcomeEmailAsync(notification.Email, ct);
    }
}

public class AuditUserCreationHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly IAuditLogger _auditLogger;

    public AuditUserCreationHandler(IAuditLogger auditLogger)
    {
        _auditLogger = auditLogger;
    }

    public async Task HandleAsync(UserCreatedNotification notification, CancellationToken ct)
    {
        await _auditLogger.LogAsync($"User created: {notification.UserId}", ct);
    }
}
```

### Publishing Notifications

```csharp
// In command handler
public async Task<UserDto> HandleAsync(CreateUserCommand command, CancellationToken ct)
{
    var user = new User { Email = command.Email, Name = command.Name };
    await _repository.AddAsync(user);

    // Publish notification after successful operation
    await _mediator.PublishAsync(
        new UserCreatedNotification(user.ExternalId, user.Email),
        ct);

    return new UserDto { Id = user.ExternalId, Email = user.Email };
}
```

## Mediator Usage

### Controller Integration

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken ct)
    {
        var command = new CreateUserCommand(request.Email, request.Name);
        var user = await _mediator.SendAsync(command, ct);
        return Created($"/api/users/{user.Id}", user);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var query = new GetUserByIdQuery(id);
        var user = await _mediator.SendAsync(query, ct);
        return user is null ? NotFound() : Ok(user);
    }
}
```

## Dependency Registration

```csharp
// Program.cs
builder.Services.AddKommand(config =>
{
    // Auto-discover handlers and validators from assembly
    config.RegisterHandlersFromAssembly(typeof(Program).Assembly);

    // Add interceptors in order (first registered = outermost)
    config.AddInterceptor(typeof(LoggingInterceptor<,>));
    config.AddInterceptor(typeof(CommandAuditInterceptor<,>));
    config.AddInterceptor(typeof(QueryCachingInterceptor<,>));

    // Enable validation
    config.WithValidation();
});
```

## OpenTelemetry Integration

Kommand includes built-in OpenTelemetry support with zero configuration.

```csharp
// Optional: Configure exporter
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("MyApp"))
    .WithTracing(tracing => tracing
        .AddSource("Kommand")
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics
        .AddMeter("Kommand")
        .AddConsoleExporter());
```

### What Kommand Exports

- **Traces**: Activity spans for each command/query with detailed tags
- **Metrics**: Request counts, durations, and validation failures
- **When OTEL not configured**: ~10-50ns overhead per request (negligible)

## ExternalId Strategy

### Rule

**Use ExternalId (string/Guid) for all public-facing command/query parameters. Use int Id internally in repositories.**

### Implementation

```csharp
// Command uses ExternalId
public record UpdateUserCommand(string Id, string Name) : ICommand<UserDto>;

// Handler converts to int Id for repository
public async Task<UserDto> HandleAsync(UpdateUserCommand command, CancellationToken ct)
{
    var user = await _repository.GetByIdAsync(command.Id, ct); // Repository uses ExternalId

    if (user is null)
        throw new NotFoundException($"User {command.Id} not found");

    user.Name = command.Name;

    await _unitOfWork.InTransactionAsync(async () =>
    {
        // Save changes
    }, ct);

    return new UserDto { Id = user.ExternalId, Name = user.Name };
}
```
