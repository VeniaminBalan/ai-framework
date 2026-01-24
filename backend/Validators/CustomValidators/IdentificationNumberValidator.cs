using PatientSyncHealth.Domain.Enums;
using PatientSyncHealth.Domain.ValueObjects;

namespace PatientSyncHealth.Validators.CustomValidators;

/// <summary>
/// Validation facade for personal identification numbers from different countries.
/// Delegates to the respective value objects which are the single source of truth for validation.
/// </summary>
public static class IdentificationNumberValidator
{
    /// <summary>
    /// Checks if the value has valid format for the specified identification number type.
    /// </summary>
    public static bool IsValidFormat(string? value, IdentificationNumberType type)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return type switch
        {
            IdentificationNumberType.RO => RomanianCnp.IsValidFormat(value),
            IdentificationNumberType.MD => MoldovanIdnp.IsValidFormat(value),
            _ => false
        };
    }

    /// <summary>
    /// Validates the checksum for the specified identification number type.
    /// </summary>
    public static bool IsValidChecksum(string? value, IdentificationNumberType type)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return type switch
        {
            IdentificationNumberType.RO => RomanianCnp.IsValidChecksum(value),
            IdentificationNumberType.MD => MoldovanIdnp.IsValidChecksum(value),
            _ => false
        };
    }

    /// <summary>
    /// Validates the structure for the specified identification number type.
    /// </summary>
    public static bool IsValidStructure(string? value, IdentificationNumberType type)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return type switch
        {
            IdentificationNumberType.RO => RomanianCnp.IsValidStructure(value),
            IdentificationNumberType.MD => MoldovanIdnp.IsValidStructure(value),
            _ => false
        };
    }

    /// <summary>
    /// Extracts date of birth from the identification number if the type encodes it.
    /// </summary>
    public static DateTime? ExtractDateOfBirth(string? value, IdentificationNumberType type)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return type switch
        {
            IdentificationNumberType.RO => RomanianCnp.TryExtractDateOfBirth(value),
            IdentificationNumberType.MD => null, // IDNP does not encode date of birth
            _ => null
        };
    }

    /// <summary>
    /// Extracts gender from the identification number if the type encodes it.
    /// </summary>
    public static Gender? ExtractGender(string? value, IdentificationNumberType type)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return type switch
        {
            IdentificationNumberType.RO => RomanianCnp.TryExtractGender(value),
            IdentificationNumberType.MD => null, // IDNP does not encode gender
            _ => null
        };
    }

    /// <summary>
    /// Returns whether the identification number type encodes date of birth.
    /// </summary>
    public static bool EncodesDateOfBirth(IdentificationNumberType type)
    {
        return type == IdentificationNumberType.RO;
    }

    /// <summary>
    /// Returns whether the identification number type encodes gender.
    /// </summary>
    public static bool EncodesGender(IdentificationNumberType type)
    {
        return type == IdentificationNumberType.RO;
    }
}
