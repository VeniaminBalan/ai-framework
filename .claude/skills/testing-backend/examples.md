# Testing Backend Examples

## Service Tests with Mocks

```csharp
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly Mock<IUserContext> _userContextMock;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _repositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<UserService>>();
        _userContextMock = new Mock<IUserContext>();

        _service = new UserService(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            _userContextMock.Object);
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenUserExists_ReturnsUserDto()
    {
        // Arrange
        var userId = 1;
        var user = new User
        {
            Id = userId,
            Name = "John Doe",
            Email = "john@example.com"
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _service.GetUserByIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal("john@example.com", result.Email);
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenUserNotFound_ReturnsNull()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((User)null);

        // Act
        var result = await _service.GetUserByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateUserAsync_WhenEmailExists_ThrowsBusinessException()
    {
        // Arrange
        var dto = new CreateUserDto { Email = "existing@example.com", Name = "Test" };

        _repositoryMock
            .Setup(r => r.ExistsByEmailAsync(dto.Email))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<BusinessException>(
            () => _service.CreateUserAsync(dto));
    }

    [Fact]
    public async Task CreateUserAsync_WhenValid_CreatesUserAndReturnsDto()
    {
        // Arrange
        var dto = new CreateUserDto { Email = "new@example.com", Name = "New User" };
        var currentUserId = 123;

        _userContextMock.Setup(c => c.UserId).Returns(currentUserId);
        _repositoryMock.Setup(r => r.ExistsByEmailAsync(dto.Email)).ReturnsAsync(false);

        User capturedUser = null;
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u);

        // Act
        var result = await _service.CreateUserAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Name, result.Name);
        Assert.Equal(dto.Email, result.Email);
        Assert.Equal(currentUserId, capturedUser.CreatedBy);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }
}
```

## Repository Tests (Integration with InMemory DB)

```csharp
public class UserRepositoryTests : IDisposable
{
    private readonly OvertimeDbContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<OvertimeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new OvertimeDbContext(options);
        _repository = new UserRepository(_context);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserExists_ReturnsUser()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test User", result.Name);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsCorrectPage()
    {
        // Arrange
        for (int i = 1; i <= 25; i++)
        {
            _context.Users.Add(new User
            {
                Id = i,
                Name = $"User {i}",
                Email = $"user{i}@example.com"
            });
        }
        await _context.SaveChangesAsync();

        var parameters = new PaginationParameters
        {
            PageNumber = 2,
            PageSize = 10
        };

        // Act
        var result = await _repository.GetPagedAsync(parameters);

        // Assert
        Assert.Equal(10, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.True(result.HasPrevious);
        Assert.True(result.HasNext);
    }

    [Fact]
    public async Task ExistsByEmailAsync_WhenExists_ReturnsTrue()
    {
        // Arrange
        _context.Users.Add(new User
        {
            Id = 1,
            Name = "Test",
            Email = "test@example.com"
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsByEmailAsync("test@example.com");

        // Assert
        Assert.True(result);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
```

## Controller Tests

```csharp
public class UsersControllerTests
{
    private readonly Mock<IUserService> _serviceMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _serviceMock = new Mock<IUserService>();
        _controller = new UsersController(_serviceMock.Object);
    }

    [Fact]
    public async Task GetById_WhenUserExists_ReturnsOkResult()
    {
        // Arrange
        var userDto = new UserDto { Id = 1, Name = "Test", Email = "test@example.com" };
        _serviceMock.Setup(s => s.GetUserByIdAsync(1)).ReturnsAsync(userDto);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedUser = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal(1, returnedUser.Id);
    }

    [Fact]
    public async Task GetById_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetUserByIdAsync(999)).ReturnsAsync((UserDto)null);

        // Act
        var result = await _controller.GetById(999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_WhenValid_ReturnsCreatedResult()
    {
        // Arrange
        var createDto = new CreateUserDto { Name = "New User", Email = "new@example.com" };
        var userDto = new UserDto { Id = 1, Name = "New User", Email = "new@example.com" };

        _serviceMock.Setup(s => s.CreateUserAsync(createDto)).ReturnsAsync(userDto);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedUser = Assert.IsType<UserDto>(createdResult.Value);
        Assert.Equal(1, returnedUser.Id);
    }
}
```

## Integration Tests

### WebApplicationFactory Setup

```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove real database
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<OvertimeDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add in-memory database
            services.AddDbContext<OvertimeDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
            });

            // Build service provider and seed data
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OvertimeDbContext>();
            db.Database.EnsureCreated();
            SeedTestData(db);
        });
    }

    private void SeedTestData(OvertimeDbContext context)
    {
        context.Users.Add(new User { Id = 1, Name = "Test User", Email = "test@example.com" });
        context.SaveChanges();
    }
}
```

### Integration Test Example

```csharp
public class UsersIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UsersIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_ReturnsSuccessAndCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/users?pageNumber=1&pageSize=10");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json; charset=utf-8",
            response.Content.Headers.ContentType.ToString());
    }

    [Fact]
    public async Task CreateUser_WithValidData_ReturnsCreated()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            Name = "New User",
            Email = "newuser@example.com"
        };
        var content = new StringContent(
            JsonSerializer.Serialize(createDto),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/users", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var location = response.Headers.Location;
        Assert.NotNull(location);
    }
}
```
