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

## Base Entity Class

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

## Entity Implementation Examples

### ID Strategy Rule

**Always use integer IDs for entities and foreign keys for optimal query performance. Use GUIDs/strings for external APIs and public-facing methods.**

**Rationale:**
- Integer IDs provide better database performance (smaller indexes, faster joins)
- GUIDs/strings provide security and prevent enumeration in public APIs
- Maintain integer PK internally, expose GUID externally via a separate column

**Implementation Pattern:**
```csharp
// Entity with int PK and Guid for external use
public class Order : Entity<int>
{
    public int CustomerId { get; set; } // FK uses int for performance
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }

    public virtual Customer Customer { get; set; } = null!;
    public virtual ICollection<OrderLine> OrderLines { get; set; } = new List<OrderLine>();
}

// Repository method uses Guid for external callers
public async Task<Order> GetByIdAsync(string externalId)
{
    return await _context.Orders
        .Include(o => o.Customer)
        .FirstOrDefaultAsync(o => o.externalId == externalId);
}
```

**Standard Examples:**
```csharp
// Entity with int ID
public class User : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<OvertimeRequest> OvertimeRequests { get; set; } = new List<OvertimeRequest>();
}
```

## Unit of Work Implementation

```csharp
public class UnitOfWork(AppDbContext context, IMediator mediator/*optional*/, IUserContext userContext) : IUnitOfWork
{
    public async Task InTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        _logger.LogdDebug("Starting async transaction.");
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await action(_dbContext);
            await SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _logger.LogdDebug("Async transaction committed.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Async transaction rolled back due to exception.");
            throw;
        }
    }

    public async Task<TReturn> InTransactionAsync<TReturn>(Func<Task<TReturn>> action, CancellationToken cancellationToken = default)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        _logger.LogdDebug("Starting async transaction (generic).");
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await action();
            await SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _logger.LogdDebug("Async transaction (generic) committed.");
            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Async transaction (generic) rolled back due to exception.");
            throw;
        }
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Auto-update audit fields on Entity base class
        var currentUser = userContext.GetCurrentUserId();
        var now = DateTime.UtcNow;
        
        foreach (var entry in context.ChangeTracker.Entries<Entity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = currentUser;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = currentUser;
                    break;
                    
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = currentUser;
                    break;
            }
        }
        
        var entitiesWithDomainEvents = context.ChangeTracker.Entries<AggregateRoot>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count != 0)
            .ToList();
        
        var domainEvents = entitiesWithDomainEvents
            .SelectMany(e => e.DomainEvents)
            .ToList();
        
        await context.SaveChangesAsync(cancellationToken);
        
        // Wrap each domain event with context and publish
        foreach (var domainEvent in domainEvents)
        {
            await mediator.Publish(domainEvent, cancellationToken); // optinally when MediatR library used
        }
        
        foreach (var entity in entitiesWithDomainEvents)
        {
            entity.ClearDomainEvents();
        }
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

    await _unitOfWork.InTransactionAsync(_ => 
    {
        var request = await _overtimeRepository.GetByIdAsync(requestId);
        request.Approve()

        var user = await _userRepository.GetByIdAsync(request.UserId);
        user.OvertimeBalance += request.Hours;
    });

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

        // Audit fields from Entity<TId>
        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.CreatedBy)
            .HasMaxLength(100);

        builder.Property(u => u.UpdatedBy)
            .HasMaxLength(100);

        // Entity-specific properties
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

## Base Entity Configuration (Optional)

```csharp
// Create a base configuration for common audit fields
public abstract class EntityConfiguration<TEntity, TId> : IEntityTypeConfiguration<TEntity>
    where TEntity : Entity<TId>
    where TId : notnull
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(100);

        builder.Property(e => e.UpdatedBy)
            .HasMaxLength(100);
    }
}

// Usage
public class UserConfiguration : EntityConfiguration<User, int>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder); // Apply base audit field configuration

        builder.ToTable("Users");

        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(u => u.Email)
            .IsUnique();
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
