using System.ComponentModel.DataAnnotations;

namespace PatientSyncHealth.DTOs.Patients;

public record RecordExaminationDto
{
    [Required]
    public required DateTime ExaminationDate { get; init; }
}
