using System.Text.RegularExpressions;
using PatientSyncHealth.Domain.Common;
using PatientSyncHealth.Domain.Exceptions;

namespace PatientSyncHealth.Domain.ValueObjects;

/// <summary>
/// International phone number value object using E.164 standard.
/// Format: +[country code][subscriber number] (7-15 digits after +)
/// Examples: +40712345678 (Romania), +37360123456 (Moldova), +14155551234 (USA)
/// </summary>
public partial class PhoneNumber : ValueObject
{
    public string Value { get; }

    private PhoneNumber() { Value = string.Empty; } // EF Core

    public PhoneNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Phone number is required");

        value = value.Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

        if (!E164PhoneRegex().IsMatch(value))
            throw new DomainException("Invalid phone number format. Expected E.164 format: +[country code][number] (e.g., +40712345678)");

        Value = value;
    }

    [GeneratedRegex(@"^\+[1-9]\d{6,14}$")]
    private static partial Regex E164PhoneRegex();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(PhoneNumber phone) => phone.Value;

    public override string ToString() => Value;
}
