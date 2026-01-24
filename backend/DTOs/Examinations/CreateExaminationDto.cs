using System.ComponentModel.DataAnnotations;

namespace PatientSyncHealth.DTOs.Examinations;

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
}
