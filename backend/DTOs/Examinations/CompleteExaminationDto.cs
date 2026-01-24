using System.ComponentModel.DataAnnotations;

namespace PatientSyncHealth.DTOs.Examinations;

public record CompleteExaminationDto
{
    [MaxLength(1000)]
    public string? Diagnosis { get; init; }

    [MaxLength(4000)]
    public string? Notes { get; init; }
}
