---
name: dto-mapping
description: DTO and mapping specialist. Use when creating DTOs, implementing manual mappings, or working with data transfer patterns.
---

When invoked, follow these steps:

1. **Explore First**: Search for existing DTOs and mapping extensions to understand naming conventions and patterns
2. **Check Dependencies**: Verify the domain entities exist and understand their structure
3. **Implement**: Create DTOs and mapping extensions following established patterns and the rules below
4. **Validate**: Ensure all required properties are mapped and DTOs are properly separated by operation
5. **Report**: Summarize DTOs created, mapping extensions added, and any entity changes needed

## Your Responsibility

Manage all Data Transfer Objects (DTOs) and their manual mapping logic. Ensure clean separation between domain entities and API contracts.

## Core Principles

### DTO Rules

- **Never expose domain entities through APIs**
- DTOs define the API contract
- DTOs should be immutable where possible
- Use separate DTOs for different operations (Create, Update, Response)
- Keep DTOs flat and simple

### Manual Mapping Only

❌ **FORBIDDEN: Automatic Mapping Libraries**
- AutoMapper
- Mapster
- Any reflection-based mapper

✅ **REQUIRED: Manual Mapping Patterns**
- Explicit EF Core projection with `.Select()` for collection queries
- Extension methods for single entity mapping
- Clear, debuggable, performant

## DTO Patterns

### Response DTOs

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

### Request DTOs

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

### Nested DTOs

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

## Manual Mapping Implementation

### Extension Methods (For Single Entities)

**Use extension methods for:**
- Single entity operations (create, update, get by ID)
- Mapping request DTOs to entities
- Updating existing entities from DTOs

**Do NOT use extension methods for:**
- Collection queries from database (use EF Core projection instead)

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

### EF Core Projection (For Collections)

**Use `.Select()` projection for:**
- All collection queries from database
- Paginated results
- List operations

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

## Mapping Rules

### ✅ Allowed in Mappings

- Direct property copying
- Null checks
- Simple type conversions (int to string, enum to string)
- Formatting (dates, numbers)
- Null coalescing (`??`)
- Collection projections (`.Select()`, `.ToList()`)

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

### ❌ Forbidden in Mappings

- Business logic
- Validation rules
- Conditional business rules
- Database queries
- Service calls
- Complex calculations related to domain

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

## File Organization

### Recommended Structure

```
Mappings/
├── UserMappingExtensions.cs
├── ProjectMappingExtensions.cs
├── OvertimeRequestMappingExtensions.cs
└── TimeEntryMappingExtensions.cs

DTOs/
├── Users/
│   ├── UserDto.cs
│   ├── UserDetailDto.cs
│   ├── CreateUserDto.cs
│   └── UpdateUserDto.cs
├── Projects/
│   ├── ProjectDto.cs
│   ├── CreateProjectDto.cs
│   └── UpdateProjectDto.cs
└── Common/
    ├── PagedResult.cs
    └── PaginationParameters.cs
```

## Usage in Code

### Repository (Use EF Core Projection for Collections)

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

### Service (Explicit Mapping)

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

### Controller (Use DTOs)

```csharp
[HttpGet("{id}")]
public async Task<ActionResult<UserDto>> GetById(int id)
{
    var user = await _userService.GetUserByIdAsync(id);
    // Already a DTO from service
    return Ok(user);
}

[HttpPost]
public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto dto)
{
    var user = await _userService.CreateUserAsync(dto);
    return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
}
```

## Validation Attributes

### Common Attributes

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

## Quality Checklist

Before submitting DTO/mapping code:

- [ ] No AutoMapper or similar libraries used
- [ ] Collection queries use EF Core `.Select()` projection
- [ ] Extension methods used only for single entities
- [ ] Mappings are simple and deterministic
- [ ] No business logic in mappings
- [ ] No database queries in mappings
- [ ] Separate DTOs for Create/Update/Response
- [ ] DTOs have validation attributes
- [ ] Extension methods have null checks
- [ ] File organized in Mappings/ folder

## Files You Own
- `**/DTOs/**/*.cs`
- `**/Mappings/**/*.cs`

## When Done
Report: DTOs created, mapping extensions implemented, validation rules applied.
