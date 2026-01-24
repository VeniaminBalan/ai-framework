using PatientSyncHealth.Domain.Common;
using PatientSyncHealth.Domain.Enums;
using PatientSyncHealth.Domain.Exceptions;

namespace PatientSyncHealth.Domain.ValueObjects;

/// <summary>
/// Abstract base class for personal identification numbers across different countries.
/// Each country has its own validation rules and data extraction capabilities.
/// </summary>
public abstract class IdentificationNumber : ValueObject
{
    /// <summary>
    /// The raw identification number value.
    /// </summary>
    public string Value { get; protected set; } = string.Empty;

    /// <summary>
    /// The type/country of the identification number.
    /// </summary>
    public abstract IdentificationNumberType Type { get; }

    protected IdentificationNumber() { } // EF Core

    /// <summary>
    /// Validates the identification number format and structure.
    /// Throws DomainException if validation fails.
    /// </summary>
    protected abstract void Validate(string value);

    /// <summary>
    /// Attempts to extract the gender from the identification number.
    /// Returns null if the identification number type does not encode gender.
    /// </summary>
    public virtual Gender? ExtractGender() => null;

    /// <summary>
    /// Attempts to extract the date of birth from the identification number.
    /// Returns null if the identification number type does not encode date of birth.
    /// </summary>
    public virtual DateTime? ExtractDateOfBirth() => null;

    /// <summary>
    /// Indicates whether this identification number type encodes gender information.
    /// </summary>
    public virtual bool EncodesGender => false;

    /// <summary>
    /// Indicates whether this identification number type encodes date of birth information.
    /// </summary>
    public virtual bool EncodesDateOfBirth => false;

    /// <summary>
    /// Factory method to create the appropriate identification number instance based on type.
    /// </summary>
    public static IdentificationNumber Create(string value, IdentificationNumberType type)
    {
        return type switch
        {
            IdentificationNumberType.RO => new RomanianCnp(value),
            IdentificationNumberType.MD => new MoldovanIdnp(value),
            _ => throw new DomainException($"Unsupported identification number type: {type}")
        };
    }

    /// <summary>
    /// Reconstructs a IdentificationNumber from stored values (for EF Core).
    /// Does not re-validate since the data was validated when originally stored.
    /// </summary>
    internal static IdentificationNumber Reconstruct(string value, IdentificationNumberType type)
    {
        return type switch
        {
            IdentificationNumberType.RO => RomanianCnp.Reconstruct(value),
            IdentificationNumberType.MD => MoldovanIdnp.Reconstruct(value),
            _ => throw new DomainException($"Unsupported identification number type: {type}")
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
        yield return Type;
    }

    public static implicit operator string(IdentificationNumber pin) => pin.Value;

    public override string ToString() => Value;
}
