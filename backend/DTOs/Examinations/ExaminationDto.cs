namespace PatientSyncHealth.DTOs.Examinations;

public record ExaminationDto
{
    public required string Id { get; init; }
    public required string PatientId { get; init; }
    public required string DoctorId { get; init; }
    public required DateTime ExaminationDate { get; init; }
    public string? Diagnosis { get; init; }
    public string? Notes { get; init; }
    public required bool IsCompleted { get; init; }
    public required DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
}
