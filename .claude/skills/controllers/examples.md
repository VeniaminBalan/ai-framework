# Controller Examples

## Standard Controller Pattern

```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Gets a user by ID
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <returns>User details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound();

        return Ok(user);
    }

    /// <summary>
    /// Gets all users with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserDto>>> GetAll(
        [FromQuery] PaginationParameters parameters)
    {
        var result = await _userService.GetUsersAsync(parameters);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new user
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto dto)
    {
        var user = await _userService.CreateUserAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    /// <summary>
    /// Updates an existing user
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
    {
        await _userService.UpdateUserAsync(id, dto);
        return NoContent();
    }

    /// <summary>
    /// Deletes a user
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await _userService.DeleteUserAsync(id);
        return NoContent();
    }
}
```

## Input Validation Example

```csharp
[HttpPost]
public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserDto dto)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    // Validation passed, delegate to service
    var user = await _userService.CreateUserAsync(dto);
    return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
}
```

## Pagination Example

```csharp
[HttpGet]
public async Task<ActionResult<PagedResult<UserDto>>> GetAll(
    [FromQuery] PaginationParameters parameters)
{
    var result = await _userService.GetUsersAsync(parameters);

    // Add pagination metadata to response headers
    Response.Headers.Add("X-Pagination",
        JsonSerializer.Serialize(result.Metadata));

    return Ok(result);
}
```

## Authorization Example

```csharp
[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResult<UserDto>>> GetAll()

    [HttpGet("{id}")]
    [Authorize(Policy = "CanViewUser")]
    public async Task<ActionResult<UserDto>> GetById(int id)
}
```

## XML Documentation Example

```csharp
/// <summary>
/// Retrieves a user by their unique identifier
/// </summary>
/// <param name="id">The unique user identifier</param>
/// <returns>The user details if found</returns>
/// <response code="200">Returns the user details</response>
/// <response code="404">If the user is not found</response>
[HttpGet("{id}")]
[ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<UserDto>> GetById(int id)
```

## Common Mistakes to Avoid

### Business logic in controllers

```csharp
// Wrong
[HttpPost]
public async Task<ActionResult> Create(CreateUserDto dto)
{
    if (await _repository.ExistsAsync(dto.Email))
        return Conflict();
    var user = new User { Email = dto.Email };
    await _repository.AddAsync(user);
    return Ok();
}

// Correct - delegate to service
[HttpPost]
public async Task<ActionResult> Create(CreateUserDto dto)
{
    var user = await _userService.CreateUserAsync(dto);
    return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
}
```

### Returning entities instead of DTOs

```csharp
// Wrong
[HttpGet]
public async Task<ActionResult<List<User>>> GetAll()

// Correct
[HttpGet]
public async Task<ActionResult<PagedResult<UserDto>>> GetAll([FromQuery] PaginationParameters parameters)
```

### Catching exceptions in controllers

```csharp
// Wrong
[HttpGet("{id}")]
public async Task<ActionResult<UserDto>> GetById(int id)
{
    try
    {
        var user = await _userService.GetUserByIdAsync(id);
        return Ok(user);
    }
    catch (Exception ex)
    {
        return StatusCode(500, ex.Message);
    }
}

// Correct - let middleware handle exceptions
[HttpGet("{id}")]
public async Task<ActionResult<UserDto>> GetById(int id)
{
    var user = await _userService.GetUserByIdAsync(id);
    if (user == null)
        return NotFound();

    return Ok(user);
}
```
