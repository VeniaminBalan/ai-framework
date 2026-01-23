# Testing Backend Reference

Detailed rules and conventions for backend testing.

## Testing Requirements

### Core Rules

- Minimum 90% test coverage
- Tests must be deterministic
- Tests must be fast
- Tests must be isolated
- **Always run all tests before accepting new changes**

## Test Structure (Arrange-Act-Assert)

```csharp
[Fact]
public async Task GetById_WhenUserExists_ReturnsUser()
{
    // Arrange
    var expectedUser = new User { Id = 1, Name = "Test" };
    _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(expectedUser);

    // Act
    var result = await _sut.GetByIdAsync(1);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(expectedUser.Name, result.Name);
}
```

## Naming Convention

`{MethodName}_{Scenario}_{ExpectedResult}`

Examples:
- `GetById_WhenUserExists_ReturnsUser`
- `Create_WithInvalidInput_ThrowsValidationException`
- `Delete_WhenUserHasActiveOrders_ThrowsBusinessException`

## Test Pyramid

| Type | Percentage | Purpose |
|------|------------|---------|
| Unit tests | 70% | Fast, isolated component testing |
| Integration tests | 20% | API, services, database |
| E2E tests | 10% | Critical user journeys |

## Coverage Targets

- Overall: 80%
- Critical paths: 95%
- New code: 85%

## Anti-Patterns to Avoid

- Testing implementation details
- Flaky tests (non-deterministic)
- Test interdependence
- Over-mocking
- No assertions
- Shared state between tests

## Test Organization

### File Structure

```
Tests/
├── OvertimeRequest.Api.Tests/
│   ├── Controllers/
│   │   ├── UsersControllerTests.cs
│   │   └── OvertimeRequestsControllerTests.cs
│   ├── Services/
│   │   ├── UserServiceTests.cs
│   │   └── OvertimeServiceTests.cs
│   ├── Integration/
│   │   ├── CustomWebApplicationFactory.cs
│   │   ├── UsersIntegrationTests.cs
│   │   └── OvertimeIntegrationTests.cs
│   └── Repositories/
│       ├── UserRepositoryTests.cs
│       └── OvertimeRepositoryTests.cs
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run specific test
dotnet test --filter "FullyQualifiedName~UserServiceTests.GetUserByIdAsync_WhenUserExists_ReturnsUserDto"
```

## Before Writing Tests

1. Identify test type needed
2. Plan test cases (happy path, edge cases, errors)
3. Prepare test data using builders/factories
