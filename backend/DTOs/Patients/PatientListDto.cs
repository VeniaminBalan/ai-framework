namespace PatientSyncHealth.DTOs.Patients;

public record PatientListDto
{
    public required string Id { get; init; }
    public required string FullName { get; init; }
    public required string Cnp { get; init; }
    public required int Age { get; init; }
    public string? Phone { get; init; }
    public DateTime? NextExaminationDate { get; init; }
    public required bool IsOverdue { get; init; }
    public required bool IsActive { get; init; }
}
