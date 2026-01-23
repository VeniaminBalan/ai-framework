# Database Reference

Detailed rules and conventions for Entity Framework Core data access.

## Repository Pattern

### Repository Interface

```csharp
public interface IUserRepository
{
    // Single entity queries (may return tracked entity for updates)
    // Use ExternalId (string) for public-facing methods
    Task<User?> GetByIdAsync(string externalId);
    Task<User?> GetByEmailAsync(string email);

    // Collection queries (must use explicit projection to DTOs)
    Task<PagedResult<UserDto>> GetPagedAsync(PaginationParameters parameters);
    Task<List<UserDto>> GetActiveUsersAsync();

    // Existence checks
    Task<bool> ExistsAsync(string externalId);
    Task<bool> ExistsByEmailAsync(string email);

    // Commands
    Task AddAsync(User user);
    void Update(User user);
    void Remove(User user);

    // Specialized queries (internal methods may use int id for performance)
    Task<bool> HasActiveOrdersAsync(int userId);
}
```

## Entity Base Classes

### Base Entity Class (Required)

All entities **must** inherit from `Entity`:

```csharp
public class Entity
{
    public string ExternalId { get; set; } = Guid.CreateVersion7().ToString();
    public int Id { get; set; } = default!; // created by the database, used for foreign keys
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
}
```

### AggregateRoot Class (For Domain Events)

For domain models that publish domain events, inherit from `AggregateRoot`:

```csharp
public class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    [NotMapped]
    public ICollection<IDomainEvent> DomainEvents => _domainEvents;

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

### ID Strategy Rule (Critical)

**Always use integer IDs for entities and foreign keys for optimal query performance. Use GUIDs/strings (ExternalId) for external APIs and public-facing methods.**

**Rationale:**
- Integer IDs provide better database performance (smaller indexes, faster joins)
- GUIDs/strings provide security and prevent enumeration in public APIs
- The `Entity` base class provides both: `Id` (int) for internal use and `ExternalId` (string/Guid) for external APIs

**Implementation:**
- All entities use `int Id` as primary key (inherited from `Entity`)
- All foreign keys use `int` for performance
- Repository methods exposed to external layers use `ExternalId` parameter: `GetByIdAsync(string externalId)`
- Internal repository methods may use `int Id` when needed for performance

### Entity Example

```csharp
public class User : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public class Order : Entity
{
    public int CustomerId { get; set; } // FK uses int for performance
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }

    public virtual Customer Customer { get; set; } = null!;
}
```

## Unit of Work Pattern

### IUnitOfWork Interface

```csharp
public interface IUnitOfWork
{
    Task InTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);
    Task<TReturn> InTransactionAsync<TReturn>(Func<Task<TReturn>> action, CancellationToken cancellationToken = default);
}
```

### Key Responsibilities

The Unit of Work implementation handles:
- Transaction management (begin, commit, rollback)
- Auto-updating audit fields (CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
- Publishing domain events from AggregateRoot entities after SaveChanges
- Clearing domain events after publishing

## Query Rules (Critical)

### Collection Queries
- **Must** use explicit projection with `.Select()`
- **Must** return DTOs, not entities
- **Must** paginate large collections
- **Never** return tracked entities
- **Never** load entities then map to DTOs (inefficient)

```csharp
// ✅ Correct - Project directly in the query
return await _context.Users
    .OrderBy(u => u.Name)
    .Select(u => new UserDto
    {
        Id = u.ExternalId,  // Expose ExternalId to external consumers
        Name = u.Name,
        Email = u.Email
    })
    .ToListAsync();

// ❌ Wrong - Loads entities then maps
var users = await _context.Users.ToListAsync();
return users.Select(u => new UserDto { ... }).ToList();
```

### Single Entity Queries
- May return tracked entities for updates
- Use `.Include()` for related data needed for updates
- Use `ExternalId` parameter for public methods

```csharp
public async Task<User?> GetByIdAsync(string externalId)
{
    return await _context.Users
        .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
        .FirstOrDefaultAsync(u => u.ExternalId == externalId);
}
```

## Transaction Management

### When to Use Transactions
- Multiple related operations that must succeed or fail together
- Cross-aggregate updates
- Operations requiring consistency guarantees

### Transaction Pattern

```csharp
// In Service - use Unit of Work's InTransactionAsync
public async Task ApproveOvertimeAsync(string requestExternalId)
{
    await _unitOfWork.InTransactionAsync(async () =>
    {
        var request = await _overtimeRepository.GetByIdAsync(requestExternalId);
        request.Approve();

        var user = await _userRepository.GetByIdAsync(request.UserId);
        user.OvertimeBalance += request.Hours;
    });
}
```

## Entity Configuration

### Use Fluent API
- Configure in `IEntityTypeConfiguration<T>` classes
- Define table names, keys, indexes
- Configure relationships and delete behavior
- Set property constraints (required, max length)
- Configure audit fields from Entity base class

```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        // ExternalId - unique index for lookups
        builder.HasIndex(u => u.ExternalId).IsUnique();

        // Audit fields from Entity
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.CreatedBy).HasMaxLength(100);
        builder.Property(u => u.UpdatedBy).HasMaxLength(100);

        // Entity-specific properties
        builder.Property(u => u.Name).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(255);
        builder.HasIndex(u => u.Email).IsUnique();

        // Relationships
        builder.HasMany(u => u.OvertimeRequests)
            .WithOne(o => o.User)
            .HasForeignKey(o => o.UserId)  // FK uses int Id
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

## DbContext

```csharp
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<OvertimeRequest> OvertimeRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

## Dependency Registration

```csharp
// Program.cs
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
```
