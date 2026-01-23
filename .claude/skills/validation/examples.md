# Validation Examples

## Basic Validator

```csharp
public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters")
            .Matches("^[a-zA-Z\\s]+$").WithMessage("Name can only contain letters and spaces");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[A-Z]").WithMessage("Password must contain uppercase letter")
            .Matches("[0-9]").WithMessage("Password must contain digit");
    }
}
```

## Validator with Dependencies (Async Validation)

```csharp
public class CreateOvertimeRequestDtoValidator : AbstractValidator<CreateOvertimeRequestDto>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUserContext _userContext;

    public CreateOvertimeRequestDtoValidator(
        IProjectRepository projectRepository,
        IUserContext userContext)
    {
        _projectRepository = projectRepository;
        _userContext = userContext;

        RuleFor(x => x.Date)
            .NotEmpty()
            .Must(BeValidWorkDate).WithMessage("Date must be within last 3 months")
            .Must(BeAWorkday).WithMessage("Overtime only for workdays");

        RuleFor(x => x.Hours)
            .InclusiveBetween(0.5m, 12m)
            .Must(h => h % 0.5m == 0).WithMessage("Hours must be in 0.5 increments");

        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .MustAsync(ProjectExists).WithMessage("Project does not exist")
            .MustAsync(UserHasAccessToProject).WithMessage("No access to project");
    }

    private bool BeValidWorkDate(DateTime date) =>
        date <= DateTime.UtcNow.Date && date >= DateTime.UtcNow.AddMonths(-3);

    private bool BeAWorkday(DateTime date) =>
        date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday;

    private async Task<bool> ProjectExists(int id, CancellationToken ct) =>
        await _projectRepository.ExistsAsync(id);

    private async Task<bool> UserHasAccessToProject(int projectId, CancellationToken ct) =>
        await _projectRepository.UserHasAccessAsync(_userContext.UserId, projectId);
}
```

## Cross-Property Validation

```csharp
public class UpdateProjectDtoValidator : AbstractValidator<UpdateProjectDto>
{
    public UpdateProjectDtoValidator()
    {
        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date");

        RuleFor(x => x)
            .Must(HaveValidBudgetAllocation)
            .WithMessage("Budget allocation cannot exceed project budget")
            .When(x => x.Budget.HasValue && x.Allocations != null);
    }

    private bool HaveValidBudgetAllocation(UpdateProjectDto dto) =>
        dto.Allocations.Sum(a => a.Amount) <= dto.Budget.Value;
}
```

## Conditional Validation

```csharp
public class CreateEmployeeDtoValidator : AbstractValidator<CreateEmployeeDto>
{
    public CreateEmployeeDtoValidator()
    {
        RuleFor(x => x.WeeklyHours)
            .Equal(40).WithMessage("Full-time must work 40 hours")
            .When(x => x.ContractType == ContractType.FullTime);

        RuleFor(x => x.WeeklyHours)
            .InclusiveBetween(10, 35)
            .When(x => x.ContractType == ContractType.PartTime);

        RuleFor(x => x.HourlyRate)
            .GreaterThan(0).WithMessage("Hourly rate required for contractors")
            .When(x => x.ContractType == ContractType.Contractor);
    }
}
```

## Collection Validation

```csharp
public class CreateBulkRequestDtoValidator : AbstractValidator<CreateBulkRequestDto>
{
    public CreateBulkRequestDtoValidator()
    {
        RuleFor(x => x.Requests)
            .NotEmpty()
            .Must(x => x.Count <= 50).WithMessage("Max 50 requests");

        RuleForEach(x => x.Requests)
            .SetValidator(new CreateOvertimeRequestDtoValidator());

        RuleFor(x => x.Requests)
            .Must(NotHaveDuplicateDates)
            .WithMessage("Cannot have duplicate dates");
    }

    private bool NotHaveDuplicateDates(List<CreateOvertimeRequestDto> requests) =>
        requests.Select(r => r.Date).Distinct().Count() == requests.Count;
}
```

## Custom Reusable Validators

```csharp
public static class CustomValidators
{
    public static IRuleBuilderOptions<T, DateTime> MustBeWithinMonths<T>(
        this IRuleBuilder<T, DateTime> ruleBuilder, int months)
    {
        return ruleBuilder
            .Must(date => date >= DateTime.UtcNow.AddMonths(-months) && date <= DateTime.UtcNow)
            .WithMessage($"Date must be within last {months} months");
    }

    public static IRuleBuilderOptions<T, string> MustBeValidProjectCode<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Matches("^[A-Z]{3}-[0-9]{4}$")
            .WithMessage("Project code format: XXX-0000");
    }

    public static IRuleBuilderOptions<T, decimal> MustBeInIncrements<T>(
        this IRuleBuilder<T, decimal> ruleBuilder, decimal increment)
    {
        return ruleBuilder
            .Must(value => value % increment == 0)
            .WithMessage($"Must be in increments of {increment}");
    }
}

// Usage
RuleFor(x => x.Date).MustBeWithinMonths(3);
RuleFor(x => x.Hours).MustBeInIncrements(0.25m);
RuleFor(x => x.ProjectCode).MustBeValidProjectCode();
```

## Manual Validation (for batch operations)

```csharp
[HttpPost("batch")]
public async Task<ActionResult> CreateBatch([FromBody] List<CreateUserDto> dtos)
{
    var validator = new CreateUserDtoValidator();
    var errors = new List<ValidationFailure>();

    foreach (var dto in dtos)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid) errors.AddRange(result.Errors);
    }

    if (errors.Any())
        return BadRequest(new { Errors = errors });

    await _userService.CreateBatchAsync(dtos);
    return Ok();
}
```

## Testing Validators

```csharp
public class CreateUserDtoValidatorTests
{
    private readonly CreateUserDtoValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Name_Empty()
    {
        var dto = new CreateUserDto { Name = "" };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Valid()
    {
        var dto = new CreateUserDto { Name = "John Doe" };
        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }
}

// Testing async validators
public class ValidatorTests
{
    private readonly Mock<IProjectRepository> _mockRepo = new();
    private readonly CreateOvertimeRequestDtoValidator _validator;

    public ValidatorTests()
    {
        _validator = new(_mockRepo.Object, Mock.Of<IUserContext>());
    }

    [Fact]
    public async Task Should_Error_When_Project_Not_Exists()
    {
        _mockRepo.Setup(x => x.ExistsAsync(999)).ReturnsAsync(false);
        var dto = new CreateOvertimeRequestDto { ProjectId = 999 };
        var result = await _validator.TestValidateAsync(dto);
        result.ShouldHaveValidationErrorFor(x => x.ProjectId);
    }
}
```
