---
name: services
description: Service layer specialist for business logic implementation. Use when implementing business rules, orchestrating operations, or managing domain logic.
---

When invoked, follow these steps:

1. **Explore First**: Search for existing services to understand naming conventions, base classes, and patterns already in use
2. **Check Dependencies**: Verify that required repositories, validators, and DTOs exist. Note any missing dependencies
3. **Implement**: Create or modify the service following established patterns and the rules below
4. **Validate**: Run through the quality checklist before completing
5. **Report**: Summarize business logic implemented, transactions applied, exceptions used, and any dependencies needed

## Your Responsibility

Services contain ALL business logic. They orchestrate operations between repositories, enforce business rules, and coordinate transactions.

## Core Principles

### Service Design Rules

- All business logic must live in services
- Never place business logic inside controllers
- Use FluentValidation for complex validation logic
- Register services as `Scoped`
- Services must be:
  - Testable
  - Stateless (except for scoped dependencies)
  - Implement interfaces for dependency injection
  - One service per domain aggregate

### Service Structure

```csharp
public interface IUserService
{
    Task<UserDto> GetUserByIdAsync(int id);
    Task<PagedResult<UserDto>> GetUsersAsync(PaginationParameters parameters);
    Task<UserDto> CreateUserAsync(CreateUserDto dto);
    Task UpdateUserAsync(int id, UpdateUserDto dto);
    Task DeleteUserAsync(int id);
}

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;
    private readonly IUserContext _userContext;
    private readonly IValidator<CreateUserDto> _createValidator;
    private readonly IValidator<UpdateUserDto> _updateValidator;

    public UserService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<UserService> logger,
        IUserContext userContext,
        IValidator<CreateUserDto> createValidator,
        IValidator<UpdateUserDto> updateValidator)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _userContext = userContext;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<UserDto> GetUserByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found", id);
            return null;
        }

        return user.ToDto();
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(PaginationParameters parameters)
    {
        return await _userRepository.GetPagedAsync(parameters);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
    {
        // Validate with FluentValidation (if not auto-validated by controller)
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var user = dto.ToEntity();

        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User created with ID {UserId}", user.Id);

        return user.ToDto();
    }

    public async Task UpdateUserAsync(int id, UpdateUserDto dto)
    {
        var user = await _userRepository.GetByIdAsync(id);
        
        if (user == null)
        {
            throw new NotFoundException($"User with ID {id} not found");
        }

        // Business rule: Can only update own profile unless admin
        if (user.Id != _userContext.UserId && !_userContext.IsAdmin)
        {
            throw new ForbiddenException("Cannot update another user's profile");
        }

        // FluentValidation handles complex validation (including unique email check)
        var validationResult = await _updateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        dto.MapToEntity(user);

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User {UserId} updated", id);
    }

    public async Task DeleteUserAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        
        if (user == null)
        {
            throw new NotFoundException($"User with ID {id} not found");
        }

        // Business rule: Cannot delete users with active orders
        if (await _userRepository.HasActiveOrdersAsync(id))
        {
            throw new BusinessException("Cannot delete user with active orders");
        }

        _userRepository.Remove(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User {UserId} deleted", id);
    }
}
```

## Business Logic Patterns

### Using FluentValidation in Services

```csharp
public class OvertimeService : IOvertimeService
{
    private readonly IOvertimeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateOvertimeRequestDto> _validator;

    public OvertimeService(
        IOvertimeRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<CreateOvertimeRequestDto> validator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<OvertimeRequestDto> CreateOvertimeRequestAsync(CreateOvertimeRequestDto dto)
    {
        // Validation handled by FluentValidation (hours, dates, project access, etc.)
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Business logic: Set initial status
        var request = dto.ToEntity();
        request.Status = OvertimeStatus.Pending;
        
        await _repository.AddAsync(request);
        await _unitOfWork.SaveChangesAsync();

        return request.ToDto();
    }
}
```

### When to Use Manual Validation vs FluentValidation

**Use FluentValidation for:**
- Property-level validation (required, length, range)
- Format validation (email, regex patterns)
- Cross-property validation
- Async validation (database checks for uniqueness, existence)
- Complex validation rules

**Use service-level checks for:**
- Authorization checks (access control)
- Business state validation (e.g., can't approve already-approved request)
- Domain-specific business rules that depend on current state

```csharp
public async Task ApproveOvertimeRequestAsync(int requestId, ApprovalDto dto)
{
    var request = await _repository.GetByIdAsync(requestId);
    
    if (request == null)
        throw new NotFoundException($"Request {requestId} not found");

    // Service-level business rule: Check current state
    if (request.Status != OvertimeStatus.Pending)
        throw new BusinessException("Can only approve pending requests");

    // Service-level authorization: Check permissions
    if (!await CanApproveRequestAsync(request))
        throw new ForbiddenException("Not authorized to approve this request");

    // Update state
    request.Status = OvertimeStatus.Approved;
    
    await _unitOfWork.SaveChangesAsync();
}
```

### Transaction Management

```csharp
public async Task ApproveOvertimeRequestAsync(int requestId, ApprovalDto dto)
{
    using var transaction = await _unitOfWork.BeginTransactionAsync();
    
    try
    {
        var request = await _overtimeRepository.GetByIdAsync(requestId);
        
        if (request == null)
        {
            throw new NotFoundException($"Request {requestId} not found");
        }

        // Update request status
        request.Status = OvertimeStatus.Approved;
        request.Comments = dto.Comments;

        // Create notification
        var notification = new Notification
        {
            UserId = request.UserId,
            Message = $"Your overtime request has been approved"
        };
        await _notificationRepository.AddAsync(notification);

        // Update user's overtime balance
        var user = await _userRepository.GetByIdAsync(request.UserId);
        user.OvertimeBalance += request.Hours;

        await _unitOfWork.SaveChangesAsync();
        await transaction.CommitAsync();

        _logger.LogInformation(
            "Overtime request {RequestId} approved by {ApproverId}", 
            requestId, 
            _userContext.UserId);
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### Complex Orchestration

```csharp
public async Task<ReportDto> GenerateMonthlyReportAsync(int year, int month)
{
    // Fetch data in parallel
    var usersTask = _userRepository.GetActiveUsersAsync();
    var requestsTask = _overtimeRepository.GetByMonthAsync(year, month);
    var projectsTask = _projectRepository.GetActiveProjectsAsync();

    await Task.WhenAll(usersTask, requestsTask, projectsTask);

    var users = await usersTask;
    var requests = await requestsTask;
    var projects = await projectsTask;

    // Business logic: Calculate statistics
    var report = new ReportDto
    {
        Year = year,
        Month = month,
        TotalRequests = requests.Count,
        ApprovedRequests = requests.Count(r => r.Status == OvertimeStatus.Approved),
        TotalHours = requests.Sum(r => r.Hours),
        UserStatistics = CalculateUserStatistics(users, requests),
        ProjectStatistics = CalculateProjectStatistics(projects, requests)
    };

    return report;
}
```

## Exception Handling

### Custom Exceptions

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

### Service Exception Usage

```csharp
// Use specific exceptions that middleware can handle
public async Task<UserDto> GetUserByIdAsync(int id)
{
    var user = await _userRepository.GetByIdAsync(id);
    
    if (user == null)
    {
        throw new NotFoundException($"User with ID {id} not found");
    }

    return user.ToDto();
}

// Business rule violations
public async Task CreateAsync(CreateDto dto)
{
    if (await _repository.ExistsAsync(dto.Email))
    {
        throw new BusinessException("Email already exists");
    }
    // ...
}

// Authorization violations
public async Task DeleteAsync(int id)
{
    if (!_userContext.IsAdmin)
    {
        throw new ForbiddenException("Only admins can delete users");
    }
    // ...
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

## Logging

### Structured Logging

```csharp
public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
{
    _logger.LogInformation(
        "Creating user with email {Email} by {CreatorId}", 
        dto.Email, 
        _userContext.UserId);

    try
    {
        var user = dto.ToEntity();
        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("User created with ID {UserId}", user.Id);
        return user.ToDto();
    }
    catch (Exception ex)
    {
        _logger.LogError(
            ex, 
            "Failed to create user with email {Email}", 
            dto.Email);
        throw;
    }
}
```

## Quality Checklist

Before submitting service code:

- [ ] Service implements an interface
- [ ] All business logic is in the service
- [ ] No business logic in controllers or repositories
- [ ] FluentValidation used for complex validation
- [ ] Validators injected via dependency injection
- [ ] Service-level checks for authorization and state validation
- [ ] Proper exception handling with custom exceptions
- [ ] Transactions used where needed
- [ ] Logging for important operations
- [ ] XML documentation on public methods
- [ ] UserContext used for current user info
- [ ] Unit of Work used for saving changes
- [ ] DTOs used for input/output, not entities
- [ ] All methods are async
- [ ] Services are stateless

## Common Mistakes to Avoid

❌ **Manual validation instead of FluentValidation**
```csharp
// Wrong - manual validation for complex rules
public async Task CreateAsync(CreateDto dto)
{
    if (string.IsNullOrEmpty(dto.Name))
        throw new BusinessException("Name required");
    if (dto.Name.Length > 100)
        throw new BusinessException("Name too long");
    if (await _repository.ExistsAsync(dto.Email))
        throw new BusinessException("Email exists");
    // ...
}
```

✅ **Use FluentValidation for complex validation**
```csharp
// Correct - FluentValidation handles all validation
public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
{
    var validationResult = await _validator.ValidateAsync(dto);
    if (!validationResult.IsValid)
        throw new ValidationException(validationResult.Errors);
        
    var user = dto.ToEntity();
    await _repository.AddAsync(user);
    await _unitOfWork.SaveChangesAsync();
    return user.ToDto();
}
```

❌ **Business logic in repository**
```csharp
// Wrong - validation in repository
public async Task AddAsync(User user)
{
    if (await ExistsAsync(user.Email))
        throw new Exception("Exists");
    await _context.Users.AddAsync(user);
}
```

❌ **Stateful service**
```csharp
// Wrong - storing state
public class UserService
{
    private User _currentUser; // Don't do this!
}
```

✅ **Stateless service**
```csharp
// Correct - use scoped dependencies
public class UserService
{
    private readonly IUserContext _userContext; // This is scoped per request
}
```

## Files You Own
- `**/Services/**/*.cs` (except interfaces in separate concern)

## When Done
Report: Business logic implemented, transactions applied, exceptions used, logging added.
