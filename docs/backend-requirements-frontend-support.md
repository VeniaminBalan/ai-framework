# Backend Requirements: Frontend Support Enhancements

## Overview
This document outlines backend changes needed to fully support the frontend features. These are gaps identified between the frontend specification and the current backend implementation.

---

## 1. User Identity / Current User Endpoint

### Problem
Frontend needs to know who the current user is (Doctor or Nurse) and get their profile after login.

### Solution
Create a `/api/v1/me` endpoint that returns the current user's profile based on their Keycloak token.

### 1.1 New DTO
**Location**: `backend/DTOs/Auth/CurrentUserDto.cs`

```csharp
public record CurrentUserDto
{
    public required string Id { get; init; }              // ExternalId of Doctor/Nurse
    public required string KeycloakUserId { get; init; }
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public required string Role { get; init; }            // "doctor" or "nurse"

    // Doctor-specific (null if nurse)
    public string? Specialization { get; init; }
    public string? LicenseNumber { get; init; }

    // Nurse-specific (null if doctor)
    public string? DepartmentName { get; init; }
    public List<AttributedDoctorDto>? AttributedDoctors { get; init; }
}

public record AttributedDoctorDto
{
    public required string Id { get; init; }
    public required string FullName { get; init; }
    public required string Specialization { get; init; }
}
```

### 1.2 New Service
**Location**: `backend/Services/Interfaces/IUserIdentityService.cs`

```csharp
public interface IUserIdentityService
{
    Task<CurrentUserDto> GetCurrentUserAsync();
    Task<string?> GetDoctorIdByKeycloakUserIdAsync(string keycloakUserId);
    Task<string?> GetNurseIdByKeycloakUserIdAsync(string keycloakUserId);
}
```

### 1.3 New Controller
**Location**: `backend/Controllers/MeController.cs`

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class MeController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CurrentUserDto>> GetCurrentUser()
}
```

### 1.4 Repository Updates
Add to `IDoctorRepository`:
```csharp
Task<Doctor?> GetByKeycloakUserIdAsync(string keycloakUserId);
```

Add to `INurseRepository`:
```csharp
Task<Nurse?> GetByKeycloakUserIdAsync(string keycloakUserId);
```

---

## 2. Create Examination from Appointment

### Problem
When a doctor starts an examination from an appointment, the system should:
1. Create the examination record
2. Link it to the appointment
3. Auto-complete the appointment
4. Update patient's examination schedule

### Solution
Add optional `AppointmentId` to `CreateExaminationDto` and handle the linking in service.

### 2.1 Update CreateExaminationDto
**Location**: `backend/DTOs/Examinations/CreateExaminationDto.cs`

```csharp
public record CreateExaminationDto
{
    [Required]
    public required string PatientId { get; init; }

    [Required]
    public required string DoctorId { get; init; }

    [Required]
    public required DateTime ExaminationDate { get; init; }

    [MaxLength(1000)]
    public string? Diagnosis { get; init; }

    [MaxLength(4000)]
    public string? Notes { get; init; }

    /// <summary>
    /// Optional: Link to an existing appointment.
    /// If provided, the appointment will be auto-completed.
    /// </summary>
    public string? AppointmentId { get; init; }
}
```

### 2.2 Update ExaminationService
**Location**: `backend/Services/ExaminationService.cs`

Add to `CreateExaminationAsync`:
```csharp
// If linked to an appointment, complete it
if (!string.IsNullOrWhiteSpace(dto.AppointmentId))
{
    var appointment = await _appointmentRepository.GetByIdAsync(dto.AppointmentId);
    if (appointment != null)
    {
        // Validate appointment belongs to same patient and doctor
        if (appointment.PatientId != dto.PatientId)
            throw new BusinessException("Appointment patient does not match examination patient");
        if (appointment.DoctorId != dto.DoctorId)
            throw new BusinessException("Appointment doctor does not match examination doctor");

        // Start if not started, then complete
        if (appointment.Status == AppointmentStatus.Scheduled ||
            appointment.Status == AppointmentStatus.Confirmed)
        {
            appointment.Start();
        }
        appointment.Complete(examination.ExternalId);
        _appointmentRepository.Update(appointment);
    }
}
```

---

## 3. Patient Search Enhancements

### Problem
Frontend needs to filter patients by ExaminationFrequency.

### 3.1 Update PatientSearchParameters
**Location**: `backend/DTOs/Patients/PatientSearchParameters.cs`

```csharp
public record PatientSearchParameters
{
    // ... existing fields ...

    /// <summary>
    /// Filter by examination frequency
    /// </summary>
    public ExaminationFrequency? ExaminationFrequency { get; init; }

    /// <summary>
    /// Filter patients with NextExaminationDate within N days
    /// </summary>
    public int? ExaminationDueWithinDays { get; init; }
}
```

### 3.2 Update PatientRepository.GetPagedAsync
Add filter logic:
```csharp
if (parameters.ExaminationFrequency.HasValue)
{
    query = query.Where(p => p.ExaminationFrequency == parameters.ExaminationFrequency.Value);
}

if (parameters.ExaminationDueWithinDays.HasValue)
{
    var cutoffDate = DateTime.Today.AddDays(parameters.ExaminationDueWithinDays.Value);
    query = query.Where(p =>
        p.IsActive &&
        p.NextExaminationDate.HasValue &&
        p.NextExaminationDate.Value <= cutoffDate);
}
```

---

## 4. Appointments for Multiple Doctors (Nurse View)

### Problem
Nurses need to view appointments for multiple attributed doctors at once.

### 4.1 Update AppointmentSearchParameters
**Location**: `backend/DTOs/Appointments/AppointmentSearchParameters.cs`

```csharp
public record AppointmentSearchParameters
{
    // ... existing fields ...

    /// <summary>
    /// Single doctor filter (existing)
    /// </summary>
    public string? DoctorId { get; init; }

    /// <summary>
    /// Multiple doctors filter (for nurse view of attributed doctors)
    /// Comma-separated list of doctor IDs
    /// </summary>
    public string? DoctorIds { get; init; }
}
```

### 4.2 Update AppointmentRepository.GetPagedAsync
```csharp
if (!string.IsNullOrWhiteSpace(parameters.DoctorIds))
{
    var doctorIdList = parameters.DoctorIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
    query = query.Where(a => doctorIdList.Contains(a.DoctorId));
}
else if (!string.IsNullOrWhiteSpace(parameters.DoctorId))
{
    query = query.Where(a => a.DoctorId == parameters.DoctorId);
}
```

---

## 5. Patient Appointments Endpoint

### Problem
Frontend needs to view appointment history for a specific patient.

### 5.1 Add to AppointmentsController
```csharp
/// <summary>
/// Get all appointments for a specific patient
/// </summary>
[HttpGet("patient/{patientId}")]
[ProducesResponseType(typeof(List<AppointmentListDto>), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<List<AppointmentListDto>>> GetByPatientId(string patientId)
{
    var appointments = await _appointmentService.GetByPatientIdAsync(patientId);
    return Ok(appointments);
}
```

### 5.2 Add to IAppointmentService
```csharp
Task<List<AppointmentListDto>> GetByPatientIdAsync(string patientId);
```

### 5.3 Add to IAppointmentRepository
```csharp
Task<List<AppointmentListDto>> GetByPatientIdAsync(string patientId);
```

---

## 6. Doctor Authorization (Self-Only Operations)

### Problem
Doctors should only be able to:
- View/manage their own appointments
- Schedule appointments only for themselves

### 6.1 Add Authorization Checks in AppointmentService

**ScheduleAppointmentAsync**:
```csharp
// If current user is a doctor, they can only schedule for themselves
var currentUser = await _userIdentityService.GetCurrentUserAsync();
if (currentUser.Role == "doctor" && currentUser.Id != dto.DoctorId)
{
    throw new BusinessException("Doctors can only schedule appointments for themselves");
}
```

**RescheduleAppointmentAsync, ConfirmAppointmentAsync, etc.**:
```csharp
// Verify doctor owns this appointment
var currentUser = await _userIdentityService.GetCurrentUserAsync();
if (currentUser.Role == "doctor" && currentUser.Id != appointment.DoctorId)
{
    throw new ForbiddenException("You can only manage your own appointments");
}
```

### 6.2 New Exception
**Location**: `backend/Middleware/Exceptions/ForbiddenException.cs`

```csharp
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}
```

Update exception handling middleware to return 403 for ForbiddenException.

---

## 7. Enriched List DTOs (Include Related Names)

### Problem
Frontend needs patient/doctor names in list views without making extra API calls.

### 7.1 Update AppointmentListDto
**Location**: `backend/DTOs/Appointments/AppointmentListDto.cs`

```csharp
public record AppointmentListDto
{
    public required string Id { get; init; }
    public required string PatientId { get; init; }
    public required string PatientName { get; init; }      // NEW
    public required string DoctorId { get; init; }
    public required string DoctorName { get; init; }       // NEW
    public required DateTime ScheduledDateTime { get; init; }
    public required TimeSpan Duration { get; init; }
    public required AppointmentPurpose Purpose { get; init; }
    public required AppointmentStatus Status { get; init; }
}
```

### 7.2 Update ExaminationListDto
**Location**: `backend/DTOs/Examinations/ExaminationListDto.cs`

```csharp
public record ExaminationListDto
{
    public required string Id { get; init; }
    public required string PatientId { get; init; }
    public required string PatientName { get; init; }      // NEW
    public required string DoctorId { get; init; }
    public required string DoctorName { get; init; }       // NEW
    public required DateTime ExaminationDate { get; init; }
    public string? Diagnosis { get; init; }
    public required bool IsCompleted { get; init; }
}
```

### 7.3 Update Repository Queries
Update `AppointmentRepository.GetPagedAsync` and `ExaminationRepository.GetPagedAsync` to join with Patient and Doctor tables to get names.

**Option A**: Use navigation properties and Include
**Option B**: Use raw SQL or separate queries
**Option C**: Use a view or stored procedure

Recommended: Create a query that joins the tables:
```csharp
var items = await query
    .Join(_context.Patients, a => a.PatientId, p => p.ExternalId, (a, p) => new { Appointment = a, Patient = p })
    .Join(_context.Doctors, ap => ap.Appointment.DoctorId, d => d.ExternalId, (ap, d) => new { ap.Appointment, ap.Patient, Doctor = d })
    .Select(x => new AppointmentListDto
    {
        Id = x.Appointment.ExternalId,
        PatientId = x.Appointment.PatientId,
        PatientName = x.Patient.FirstName + " " + x.Patient.LastName,
        DoctorId = x.Appointment.DoctorId,
        DoctorName = x.Doctor.FirstName + " " + x.Doctor.LastName,
        // ... other fields
    })
    .ToListAsync();
```

---

## 8. Today's Appointments Shortcut

### Problem
Doctor dashboard needs a quick "today's appointments" view.

### 8.1 Add Endpoint to AppointmentsController
```csharp
/// <summary>
/// Get today's appointments for a doctor (convenience endpoint)
/// </summary>
[HttpGet("doctor/{doctorId}/today")]
[ProducesResponseType(typeof(List<AppointmentListDto>), StatusCodes.Status200OK)]
public async Task<ActionResult<List<AppointmentListDto>>> GetDoctorTodayAppointments(string doctorId)
{
    var today = DateTime.Today;
    var tomorrow = today.AddDays(1);
    var appointments = await _appointmentService.GetDoctorCalendarAsync(doctorId, today, tomorrow);
    return Ok(appointments);
}
```

---

## 9. Patient Details with History

### Problem
Frontend needs patient details including examination and appointment history.

### 9.1 New DTO
**Location**: `backend/DTOs/Patients/PatientDetailDto.cs`

```csharp
public record PatientDetailDto : PatientDto
{
    public List<ExaminationListDto> RecentExaminations { get; init; } = [];
    public List<AppointmentListDto> UpcomingAppointments { get; init; } = [];
    public List<AppointmentListDto> PastAppointments { get; init; } = [];
}
```

### 9.2 New Endpoint
```csharp
/// <summary>
/// Get patient with examination and appointment history
/// </summary>
[HttpGet("{id}/details")]
[ProducesResponseType(typeof(PatientDetailDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<ActionResult<PatientDetailDto>> GetDetails(string id)
```

---

## 10. Appointment Status Transitions Validation

### Problem
Need to ensure valid status transitions and proper authorization.

### 10.1 Valid Transitions
```
Scheduled → Confirmed → InProgress → Completed
    ↓           ↓           ↓
 Cancelled  Cancelled   Cancelled
    ↓           ↓
  NoShow     NoShow
```

### 10.2 Who Can Do What
| Action | Doctor | Nurse |
|--------|--------|-------|
| Schedule | Own only | Attributed doctors |
| Confirm | Own appointments | Attributed doctors |
| Start | Own appointments | No |
| Complete | Own appointments | No |
| Cancel | Own appointments | Attributed doctors |
| Reschedule | Own appointments | Attributed doctors |

Add these checks in AppointmentService methods.

---

## 11. Files Summary

### New Files to Create
```
backend/
├── DTOs/
│   ├── Auth/
│   │   └── CurrentUserDto.cs
│   └── Patients/
│       └── PatientDetailDto.cs
├── Services/
│   ├── Interfaces/
│   │   └── IUserIdentityService.cs
│   └── UserIdentityService.cs
├── Controllers/
│   └── MeController.cs
└── Middleware/
    └── Exceptions/
        └── ForbiddenException.cs
```

### Files to Modify
```
backend/
├── DTOs/
│   ├── Patients/
│   │   └── PatientSearchParameters.cs      # Add ExaminationFrequency filter
│   ├── Appointments/
│   │   ├── AppointmentSearchParameters.cs  # Add DoctorIds filter
│   │   └── AppointmentListDto.cs           # Add PatientName, DoctorName
│   └── Examinations/
│       ├── CreateExaminationDto.cs         # Add AppointmentId
│       └── ExaminationListDto.cs           # Add PatientName, DoctorName
├── Domain/
│   └── Interfaces/
│       ├── IDoctorRepository.cs            # Add GetByKeycloakUserIdAsync
│       └── INurseRepository.cs             # Add GetByKeycloakUserIdAsync
├── Infrastructure/
│   └── Repositories/
│       ├── DoctorRepository.cs             # Implement GetByKeycloakUserIdAsync
│       ├── NurseRepository.cs              # Implement GetByKeycloakUserIdAsync
│       ├── AppointmentRepository.cs        # Update queries for enriched DTOs
│       ├── ExaminationRepository.cs        # Update queries for enriched DTOs
│       └── PatientRepository.cs            # Add ExaminationFrequency filter
├── Services/
│   ├── ExaminationService.cs               # Handle AppointmentId linking
│   ├── AppointmentService.cs               # Add authorization checks
│   └── PatientService.cs                   # Add GetDetailsAsync
├── Controllers/
│   ├── AppointmentsController.cs           # Add patient/{id} and today endpoints
│   └── PatientsController.cs               # Add details endpoint
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs      # Handle ForbiddenException
└── Program.cs                              # Register new services
```

---

## 12. API Endpoints Summary (New/Modified)

| Method | Endpoint | Description | New/Modified |
|--------|----------|-------------|--------------|
| GET | `/api/v1/me` | Get current user profile | New |
| GET | `/api/v1/appointments/patient/{patientId}` | Get patient's appointments | New |
| GET | `/api/v1/appointments/doctor/{doctorId}/today` | Get doctor's today appointments | New |
| GET | `/api/v1/patients/{id}/details` | Get patient with history | New |
| GET | `/api/v1/patients` | Add ExaminationFrequency filter | Modified |
| GET | `/api/v1/appointments` | Add DoctorIds filter | Modified |
| POST | `/api/v1/examinations` | Add AppointmentId support | Modified |
