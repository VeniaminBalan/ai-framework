# CQRS Examples

## Folder Structure (Vertical Slices)

```
Application/
├── Features/
│   ├── Users/
│   │   ├── Commands/
│   │   │   ├── CreateUser/
│   │   │   │   ├── CreateUserCommand.cs
│   │   │   │   ├── CreateUserCommandHandler.cs
│   │   │   │   └── CreateUserCommandValidator.cs
│   │   │   ├── UpdateUser/
│   │   │   │   ├── UpdateUserCommand.cs
│   │   │   │   ├── UpdateUserCommandHandler.cs
│   │   │   │   └── UpdateUserCommandValidator.cs
│   │   │   └── DeleteUser/
│   │   │       ├── DeleteUserCommand.cs
│   │   │       └── DeleteUserCommandHandler.cs
│   │   ├── Queries/
│   │   │   ├── GetUserById/
│   │   │   │   ├── GetUserByIdQuery.cs
│   │   │   │   └── GetUserByIdQueryHandler.cs
│   │   │   └── GetUsers/
│   │   │       ├── GetUsersQuery.cs
│   │   │       ├── GetUsersQueryHandler.cs
│   │   │       └── GetUsersQueryParams.cs
│   │   └── Notifications/
│   │       ├── UserCreatedNotification.cs
│   │       └── Handlers/
│   │           ├── SendWelcomeEmailHandler.cs
│   │           └── AuditUserCreationHandler.cs
│   └── Orders/
│       ├── Commands/
│       ├── Queries/
│       └── Notifications/
├── Interceptors/
│   ├── LoggingInterceptor.cs
│   ├── CommandAuditInterceptor.cs
│   └── QueryCachingInterceptor.cs
```

## Complete Command Example

### CreateUserCommand.cs

```csharp
using Kommand.Abstractions;

namespace Application.Features.Users.Commands.CreateUser;

public record CreateUserCommand(
    string Email,
    string Name,
    string? Department) : ICommand<UserDto>;
```

### CreateUserCommandHandler.cs

```csharp
using Kommand.Abstractions;

namespace Application.Features.Users.Commands.CreateUser;

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IMediator mediator)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
    }

    public async Task<UserDto> HandleAsync(CreateUserCommand command, CancellationToken ct)
    {
        var user = new User
        {
            Email = command.Email,
            Name = command.Name,
            Department = command.Department,
            IsActive = true
        };

        await _userRepository.AddAsync(user);

        await _unitOfWork.InTransactionAsync(async () =>
        {
            // Transaction commits here
        }, ct);

        // Publish domain event after successful creation
        await _mediator.PublishAsync(
            new UserCreatedNotification(user.ExternalId, user.Email, user.Name),
            ct);

        return new UserDto
        {
            Id = user.ExternalId,
            Email = user.Email,
            Name = user.Name,
            Department = user.Department,
            IsActive = user.IsActive
        };
    }
}
```

### CreateUserCommandValidator.cs

```csharp
using Kommand.Abstractions;

namespace Application.Features.Users.Commands.CreateUser;

public class CreateUserCommandValidator : IValidator<CreateUserCommand>
{
    private readonly IUserRepository _userRepository;

    public CreateUserCommandValidator(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ValidationResult> ValidateAsync(
        CreateUserCommand request,
        CancellationToken ct)
    {
        var errors = new List<ValidationError>();

        // Email validation
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add(new ValidationError(nameof(request.Email), "Email is required"));
        }
        else
        {
            if (!IsValidEmail(request.Email))
                errors.Add(new ValidationError(nameof(request.Email), "Email format is invalid"));

            if (await _userRepository.EmailExistsAsync(request.Email, ct))
                errors.Add(new ValidationError(nameof(request.Email), "Email already exists"));
        }

        // Name validation
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add(new ValidationError(nameof(request.Name), "Name is required"));
        }
        else if (request.Name.Length > 100)
        {
            errors.Add(new ValidationError(nameof(request.Name), "Name must not exceed 100 characters"));
        }

        return errors.Count > 0
            ? ValidationResult.Failure(errors)
            : ValidationResult.Success();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
```

## Complete Query Example

### GetUserByIdQuery.cs

```csharp
using Kommand.Abstractions;

namespace Application.Features.Users.Queries.GetUserById;

public record GetUserByIdQuery(string Id) : IQuery<UserDto?>;
```

### GetUserByIdQueryHandler.cs

```csharp
using Kommand.Abstractions;

namespace Application.Features.Users.Queries.GetUserById;

public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDto?>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto?> HandleAsync(GetUserByIdQuery query, CancellationToken ct)
    {
        return await _userRepository.GetUserDtoByIdAsync(query.Id, ct);
    }
}
```

### GetUsersQuery.cs (Paginated)

```csharp
using Kommand.Abstractions;

namespace Application.Features.Users.Queries.GetUsers;

public record GetUsersQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    bool? IsActive = null) : IQuery<PagedResult<UserDto>>;
```

### GetUsersQueryHandler.cs

```csharp
using Kommand.Abstractions;

namespace Application.Features.Users.Queries.GetUsers;

public class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, PagedResult<UserDto>>
{
    private readonly IUserRepository _userRepository;

    public GetUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<PagedResult<UserDto>> HandleAsync(GetUsersQuery query, CancellationToken ct)
    {
        var parameters = new UserQueryParameters
        {
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            SearchTerm = query.SearchTerm,
            IsActive = query.IsActive
        };

        return await _userRepository.GetPagedAsync(parameters, ct);
    }
}
```

## Update Command Example

### UpdateUserCommand.cs

```csharp
using Kommand.Abstractions;

namespace Application.Features.Users.Commands.UpdateUser;

public record UpdateUserCommand(
    string Id,
    string Email,
    string Name,
    string? Department) : ICommand<UserDto>;
```

### UpdateUserCommandHandler.cs

```csharp
using Kommand.Abstractions;

namespace Application.Features.Users.Commands.UpdateUser;

public class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserDto> HandleAsync(UpdateUserCommand command, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(command.Id, ct);

        if (user is null)
            throw new NotFoundException($"User with ID {command.Id} not found");

        user.Email = command.Email;
        user.Name = command.Name;
        user.Department = command.Department;

        await _unitOfWork.InTransactionAsync(async () =>
        {
            // Changes tracked by EF Core, committed here
        }, ct);

        return new UserDto
        {
            Id = user.ExternalId,
            Email = user.Email,
            Name = user.Name,
            Department = user.Department,
            IsActive = user.IsActive
        };
    }
}
```

### UpdateUserCommandValidator.cs

```csharp
using Kommand.Abstractions;

namespace Application.Features.Users.Commands.UpdateUser;

public class UpdateUserCommandValidator : IValidator<UpdateUserCommand>
{
    private readonly IUserRepository _userRepository;

    public UpdateUserCommandValidator(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ValidationResult> ValidateAsync(
        UpdateUserCommand request,
        CancellationToken ct)
    {
        var errors = new List<ValidationError>();

        // Check user exists
        var existingUser = await _userRepository.GetByIdAsync(request.Id, ct);
        if (existingUser is null)
        {
            errors.Add(new ValidationError(nameof(request.Id), "User not found"));
            return ValidationResult.Failure(errors);
        }

        // Email validation
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add(new ValidationError(nameof(request.Email), "Email is required"));
        }
        else if (request.Email != existingUser.Email)
        {
            // Only check uniqueness if email is changing
            if (await _userRepository.EmailExistsAsync(request.Email, ct))
                errors.Add(new ValidationError(nameof(request.Email), "Email already exists"));
        }

        // Name validation
        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add(new ValidationError(nameof(request.Name), "Name is required"));

        return errors.Count > 0
            ? ValidationResult.Failure(errors)
            : ValidationResult.Success();
    }
}
```

## Notification Example

### UserCreatedNotification.cs

```csharp
using Kommand.Abstractions;

namespace Application.Features.Users.Notifications;

public record UserCreatedNotification(
    string UserId,
    string Email,
    string Name) : INotification;
```

### SendWelcomeEmailHandler.cs

```csharp
using Kommand.Abstractions;

namespace Application.Features.Users.Notifications.Handlers;

public class SendWelcomeEmailHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SendWelcomeEmailHandler> _logger;

    public SendWelcomeEmailHandler(
        IEmailService emailService,
        ILogger<SendWelcomeEmailHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task HandleAsync(UserCreatedNotification notification, CancellationToken ct)
    {
        _logger.LogInformation("Sending welcome email to {Email}", notification.Email);

        await _emailService.SendWelcomeEmailAsync(
            notification.Email,
            notification.Name,
            ct);
    }
}
```

### AuditUserCreationHandler.cs

```csharp
using Kommand.Abstractions;

namespace Application.Features.Users.Notifications.Handlers;

public class AuditUserCreationHandler : INotificationHandler<UserCreatedNotification>
{
    private readonly IAuditLogger _auditLogger;

    public AuditUserCreationHandler(IAuditLogger auditLogger)
    {
        _auditLogger = auditLogger;
    }

    public async Task HandleAsync(UserCreatedNotification notification, CancellationToken ct)
    {
        await _auditLogger.LogAsync(
            action: "UserCreated",
            entityType: "User",
            entityId: notification.UserId,
            details: $"User {notification.Name} ({notification.Email}) was created",
            cancellationToken: ct);
    }
}
```

## Interceptor Examples

### LoggingInterceptor.cs

```csharp
using Kommand.Abstractions;

namespace Application.Interceptors;

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
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("Executing {RequestType}", requestName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            _logger.LogInformation(
                "Completed {RequestType} in {ElapsedMs}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Failed {RequestType} after {ElapsedMs}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
```

### CommandAuditInterceptor.cs

```csharp
using Kommand.Abstractions;

namespace Application.Interceptors;

public class CommandAuditInterceptor<TCommand, TResponse> : ICommandInterceptor<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    private readonly IAuditService _auditService;
    private readonly IUserContext _userContext;
    private readonly ILogger<CommandAuditInterceptor<TCommand, TResponse>> _logger;

    public CommandAuditInterceptor(
        IAuditService auditService,
        IUserContext userContext,
        ILogger<CommandAuditInterceptor<TCommand, TResponse>> logger)
    {
        _auditService = auditService;
        _userContext = userContext;
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(
        TCommand command,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var commandName = typeof(TCommand).Name;
        var userId = _userContext.GetCurrentUserId();

        _logger.LogInformation(
            "[AUDIT] User {UserId} executing command {CommandName}",
            userId,
            commandName);

        await _auditService.LogCommandStartAsync(commandName, userId, cancellationToken);

        var response = await next();

        await _auditService.LogCommandSuccessAsync(commandName, userId, cancellationToken);

        _logger.LogInformation(
            "[AUDIT] User {UserId} completed command {CommandName}",
            userId,
            commandName);

        return response;
    }
}
```

### QueryCachingInterceptor.cs

```csharp
using Kommand.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace Application.Interceptors;

public class QueryCachingInterceptor<TQuery, TResponse> : IQueryInterceptor<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<QueryCachingInterceptor<TQuery, TResponse>> _logger;

    public QueryCachingInterceptor(
        IMemoryCache cache,
        ILogger<QueryCachingInterceptor<TQuery, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(
        TQuery query,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Skip caching for certain query types if needed
        if (!ShouldCache(query))
            return await next();

        var cacheKey = GenerateCacheKey(query);

        if (_cache.TryGetValue(cacheKey, out TResponse? cached) && cached is not null)
        {
            _logger.LogDebug("Cache hit for {QueryType}", typeof(TQuery).Name);
            return cached;
        }

        _logger.LogDebug("Cache miss for {QueryType}", typeof(TQuery).Name);

        var response = await next();

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(5))
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

        _cache.Set(cacheKey, response, cacheOptions);

        return response;
    }

    private static bool ShouldCache(TQuery query)
    {
        // Add logic to skip caching for certain queries
        return true;
    }

    private static string GenerateCacheKey(TQuery query)
    {
        return $"{typeof(TQuery).Name}:{JsonSerializer.Serialize(query)}";
    }
}
```

## Controller Integration

### UsersController.cs

```csharp
using Kommand.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken ct)
    {
        var command = new CreateUserCommand(
            request.Email,
            request.Name,
            request.Department);

        var user = await _mediator.SendAsync(command, ct);

        return Created($"/api/users/{user.Id}", user);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var query = new GetUserByIdQuery(id);
        var user = await _mediator.SendAsync(query, ct);

        return user is null ? NotFound() : Ok(user);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var query = new GetUsersQuery(pageNumber, pageSize, search);
        var result = await _mediator.SendAsync(query, ct);

        return Ok(result);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateUserRequest request,
        CancellationToken ct)
    {
        var command = new UpdateUserCommand(
            id,
            request.Email,
            request.Name,
            request.Department);

        var user = await _mediator.SendAsync(command, ct);

        return Ok(user);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var command = new DeleteUserCommand(id);
        await _mediator.SendAsync(command, ct);

        return NoContent();
    }
}
```

## DI Configuration

### KommandConfiguration.cs

```csharp
using Kommand;

namespace Application.Infrastructure.Kommand;

public static class KommandConfiguration
{
    public static IServiceCollection AddKommandMediator(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        services.AddKommand(config =>
        {
            // Register handlers from specified assemblies
            foreach (var assembly in assemblies)
            {
                config.RegisterHandlersFromAssembly(assembly);
            }

            // Add interceptors in execution order (first = outermost)
            config.AddInterceptor(typeof(LoggingInterceptor<,>));
            config.AddInterceptor(typeof(CommandAuditInterceptor<,>));
            config.AddInterceptor(typeof(QueryCachingInterceptor<,>));

            // Enable validation (auto-discovers IValidator<T> implementations)
            config.WithValidation();
        });

        return services;
    }
}
```

### Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Kommand with configuration
builder.Services.AddKommandMediator(typeof(Program).Assembly);

// Optional: Add OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("MyApp"))
    .WithTracing(tracing => tracing
        .AddSource("Kommand")
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddMeter("Kommand")
        .AddOtlpExporter());

var app = builder.Build();
```

## Common Mistakes to Avoid

### Returning Entities from Queries

```csharp
// ❌ Wrong - Returns entity directly
public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, User?>
{
    public async Task<User?> HandleAsync(GetUserByIdQuery query, CancellationToken ct)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.ExternalId == query.Id, ct);
    }
}

// ✅ Correct - Returns DTO with projection
public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDto?>
{
    public async Task<UserDto?> HandleAsync(GetUserByIdQuery query, CancellationToken ct)
    {
        return await _context.Users
            .Where(u => u.ExternalId == query.Id)
            .Select(u => new UserDto
            {
                Id = u.ExternalId,
                Email = u.Email,
                Name = u.Name
            })
            .FirstOrDefaultAsync(ct);
    }
}
```

### Missing CancellationToken

```csharp
// ❌ Wrong - Ignores CancellationToken
public async Task<UserDto> HandleAsync(CreateUserCommand command, CancellationToken ct)
{
    await _repository.AddAsync(user); // Missing ct!
    await _emailService.SendAsync(email); // Missing ct!
    return dto;
}

// ✅ Correct - Passes CancellationToken to all async operations
public async Task<UserDto> HandleAsync(CreateUserCommand command, CancellationToken ct)
{
    await _repository.AddAsync(user, ct);
    await _emailService.SendAsync(email, ct);
    return dto;
}
```

### Business Logic in Controller

```csharp
// ❌ Wrong - Logic in controller
[HttpPost]
public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken ct)
{
    // Validation in controller
    if (await _userRepo.EmailExistsAsync(request.Email, ct))
        return BadRequest("Email exists");

    // Business logic in controller
    var user = new User { Email = request.Email.ToLower(), Name = request.Name };
    await _userRepo.AddAsync(user, ct);

    return Created($"/users/{user.ExternalId}", user);
}

// ✅ Correct - All logic in handler
[HttpPost]
public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken ct)
{
    var command = new CreateUserCommand(request.Email, request.Name);
    var user = await _mediator.SendAsync(command, ct);
    return Created($"/users/{user.Id}", user);
}
```

### Using int Id in Public API

```csharp
// ❌ Wrong - Exposes internal int ID
public record GetUserByIdQuery(int Id) : IQuery<UserDto?>;

// ✅ Correct - Uses ExternalId (string) for public API
public record GetUserByIdQuery(string Id) : IQuery<UserDto?>;
```

### Wrong Interceptor Type

```csharp
// ❌ Wrong - Caching interceptor runs on commands too
public class CachingInterceptor<TRequest, TResponse> : IInterceptor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // This will cache command results, which is incorrect!
}

// ✅ Correct - Query-only caching interceptor
public class QueryCachingInterceptor<TQuery, TResponse> : IQueryInterceptor<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    // Only caches query results
}
```
