namespace PatientSyncHealth.DTOs.Examinations;

public record ExaminationListDto
{
    public required string Id { get; init; }
    public required string PatientId { get; init; }
    public required string DoctorId { get; init; }
    public required DateTime ExaminationDate { get; init; }
    public string? Diagnosis { get; init; }
    public required bool IsCompleted { get; init; }
}
