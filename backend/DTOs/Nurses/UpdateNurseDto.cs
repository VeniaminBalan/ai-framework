using System.ComponentModel.DataAnnotations;

namespace PatientSyncHealth.DTOs.Nurses;

public record UpdateNurseDto
{
    [Required]
    [MaxLength(100)]
    public required string FirstName { get; init; }

    [Required]
    [MaxLength(100)]
    public required string LastName { get; init; }

    [Required]
    [MaxLength(100)]
    public required string DepartmentName { get; init; }

    [MaxLength(20)]
    public string? DepartmentCode { get; init; }

    [MaxLength(50)]
    public string? KeycloakUserId { get; init; }

    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; init; }

    [MaxLength(20)]
    public string? Phone { get; init; }
}
