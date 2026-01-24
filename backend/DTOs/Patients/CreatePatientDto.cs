using System.ComponentModel.DataAnnotations;
using PatientSyncHealth.Domain.Enums;
using PatientSyncHealth.DTOs.Common;

namespace PatientSyncHealth.DTOs.Patients;

public record CreatePatientDto
{
    [Required]
    [MaxLength(100)]
    public required string FirstName { get; init; }

    [Required]
    [MaxLength(100)]
    public required string LastName { get; init; }

    [Required]
    public required IdentificationNumberDto IdentificationNumber { get; init; }

    [Required]
    public required DateTime DateOfBirth { get; init; }

    [Required]
    public required Gender Gender { get; init; }

    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; init; }

    [MaxLength(20)]
    public string? Phone { get; init; }

    public AddressDto? Address { get; init; }

    [Required]
    public required ExaminationFrequency ExaminationFrequency { get; init; }
}
