using System.Text.RegularExpressions;
using PatientSyncHealth.Domain.Common;
using PatientSyncHealth.Domain.Exceptions;

namespace PatientSyncHealth.Domain.ValueObjects;

/// <summary>
/// Romanian phone number value object.
/// Accepts formats: +40xxxxxxxxx, 0xxxxxxxxx
/// Stores in normalized format: +40xxxxxxxxx
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

        if (!IsValidRomanianPhoneNumber(value))
            throw new DomainException("Invalid Romanian phone number format. Expected +40xxxxxxxxx or 0xxxxxxxxx");

        Value = NormalizePhoneNumber(value);
    }

    private static bool IsValidRomanianPhoneNumber(string phone)
    {
        // Romanian mobile: +407xxxxxxxx or 07xxxxxxxx
        // Romanian landline: +402xxxxxxxx or 02xxxxxxxx, +403xxxxxxxx or 03xxxxxxxx
        return RomanianPhoneRegex().IsMatch(phone);
    }

    private static string NormalizePhoneNumber(string phone)
    {
        if (phone.StartsWith("+40"))
            return phone;

        if (phone.StartsWith("0"))
            return "+4" + phone;

        return phone;
    }

    [GeneratedRegex(@"^(\+40|0)[2-9]\d{8}$")]
    private static partial Regex RomanianPhoneRegex();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(PhoneNumber phone) => phone.Value;

    public override string ToString() => Value;
}
