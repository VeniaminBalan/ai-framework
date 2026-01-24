using PatientSyncHealth.Domain.Enums;

namespace PatientSyncHealth.Validators.CustomValidators;

/// <summary>
/// Validation for personal identification numbers.
/// Validates that the value is exactly 13 digits.
/// </summary>
public static class IdentificationNumberValidator
{
    /// <summary>
    /// Checks if the value has valid format (13 digits).
    /// </summary>
    public static bool IsValidFormat(string? value, IdentificationNumberType type)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        value = value.Trim();

        return value.Length == 13 && value.All(char.IsDigit);
    }
}
