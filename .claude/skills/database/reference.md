# Database Reference

Detailed rules and conventions for Entity Framework Core data access.

## Repository Pattern

### Repository Interface

```csharp
public interface IUserRepository
{
    // Single entity queries (may return tracked entity for updates)
    Task<User> GetByIdAsync(int id);
    Task<User> GetByEmailAsync(string email);

    // Collection queries (must use explicit projection to DTOs)
    Task<PagedResult<UserDto>> GetPagedAsync(PaginationParameters parameters);
    Task<List<UserDto>> GetActiveUsersAsync();

    // Existence checks
    Task<bool> ExistsAsync(int id);
    Task<bool> ExistsByEmailAsync(string email);

    // Commands
    Task AddAsync(User user);
    void Update(User user);
    void Remove(User user);

    // Specialized queries
    Task<bool> HasActiveOrdersAsync(int userId);
}
```

## Unit of Work Pattern

### Base Entity Class

```csharp
public abstract class Entity
{
    public int CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedOn { get; set; }
}
```

### IUnitOfWork Interface

```csharp
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync();
}
```

## Query Rules (Critical)

### Collection Queries
- Must use explicit projection with `.Select()`
- Must return DTOs, not entities
- Must paginate large collections
- Never return tracked entities

### Single Entity Queries
- May return tracked entities for updates
- Use `.Include()` for related data needed for updates

## Transaction Management

### When to Use Transactions
- Multiple related operations that must succeed or fail together
- Cross-aggregate updates
- Operations requiring consistency guarantees

## Entity Configuration

### Use Fluent API
- Configure in `IEntityTypeConfiguration<T>` classes
- Define table names, keys, indexes
- Configure relationships and delete behavior
- Set property constraints (required, max length)

## Dependency Registration

```csharp
// Program.cs
builder.Services.AddDbContext<OvertimeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
```
