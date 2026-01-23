# DTO Mapping Reference

Detailed rules and conventions for DTOs and manual mapping.

## DTO Rules

- **Never expose domain entities through APIs**
- DTOs define the API contract
- DTOs should be immutable where possible
- Use separate DTOs for different operations (Create, Update, Response)
- Keep DTOs flat and simple

## Manual Mapping Only

### ❌ FORBIDDEN: Automatic Mapping Libraries
- AutoMapper
- Mapster
- Any reflection-based mapper

### ✅ REQUIRED: Manual Mapping Patterns
- Explicit EF Core projection with `.Select()` for collection queries
- Extension methods for single entity mapping
- Clear, debuggable, performant

## Extension Methods vs EF Core Projection

### Extension Methods (For Single Entities)

**Use extension methods for:**
- Single entity operations (create, update, get by ID)
- Mapping request DTOs to entities
- Updating existing entities from DTOs

**Do NOT use extension methods for:**
- Collection queries from database (use EF Core projection instead)

### EF Core Projection (For Collections)

**Use `.Select()` projection for:**
- All collection queries from database
- Paginated results
- List operations

## Mapping Rules

### ✅ Allowed in Mappings

- Direct property copying
- Null checks
- Simple type conversions (int to string, enum to string)
- Formatting (dates, numbers)
- Null coalescing (`??`)
- Collection projections (`.Select()`, `.ToList()`)

### ❌ Forbidden in Mappings

- Business logic
- Validation rules
- Conditional business rules
- Database queries
- Service calls
- Complex calculations related to domain

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

## Validation Attributes

### Common Attributes
- `[Required]` - Field is required
- `[MaxLength(n)]` - Maximum string length
- `[MinLength(n)]` - Minimum string length
- `[Range(min, max)]` - Numeric range
- `[EmailAddress]` - Valid email format
- `[RegularExpression]` - Pattern matching
