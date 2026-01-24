using System.ComponentModel.DataAnnotations;
using PatientSyncHealth.Domain.Enums;

namespace PatientSyncHealth.DTOs.Common;

public record IdentificationNumberDto
{
    [Required]
    [StringLength(13, MinimumLength = 13, ErrorMessage = "Value must be exactly 13 digits")]
    [RegularExpression(@"^\d{13}$", ErrorMessage = "Value must contain only digits")]
    public required string Value { get; init; }

    [Required]
    public required IdentificationNumberType Type { get; init; }
}
