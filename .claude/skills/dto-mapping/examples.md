# DTO Mapping Examples

## Response DTOs

```csharp
// Response DTO - What API returns
public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; }
}

// Detailed response DTO - For single resource
public class UserDetailDto : UserDto
{
    public string PhoneNumber { get; set; }
    public AddressDto Address { get; set; }
    public List<OrderDto> RecentOrders { get; set; }
}
```

## Request DTOs

```csharp
// Create DTO - For POST requests
public class CreateUserDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [MinLength(8)]
    public string Password { get; set; }
}

// Update DTO - For PUT/PATCH requests
public class UpdateUserDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [EmailAddress]
    public string Email { get; set; }

    public string PhoneNumber { get; set; }
}
```

## Nested DTOs

```csharp
public class OvertimeRequestDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public decimal Hours { get; set; }
    public string Status { get; set; }

    // Nested DTOs
    public UserSummaryDto User { get; set; }
    public ProjectSummaryDto Project { get; set; }
}

// Summary DTOs for nested objects
public class UserSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class ProjectSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
}
```

## Mapping Extension Methods

```csharp
// File: Mappings/UserMappingExtensions.cs

public static class UserMappingExtensions
{
    // Entity to DTO
    public static UserDto ToDto(this User entity)
    {
        if (entity == null) return null;

        return new UserDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Email = entity.Email,
            CreatedAt = entity.CreatedAt,
            Roles = entity.UserRoles?.Select(ur => ur.Role.Name).ToList()
        };
    }

    // Entity to detailed DTO
    public static UserDetailDto ToDetailDto(this User entity)
    {
        if (entity == null) return null;

        return new UserDetailDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Email = entity.Email,
            CreatedAt = entity.CreatedAt,
            Roles = entity.UserRoles?.Select(ur => ur.Role.Name).ToList(),
            PhoneNumber = entity.PhoneNumber,
            Address = entity.Address?.ToDto(),
            RecentOrders = entity.Orders?
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select(o => o.ToDto())
                .ToList()
        };
    }

    // Create DTO to Entity
    public static User ToEntity(this CreateUserDto dto)
    {
        if (dto == null) return null;

        return new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = dto.Password // Will be hashed by service
        };
    }

    // Update DTO to Entity (for updating existing)
    public static void MapToEntity(this UpdateUserDto dto, User entity)
    {
        if (dto == null || entity == null) return;

        entity.Name = dto.Name;
        entity.Email = dto.Email ?? entity.Email;
        entity.PhoneNumber = dto.PhoneNumber;
    }
}
```

## EF Core Projection for Collections

```csharp
// ✅ CORRECT - Use projection for collections
public async Task<List<UserDto>> GetAllUsersAsync()
{
    return await _context.Users
        .OrderBy(u => u.Name)
        .Select(u => new UserDto
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            CreatedAt = u.CreatedAt,
            Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
        })
        .ToListAsync();
}

// ❌ WRONG - Don't load entities then map for collections
public async Task<List<UserDto>> GetAllUsersAsync()
{
    var users = await _context.Users.ToListAsync();
    return users.ToDtoList(); // Inefficient - loads full entities
}
```

## Repository Usage

```csharp
public async Task<PagedResult<UserDto>> GetPagedAsync(PaginationParameters parameters)
{
    var query = _context.Users
        .OrderBy(u => u.Name);

    var totalCount = await query.CountAsync();

    var users = await query
        .Skip((parameters.PageNumber - 1) * parameters.PageSize)
        .Take(parameters.PageSize)
        .Select(u => new UserDto
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            CreatedAt = u.CreatedAt,
            Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
        })
        .ToListAsync();

    return new PagedResult<UserDto>
    {
        Items = users,
        TotalCount = totalCount,
        PageNumber = parameters.PageNumber,
        PageSize = parameters.PageSize
    };
}
```

## Service Usage

```csharp
public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
{
    // DTO to Entity
    var user = dto.ToEntity();

    // Business logic
    user.CreatedAt = DateTime.UtcNow;
    user.CreatedBy = _userContext.UserId;

    await _repository.AddAsync(user);
    await _unitOfWork.SaveChangesAsync();

    // Entity to DTO
    return user.ToDto();
}

public async Task UpdateUserAsync(int id, UpdateUserDto dto)
{
    var user = await _repository.GetByIdAsync(id);

    // Update DTO to Entity
    dto.MapToEntity(user);

    // Business logic
    user.UpdatedAt = DateTime.UtcNow;
    user.UpdatedBy = _userContext.UserId;

    await _unitOfWork.SaveChangesAsync();
}
```

## Validation Attributes Example

```csharp
public class CreateOvertimeRequestDto
{
    [Required(ErrorMessage = "Date is required")]
    public DateTime Date { get; set; }

    [Required]
    [Range(0.5, 12, ErrorMessage = "Hours must be between 0.5 and 12")]
    public decimal Hours { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int ProjectId { get; set; }

    [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string Reason { get; set; }
}
```

## Common Mistakes to Avoid

### ❌ Wrong: Business Logic in Mapping

```csharp
// ❌ WRONG - Business logic in mapping
public static UserDto ToDto(this User entity)
{
    return new UserDto
    {
        Id = entity.Id,
        Name = entity.Name,
        // ❌ Business rule - should be in service
        CanRequestOvertime = entity.ContractType == "FullTime" &&
                           entity.YearsOfService > 1,
        // ❌ Validation - should be in service or validator
        IsValid = !string.IsNullOrEmpty(entity.Email)
    };
}

// ✅ CORRECT - Move logic to service
public async Task<UserDto> GetUserAsync(int id)
{
    var user = await _repository.GetByIdAsync(id);
    var dto = user.ToDto();

    // Business logic in service
    dto.CanRequestOvertime = await CanUserRequestOvertimeAsync(user);

    return dto;
}
```

### ✅ Correct: Simple Mapping Only

```csharp
public static UserDto ToDto(this User entity)
{
    return new UserDto
    {
        Id = entity.Id,
        Name = entity.Name ?? "Unknown", // Simple null handling
        Status = entity.Status.ToString(), // Enum to string
        CreatedAt = entity.CreatedAt,
        Age = DateTime.UtcNow.Year - entity.BirthDate.Year // Simple calculation
    };
}
```
