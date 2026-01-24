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
/// JJ = County code (01-52 or 70 for new system)
/// NNN = Sequential number (001-999)
/// C = Checksum digit
/// </summary>
public sealed class RomanianCnp : IdentificationNumber
{
    private static readonly int[] ChecksumWeights = [2, 7, 9, 1, 4, 6, 3, 5, 8, 2, 7, 9];

    public override IdentificationNumberType Type => IdentificationNumberType.RO;

    public override bool EncodesGender => true;

    public override bool EncodesDateOfBirth => true;

    private RomanianCnp() { } // EF Core

    public RomanianCnp(string value)
    {
        Validate(value);
        Value = value.Trim();
    }

    protected override void Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("CNP is required");

        value = value.Trim();

        if (!IsValidFormat(value))
            throw new DomainException("CNP must be exactly 13 digits");

        if (!IsValidChecksum(value))
            throw new DomainException("CNP has invalid checksum");

        if (!IsValidStructure(value))
            throw new DomainException("CNP contains invalid date of birth");
    }

    /// <summary>
    /// Checks if the value has valid CNP format (13 digits).
    /// </summary>
    public static bool IsValidFormat(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        value = value.Trim();
        return value.Length == 13 && value.All(char.IsDigit);
    }

    public override Gender? ExtractGender()
    {
        var genderDigit = int.Parse(Value[0].ToString());
        return genderDigit switch
        {
            1 or 3 or 5 or 7 => Gender.Male,
            2 or 4 or 6 or 8 => Gender.Female,
            _ => Gender.Other
        };
    }

    public override DateTime? ExtractDateOfBirth()
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

    /// <summary>
    /// Validates the CNP checksum digit.
    /// </summary>
    public static bool IsValidChecksum(string? value)
    {
        if (!IsValidFormat(value))
            return false;

        var cnp = value!.Trim();

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

    /// <summary>
    /// Validates the CNP structure (date of birth and county code).
    /// </summary>
    public static bool IsValidStructure(string? value)
    {
        if (!IsValidFormat(value))
            return false;

        var cnp = value!.Trim();

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

            // Validate county code (01-52, or 70 for new integrated system)
            var countyCode = int.Parse(cnp.Substring(7, 2));
            if (countyCode < 1 || (countyCode > 52 && countyCode != 70))
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to extract gender from a CNP value.
    /// </summary>
    public static Gender? TryExtractGender(string? value)
    {
        if (!IsValidFormat(value))
            return null;

        var cnp = value!.Trim();
        var genderDigit = int.Parse(cnp[0].ToString());
        return genderDigit switch
        {
            1 or 3 or 5 or 7 => Gender.Male,
            2 or 4 or 6 or 8 => Gender.Female,
            _ => Gender.Other
        };
    }

    /// <summary>
    /// Attempts to extract date of birth from a CNP value.
    /// </summary>
    public static DateTime? TryExtractDateOfBirth(string? value)
    {
        if (!IsValidFormat(value) || !IsValidStructure(value))
            return null;

        var cnp = value!.Trim();

        try
        {
            var genderDigit = int.Parse(cnp[0].ToString());
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

            return new DateTime(century + year, month, day);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Reconstructs a RomanianCnp without validation (for EF Core loading).
    /// </summary>
    internal static RomanianCnp Reconstruct(string value)
    {
        return new RomanianCnp { Value = value };
    }
}
