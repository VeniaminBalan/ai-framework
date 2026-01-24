namespace PatientSyncHealth.DTOs.Doctors;

public record DoctorListDto
{
    public required string Id { get; init; }
    public required string FullName { get; init; }
    public required string Specialization { get; init; }
    public required string LicenseNumber { get; init; }
    public string? Phone { get; init; }
    public required bool IsActive { get; init; }
}
