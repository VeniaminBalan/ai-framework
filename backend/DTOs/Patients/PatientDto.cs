using PatientSyncHealth.Domain.Enums;
using PatientSyncHealth.DTOs.Common;

namespace PatientSyncHealth.DTOs.Patients;

public record PatientDto
{
    public required string Id { get; init; }

    // Personal Information
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string FullName { get; init; }
    public required IdentificationNumberDto IdentificationNumber { get; init; }
    public required DateTime DateOfBirth { get; init; }
    public required int Age { get; init; }
    public required Gender Gender { get; init; }

    // Contact Information
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public AddressDto? Address { get; init; }

    // Examination Schedule
    public required ExaminationFrequency ExaminationFrequency { get; init; }
    public DateTime? LastExaminationDate { get; init; }
    public DateTime? NextExaminationDate { get; init; }
    public required bool IsOverdueForExamination { get; init; }

    // Status
    public required bool IsActive { get; init; }

    // Audit
    public required DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
}
