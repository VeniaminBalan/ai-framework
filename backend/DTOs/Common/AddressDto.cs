using System.ComponentModel.DataAnnotations;

namespace PatientSyncHealth.DTOs.Common;

public record AddressDto
{
    [Required]
    [MaxLength(200)]
    public required string Street { get; init; }

    [Required]
    [MaxLength(100)]
    public required string City { get; init; }

    [Required]
    [MaxLength(100)]
    public required string County { get; init; }

    [MaxLength(6)]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Postal code must be 6 digits")]
    public string? PostalCode { get; init; }

    [MaxLength(100)]
    public string Country { get; init; } = "Romania";
}
