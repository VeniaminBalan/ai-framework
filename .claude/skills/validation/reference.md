# Validation Reference

Detailed rules and conventions for FluentValidation.

## Common Validation Rules

### String
```csharp
.NotEmpty()                    // Not null/empty
.Length(5, 50)                 // Between 5-50 chars
.MaximumLength(100)            // Max 100 chars
.Matches("^[a-zA-Z]+$")       // Regex pattern
.EmailAddress()                // Valid email
.Must(BeValidName)             // Custom rule
.MustAsync(BeUniqueName)       // Async rule
```

### Numeric
```csharp
.GreaterThan(0)                // > 0
.GreaterThanOrEqualTo(18)      // >= 18
.InclusiveBetween(18, 65)     // 18-65 inclusive
.PrecisionScale(10, 2, false) // 10 digits, 2 decimals
```

### DateTime
```csharp
.LessThan(DateTime.Now)        // Past date
.GreaterThan(x => x.StartDate) // After another date
```

### Collections
```csharp
.NotEmpty()                    // Not empty collection
.Must(x => x.Count <= 10)     // Max 10 items
RuleForEach(x => x.Items)     // Validate each item
```

### Enums
```csharp
.IsInEnum()                    // Valid enum value
```

## Error Messages

```csharp
// Simple message
.WithMessage("Hours are required")

// Message with value
.WithMessage(x => $"Hours {x.Hours} invalid. Must be 0.5-12")

// Message with placeholders
.Length(10, 500).WithMessage("Between {MinLength}-{MaxLength} chars. You entered {TotalLength}")

// Error code
.WithErrorCode("PROJECT_REQUIRED")

// Localized messages
.WithMessage(localizer["NameRequired"])
```

## Registration and Integration

### Service Registration

```csharp
// Program.cs
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
```

### Controller Integration

```csharp
// Automatic validation
[HttpPost]
public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto dto)
{
    // Validation happens automatically
    // Returns 400 with errors if validation fails
    var user = await _userService.CreateUserAsync(dto);
    return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
}
```

## File Organization

```
Validators/
├── Users/
│   ├── CreateUserDtoValidator.cs
│   └── UpdateUserDtoValidator.cs
├── Projects/
│   ├── CreateProjectDtoValidator.cs
│   └── UpdateProjectDtoValidator.cs
├── OvertimeRequests/
│   └── CreateOvertimeRequestDtoValidator.cs
└── CustomValidators/
    └── DateRangeValidator.cs
```

## When to Use FluentValidation vs Data Annotations

### Use Data Annotations for:
- Simple required fields: `[Required]`
- Basic length constraints: `[MaxLength(100)]`
- Standard formats: `[EmailAddress]`

### Use FluentValidation for:
- Cross-property validation
- Conditional validation
- Async validation (database checks)
- Complex business rules
- Custom error messages
- Collection validation
