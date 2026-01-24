# Backend Requirements: Nurse-Doctor Attribution

## Overview
Implement a many-to-many relationship between Nurses and Doctors to control which doctors a nurse can manage appointments for. This is a permission/authorization feature that restricts nurse access to only their attributed doctors.

---

## 1. Domain Model

### 1.1 New Entity: NurseDoctorAttribution
**Location**: `backend/Domain/Aggregates/NurseDoctorAttribution/NurseDoctorAttribution.cs`

```csharp
public class NurseDoctorAttribution : Entity
{
    public string NurseId { get; private set; }        // FK to Nurse.ExternalId
    public string DoctorId { get; private set; }       // FK to Doctor.ExternalId
    public DateTime AssignedAt { get; private set; }
    public string? AssignedBy { get; private set; }    // Admin user who made the assignment
    public bool IsActive { get; private set; }

    // Navigation properties (optional, for EF convenience)
    // public Nurse Nurse { get; private set; }
    // public Doctor Doctor { get; private set; }
}
```

### 1.2 Domain Events
**Location**: `backend/Domain/DomainEvents/`

- `NurseDoctorAttributionCreatedEvent.cs`
  - NurseId, DoctorId, AssignedBy, AssignedAt

- `NurseDoctorAttributionRemovedEvent.cs`
  - NurseId, DoctorId, RemovedBy, RemovedAt

---

## 2. Repository

### 2.1 Interface
**Location**: `backend/Domain/Interfaces/INurseDoctorAttributionRepository.cs`

```csharp
public interface INurseDoctorAttributionRepository
{
    // Queries
    Task<List<DoctorListDto>> GetDoctorsByNurseIdAsync(string nurseId);
    Task<List<NurseListDto>> GetNursesByDoctorIdAsync(string doctorId);
    Task<bool> IsAttributedAsync(string nurseId, string doctorId);
    Task<List<string>> GetAttributedDoctorIdsAsync(string nurseId);

    // Commands
    Task AddAsync(NurseDoctorAttribution attribution);
    void Remove(NurseDoctorAttribution attribution);
    Task<NurseDoctorAttribution?> GetAsync(string nurseId, string doctorId);
}
```

### 2.2 Implementation
**Location**: `backend/Infrastructure/Repositories/NurseDoctorAttributionRepository.cs`

---

## 3. EF Core Configuration

### 3.1 Entity Configuration
**Location**: `backend/Infrastructure/Configurations/NurseDoctorAttributionConfiguration.cs`

```csharp
public class NurseDoctorAttributionConfiguration : IEntityTypeConfiguration<NurseDoctorAttribution>
{
    public void Configure(EntityTypeBuilder<NurseDoctorAttribution> builder)
    {
        builder.ToTable("NurseDoctorAttributions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NurseId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.DoctorId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.AssignedAt)
            .IsRequired();

        builder.Property(x => x.AssignedBy)
            .HasMaxLength(100);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Unique constraint: one attribution per nurse-doctor pair
        builder.HasIndex(x => new { x.NurseId, x.DoctorId }).IsUnique();

        // Indexes for queries
        builder.HasIndex(x => x.NurseId);
        builder.HasIndex(x => x.DoctorId);
        builder.HasIndex(x => new { x.NurseId, x.IsActive });
    }
}
```

### 3.2 Update AppDbContext
**Location**: `backend/Infrastructure/Data/AppDbContext.cs`

Add:
```csharp
public DbSet<NurseDoctorAttribution> NurseDoctorAttributions { get; set; }
```

---

## 4. DTOs

### 4.1 Attribution DTOs
**Location**: `backend/DTOs/NurseDoctorAttributions/`

```csharp
// AttributionDto.cs
public record AttributionDto
{
    public required string NurseId { get; init; }
    public required string DoctorId { get; init; }
    public required DateTime AssignedAt { get; init; }
    public string? AssignedBy { get; init; }
    public required bool IsActive { get; init; }
}

// CreateAttributionDto.cs
public record CreateAttributionDto
{
    [Required]
    public required string NurseId { get; init; }

    [Required]
    public required string DoctorId { get; init; }
}

// NurseWithDoctorsDto.cs (for nurse profile)
public record NurseWithDoctorsDto
{
    public required string NurseId { get; init; }
    public required string NurseName { get; init; }
    public required List<DoctorListDto> AttributedDoctors { get; init; }
}
```

---

## 5. Service Layer

### 5.1 Interface
**Location**: `backend/Services/Interfaces/INurseDoctorAttributionService.cs`

```csharp
public interface INurseDoctorAttributionService
{
    /// <summary>
    /// Get all doctors attributed to a nurse (active attributions only)
    /// </summary>
    Task<List<DoctorListDto>> GetAttributedDoctorsAsync(string nurseId);

    /// <summary>
    /// Get all nurses attributed to a doctor (active attributions only)
    /// </summary>
    Task<List<NurseListDto>> GetAttributedNursesAsync(string doctorId);

    /// <summary>
    /// Check if a nurse is attributed to a specific doctor
    /// </summary>
    Task<bool> IsNurseAttributedToDoctorAsync(string nurseId, string doctorId);

    /// <summary>
    /// Assign a doctor to a nurse (admin only)
    /// </summary>
    Task<AttributionDto> AssignDoctorToNurseAsync(string nurseId, string doctorId);

    /// <summary>
    /// Remove a doctor from a nurse (admin only)
    /// </summary>
    Task RemoveDoctorFromNurseAsync(string nurseId, string doctorId);
}
```

### 5.2 Implementation
**Location**: `backend/Services/NurseDoctorAttributionService.cs`

Key implementation details:
- Validate nurse and doctor exist and are active
- Check for duplicate attributions
- Use `IUserContext` to get the admin user for `AssignedBy`
- Emit domain events

---

## 6. Controller Endpoints

### 6.1 Extend NursesController
**Location**: `backend/Controllers/NursesController.cs`

Add endpoints:

```csharp
/// <summary>
/// Get all doctors attributed to this nurse
/// </summary>
[HttpGet("{nurseId}/doctors")]
[ProducesResponseType(typeof(List<DoctorListDto>), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<List<DoctorListDto>>> GetAttributedDoctors(string nurseId)

/// <summary>
/// Assign a doctor to this nurse (Admin only)
/// </summary>
[HttpPost("{nurseId}/doctors/{doctorId}")]
[Authorize(Roles = "admin")]
[ProducesResponseType(typeof(AttributionDto), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<AttributionDto>> AssignDoctor(string nurseId, string doctorId)

/// <summary>
/// Remove a doctor from this nurse (Admin only)
/// </summary>
[HttpDelete("{nurseId}/doctors/{doctorId}")]
[Authorize(Roles = "admin")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> RemoveDoctor(string nurseId, string doctorId)
```

### 6.2 Extend DoctorsController
**Location**: `backend/Controllers/DoctorsController.cs`

Add endpoint:

```csharp
/// <summary>
/// Get all nurses attributed to this doctor
/// </summary>
[HttpGet("{doctorId}/nurses")]
[ProducesResponseType(typeof(List<NurseListDto>), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<List<NurseListDto>>> GetAttributedNurses(string doctorId)
```

---

## 7. Authorization / Permission Checks

### 7.1 Update AppointmentService
**Location**: `backend/Services/AppointmentService.cs`

Add validation in `ScheduleAppointmentAsync`:

```csharp
// If scheduled by a nurse, verify attribution
if (!string.IsNullOrEmpty(dto.ScheduledByNurseId))
{
    var isAttributed = await _attributionService.IsNurseAttributedToDoctorAsync(
        dto.ScheduledByNurseId,
        dto.DoctorId);

    if (!isAttributed)
    {
        throw new BusinessException("Nurse is not authorized to schedule appointments for this doctor");
    }
}
```

### 7.2 Update AppointmentRepository
**Location**: `backend/Infrastructure/Repositories/AppointmentRepository.cs`

Add method for filtered queries:

```csharp
Task<PagedResult<AppointmentListDto>> GetPagedForNurseAsync(
    string nurseId,
    List<string> attributedDoctorIds,
    AppointmentSearchParameters parameters);
```

---

## 8. Validation

### 8.1 Create Validator
**Location**: `backend/Validators/NurseDoctorAttributions/CreateAttributionDtoValidator.cs`

```csharp
public class CreateAttributionDtoValidator : AbstractValidator<CreateAttributionDto>
{
    public CreateAttributionDtoValidator(
        INurseRepository nurseRepository,
        IDoctorRepository doctorRepository,
        INurseDoctorAttributionRepository attributionRepository)
    {
        RuleFor(x => x.NurseId)
            .NotEmpty()
            .MustAsync(async (id, ct) => await nurseRepository.ExistsAsync(id))
            .WithMessage("Nurse not found")
            .MustAsync(async (id, ct) => await nurseRepository.IsActiveAsync(id))
            .WithMessage("Nurse is not active");

        RuleFor(x => x.DoctorId)
            .NotEmpty()
            .MustAsync(async (id, ct) => await doctorRepository.ExistsAsync(id))
            .WithMessage("Doctor not found")
            .MustAsync(async (id, ct) => await doctorRepository.IsActiveAsync(id))
            .WithMessage("Doctor is not active");

        RuleFor(x => x)
            .MustAsync(async (dto, ct) =>
                !await attributionRepository.IsAttributedAsync(dto.NurseId, dto.DoctorId))
            .WithMessage("This nurse is already attributed to this doctor");
    }
}
```

---

## 9. Database Migration

### 9.1 Create Migration
```bash
dotnet ef migrations add AddNurseDoctorAttribution
dotnet ef database update
```

### 9.2 Expected Table Schema

```sql
CREATE TABLE "NurseDoctorAttributions" (
    "Id" SERIAL PRIMARY KEY,
    "ExternalId" VARCHAR(50) NOT NULL UNIQUE,
    "NurseId" VARCHAR(50) NOT NULL,
    "DoctorId" VARCHAR(50) NOT NULL,
    "AssignedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "AssignedBy" VARCHAR(100),
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "CreatedBy" VARCHAR(100),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE,
    "UpdatedBy" VARCHAR(100),
    CONSTRAINT "UQ_NurseDoctorAttributions_NurseId_DoctorId" UNIQUE ("NurseId", "DoctorId")
);

CREATE INDEX "IX_NurseDoctorAttributions_NurseId" ON "NurseDoctorAttributions" ("NurseId");
CREATE INDEX "IX_NurseDoctorAttributions_DoctorId" ON "NurseDoctorAttributions" ("DoctorId");
CREATE INDEX "IX_NurseDoctorAttributions_NurseId_IsActive" ON "NurseDoctorAttributions" ("NurseId", "IsActive");
```

---

## 10. Program.cs Registration

**Location**: `backend/Program.cs`

Add:
```csharp
// Repository
builder.Services.AddScoped<INurseDoctorAttributionRepository, NurseDoctorAttributionRepository>();

// Service
builder.Services.AddScoped<INurseDoctorAttributionService, NurseDoctorAttributionService>();
```

---

## 11. API Endpoints Summary

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/v1/nurses/{nurseId}/doctors` | Get attributed doctors for nurse | Nurse/Admin |
| POST | `/api/v1/nurses/{nurseId}/doctors/{doctorId}` | Assign doctor to nurse | Admin |
| DELETE | `/api/v1/nurses/{nurseId}/doctors/{doctorId}` | Remove doctor from nurse | Admin |
| GET | `/api/v1/doctors/{doctorId}/nurses` | Get attributed nurses for doctor | Doctor/Admin |

---

## 12. Business Rules

1. **Attribution Uniqueness**: A nurse can only be attributed to a doctor once (unique constraint on NurseId + DoctorId)

2. **Active Status**: Only active attributions grant permissions. Deactivating an attribution revokes access without deleting history.

3. **Nurse Permissions**:
   - Can view appointments only for attributed doctors
   - Can schedule appointments only with attributed doctors
   - Cannot access any doctor data if no active attributions

4. **Doctor Permissions**:
   - Doctors can only schedule appointments for themselves
   - Doctors cannot schedule for other doctors regardless of attributions

5. **Admin Management**:
   - Only admins can create/remove attributions
   - Attributions track who made the assignment (`AssignedBy`)

6. **Cascade Behavior**:
   - Deactivating a nurse does NOT remove attributions
   - Deactivating a doctor does NOT remove attributions
   - Attributions remain for audit purposes

---

## 13. Files to Create

```
backend/
├── Domain/
│   ├── Aggregates/
│   │   └── NurseDoctorAttribution/
│   │       └── NurseDoctorAttribution.cs
│   ├── DomainEvents/
│   │   ├── NurseDoctorAttributionCreatedEvent.cs
│   │   └── NurseDoctorAttributionRemovedEvent.cs
│   └── Interfaces/
│       └── INurseDoctorAttributionRepository.cs
├── Infrastructure/
│   ├── Configurations/
│   │   └── NurseDoctorAttributionConfiguration.cs
│   └── Repositories/
│       └── NurseDoctorAttributionRepository.cs
├── DTOs/
│   └── NurseDoctorAttributions/
│       ├── AttributionDto.cs
│       └── CreateAttributionDto.cs
├── Services/
│   ├── Interfaces/
│   │   └── INurseDoctorAttributionService.cs
│   └── NurseDoctorAttributionService.cs
└── Validators/
    └── NurseDoctorAttributions/
        └── CreateAttributionDtoValidator.cs
```

---

## 14. Files to Modify

- `backend/Infrastructure/Data/AppDbContext.cs` - Add DbSet
- `backend/Controllers/NursesController.cs` - Add attribution endpoints
- `backend/Controllers/DoctorsController.cs` - Add nurses endpoint
- `backend/Services/AppointmentService.cs` - Add attribution validation
- `backend/Program.cs` - Register new services
