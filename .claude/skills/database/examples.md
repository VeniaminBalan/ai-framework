# Database Examples

## Repository Implementation

```csharp
public class UserRepository : IUserRepository
{
    private readonly OvertimeDbContext _context;

    public UserRepository(OvertimeDbContext context)
    {
        _context = context;
    }

    // Single entity - may return tracked entity
    public async Task<User> GetByIdAsync(int id)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    // Collection - must use explicit projection to DTOs
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
                IsActive = u.IsActive,
                Roles = u.UserRoles.Select(ur => new RoleDto
                {
                    Id = ur.Role.Id,
                    Name = ur.Role.Name
                }).ToList()
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

    public async Task<List<UserDto>> GetActiveUsersAsync()
    {
        return await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                IsActive = u.IsActive
            })
            .ToListAsync();
    }

    // Existence checks
    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Users.AnyAsync(u => u.Id == id);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    // Commands
    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public void Update(User user)
    {
        _context.Users.Update(user);
    }

    public void Remove(User user)
    {
        _context.Users.Remove(user);
    }

    // Specialized queries
    public async Task<bool> HasActiveOrdersAsync(int userId)
    {
        return await _context.Orders
            .AsNoTracking()
            .AnyAsync(o => o.UserId == userId && o.Status == OrderStatus.Active);
    }
}
```

## Unit of Work Implementation

```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly OvertimeDbContext _context;
    private readonly IUserContext _userContext;

    public UnitOfWork(OvertimeDbContext context, IUserContext userContext)
    {
        _context = context;
        _userContext = userContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Set audit fields before saving
        var entries = _context.ChangeTracker.Entries<Entity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        var currentUserId = _userContext.GetCurrentUserId();
        var currentTime = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedBy = currentUserId;
                entry.Entity.CreatedOn = currentTime;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedBy = currentUserId;
                entry.Entity.ModifiedOn = currentTime;
            }
        }

        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await _context.Database.BeginTransactionAsync();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
```

## Pagination Classes

```csharp
public class PaginationParameters
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;

    public int PageNumber { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }

    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;

    public PaginationMetadata Metadata => new PaginationMetadata
    {
        TotalCount = TotalCount,
        PageSize = PageSize,
        CurrentPage = PageNumber,
        TotalPages = TotalPages,
        HasPrevious = HasPrevious,
        HasNext = HasNext
    };
}

public class PaginationMetadata
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
}
```

## Transaction Example

```csharp
// In Service
public async Task ApproveOvertimeAsync(int requestId)
{
    using var transaction = await _unitOfWork.BeginTransactionAsync();

    try
    {
        var request = await _overtimeRepository.GetByIdAsync(requestId);
        request.Status = OvertimeStatus.Approved;

        var user = await _userRepository.GetByIdAsync(request.UserId);
        user.OvertimeBalance += request.Hours;

        await _unitOfWork.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

## Entity Configuration

```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.HasMany(u => u.OvertimeRequests)
            .WithOne(o => o.User)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

## DbContext

```csharp
public class OvertimeDbContext : DbContext
{
    public OvertimeDbContext(DbContextOptions<OvertimeDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<OvertimeRequest> OvertimeRequests { get; set; }
    public DbSet<Project> Projects { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OvertimeDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
```

## Common Mistakes to Avoid

### Returning Entities Without Projection

```csharp
// ❌ Wrong - Returns full entities instead of projecting to DTOs
public async Task<List<User>> GetAllUsersAsync()
{
    return await _context.Users
        .OrderBy(u => u.Name)
        .ToListAsync();
    // Missing: Explicit projection with .Select()
}

// ❌ Wrong - Loads entities then maps (inefficient)
public async Task<List<UserDto>> GetAllUsersAsync()
{
    var users = await _context.Users
        .AsNoTracking()
        .OrderBy(u => u.Name)
        .ToListAsync();

    return users.ToDtoList();
    // Wrong: Should project with .Select() instead
}

// ✅ Correct - Uses explicit projection to DTOs
public async Task<List<UserDto>> GetAllUsersAsync()
{
    return await _context.Users
        .OrderBy(u => u.Name)
        .Select(u => new UserDto
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            IsActive = u.IsActive
        })
        .ToListAsync();
}
```
