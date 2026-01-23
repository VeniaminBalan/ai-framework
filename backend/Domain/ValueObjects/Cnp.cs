using PatientSyncHealth.Domain.Common;
using PatientSyncHealth.Domain.Enums;
using PatientSyncHealth.Domain.Exceptions;

namespace PatientSyncHealth.Domain.ValueObjects;

/// <summary>
/// Romanian CNP (Cod Numeric Personal) - 13-digit personal identification number.
/// Format: SAALLZZJJNNNC
/// S = Gender/Century (1-8)
/// AA = Year of birth (last 2 digits)
/// LL = Month of birth (01-12)
/// ZZ = Day of birth (01-31)
/// JJ = County code (01-52)
/// NNN = Sequential number (001-999)
/// C = Checksum digit
/// </summary>
public class Cnp : ValueObject
{
    private static readonly int[] ChecksumWeights = [2, 7, 9, 1, 4, 6, 3, 5, 8, 2, 7, 9];

    public string Value { get; }

    private Cnp() { Value = string.Empty; } // EF Core

    public Cnp(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("CNP is required");

        value = value.Trim();

        if (value.Length != 13)
            throw new DomainException("CNP must be exactly 13 digits");

        if (!value.All(char.IsDigit))
            throw new DomainException("CNP must contain only digits");

        if (!IsValidChecksum(value))
            throw new DomainException("CNP has invalid checksum");

        if (!IsValidDate(value))
            throw new DomainException("CNP contains invalid date of birth");

        Value = value;
    }

    public Gender ExtractGender()
    {
        var genderDigit = int.Parse(Value[0].ToString());
        return genderDigit switch
        {
            1 or 3 or 5 or 7 => Gender.Male,
            2 or 4 or 6 or 8 => Gender.Female,
            _ => Gender.Other
        };
    }

    public DateTime ExtractDateOfBirth()
    {
        var genderDigit = int.Parse(Value[0].ToString());
        var year = int.Parse(Value.Substring(1, 2));
        var month = int.Parse(Value.Substring(3, 2));
        var day = int.Parse(Value.Substring(5, 2));

        var century = genderDigit switch
        {
            1 or 2 => 1900,
            3 or 4 => 1800,
            5 or 6 => 2000,
            7 or 8 => 2000, // Resident foreigners
            _ => 1900
        };

        return new DateTime(century + year, month, day);
    }

    public string ExtractCountyCode()
    {
        return Value.Substring(7, 2);
    }

    private static bool IsValidChecksum(string cnp)
    {
        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            sum += int.Parse(cnp[i].ToString()) * ChecksumWeights[i];
        }

        var remainder = sum % 11;
        var expectedChecksum = remainder == 10 ? 1 : remainder;
        var actualChecksum = int.Parse(cnp[12].ToString());

        return expectedChecksum == actualChecksum;
    }

    private static bool IsValidDate(string cnp)
    {
        try
        {
            var genderDigit = int.Parse(cnp[0].ToString());
            if (genderDigit < 1 || genderDigit > 8)
                return false;

            var year = int.Parse(cnp.Substring(1, 2));
            var month = int.Parse(cnp.Substring(3, 2));
            var day = int.Parse(cnp.Substring(5, 2));

            var century = genderDigit switch
            {
                1 or 2 => 1900,
                3 or 4 => 1800,
                5 or 6 => 2000,
                7 or 8 => 2000,
                _ => 1900
            };

            var fullYear = century + year;

            // Validate month
            if (month < 1 || month > 12)
                return false;

            // Validate day based on month
            var daysInMonth = DateTime.DaysInMonth(fullYear, month);
            if (day < 1 || day > daysInMonth)
                return false;

            // Validate county code (01-52, with some gaps)
            var countyCode = int.Parse(cnp.Substring(7, 2));
            if (countyCode < 1 || countyCode > 52)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(Cnp cnp) => cnp.Value;

    public override string ToString() => Value;
}
