# Controller Reference

Detailed rules and conventions for ASP.NET Core API controllers.

## RESTful Conventions

### HTTP Methods
- `GET` - Retrieve resources (idempotent, safe)
- `POST` - Create new resources
- `PUT` - Update entire resource
- `PATCH` - Partial update
- `DELETE` - Remove resource

### Status Codes
- `200 OK` - Successful GET, PUT, PATCH with content
- `201 Created` - Successful POST
- `204 No Content` - Successful PUT, PATCH, DELETE without content
- `400 Bad Request` - Validation errors
- `401 Unauthorized` - Missing or invalid authentication
- `403 Forbidden` - Valid auth but insufficient permissions
- `404 Not Found` - Resource doesn't exist
- `409 Conflict` - Business rule violation
- `500 Internal Server Error` - Unexpected errors (handled by middleware)

### URL Structure
```
GET    /api/v1/users              - List all users (paginated)
GET    /api/v1/users/{id}         - Get single user
POST   /api/v1/users              - Create user
PUT    /api/v1/users/{id}         - Update user
DELETE /api/v1/users/{id}         - Delete user

GET    /api/v1/users/{id}/orders  - Get user's orders
```

## Input Validation

### Validate at Controller Level

Use `ModelState.IsValid` for basic validation or FluentValidation for complex scenarios.

### FluentValidation Setup
```csharp
public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
```

## Pagination

### Always paginate collections

- Use `[FromQuery] PaginationParameters` for pagination input
- Return `PagedResult<T>` containing items and metadata
- Add pagination metadata to response headers with `X-Pagination`

## Authorization

### Apply authorization attributes

- Use `[Authorize]` at controller level for authenticated routes
- Use `[Authorize(Roles = "Admin")]` for role-based access
- Use `[Authorize(Policy = "PolicyName")]` for policy-based access
- Use `[AllowAnonymous]` to override for specific endpoints

## Error Handling

### Don't handle errors in controllers
- Let middleware handle exceptions globally
- Only handle expected business scenarios (e.g., returning NotFound for missing resources)
- Never catch generic exceptions in controller actions
- Never return raw exception messages to clients

## Documentation

### XML Comments
- Every public action must have XML documentation
- Document parameters with `<param>` tags
- Document return types with `<returns>` tags
- Document status codes with `<response>` tags
- Use `[ProducesResponseType]` attributes for all possible responses
