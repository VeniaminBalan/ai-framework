using PatientSyncHealth.Domain.Common;
using PatientSyncHealth.Domain.Enums;
using PatientSyncHealth.Domain.Exceptions;

namespace PatientSyncHealth.Domain.ValueObjects;

/// <summary>
/// Personal identification number value object with basic format validation.
/// Supports Romanian CNP and Moldovan IDNP formats (both 13 digits).
/// </summary>
public sealed class IdentificationNumber : ValueObject
{
    /// <summary>
    /// The raw identification number value.
    /// </summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>
    /// The type/country of the identification number.
    /// </summary>
    public IdentificationNumberType Type { get; private set; }

    private IdentificationNumber() { } // EF Core

    /// <summary>
    /// Creates a new identification number with validation.
    /// </summary>
    public IdentificationNumber(string value, IdentificationNumberType type)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Identification number is required");

        value = value.Trim();

        if (value.Length != 13 || !value.All(char.IsDigit))
            throw new DomainException("Identification number must be exactly 13 digits");

        Value = value;
        Type = type;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
        yield return Type;
    }

    public override string ToString() => Value;
}
