namespace PatientSyncHealth.DTOs.Doctors;

public record DoctorDto
{
    public required string Id { get; init; }
    public string? KeycloakUserId { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string FullName { get; init; }
    public required string Specialization { get; init; }
    public required string LicenseNumber { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public required bool IsActive { get; init; }
    public required DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
}
