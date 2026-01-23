# Services Examples

## Service Interface

```csharp
public interface IUserService
{
    Task<UserDto> GetUserByIdAsync(int id);
    Task<PagedResult<UserDto>> GetUsersAsync(PaginationParameters parameters);
    Task<UserDto> CreateUserAsync(CreateUserDto dto);
    Task UpdateUserAsync(int id, UpdateUserDto dto);
    Task DeleteUserAsync(int id);
}
```

## Service Implementation

```csharp
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
        // Validate with FluentValidation
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

        // FluentValidation handles complex validation
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

## Using FluentValidation in Services

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
        // Validation handled by FluentValidation
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

## Service-Level Business Rule Checks

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

## Transaction Management

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

## Complex Orchestration

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

## Structured Logging

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

## Common Mistakes to Avoid

### ❌ Wrong: Manual validation instead of FluentValidation

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

### ✅ Correct: Use FluentValidation

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

### ❌ Wrong: Business logic in repository

```csharp
// Wrong - validation in repository
public async Task AddAsync(User user)
{
    if (await ExistsAsync(user.Email))
        throw new Exception("Exists");
    await _context.Users.AddAsync(user);
}
```

### ❌ Wrong: Stateful service

```csharp
// Wrong - storing state
public class UserService
{
    private User _currentUser; // Don't do this!
}
```

### ✅ Correct: Stateless service

```csharp
// Correct - use scoped dependencies
public class UserService
{
    private readonly IUserContext _userContext; // This is scoped per request
}
```
