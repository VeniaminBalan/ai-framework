namespace PatientSyncHealth.DTOs.Nurses;

public record NurseListDto
{
    public required string Id { get; init; }
    public required string FullName { get; init; }
    public required string DepartmentName { get; init; }
    public string? DepartmentCode { get; init; }
    public string? Phone { get; init; }
    public required bool IsActive { get; init; }
}
