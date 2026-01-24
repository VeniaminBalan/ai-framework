using System.Text.RegularExpressions;
using PatientSyncHealth.Domain.Common;
using PatientSyncHealth.Domain.Exceptions;

namespace PatientSyncHealth.Domain.ValueObjects;

public sealed partial class LicenseNumber : ValueObject
{
    public string Value { get; private set; } = string.Empty;

    private LicenseNumber() { } // EF Core

    public LicenseNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("License number is required");

        value = value.Trim().ToUpperInvariant();

        if (!LicenseNumberRegex().IsMatch(value))
            throw new DomainException("License number must be alphanumeric and between 5-20 characters");

        Value = value;
    }

    [GeneratedRegex(@"^[A-Z0-9]{5,20}$")]
    private static partial Regex LicenseNumberRegex();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
