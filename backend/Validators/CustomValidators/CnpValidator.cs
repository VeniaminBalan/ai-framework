using PatientSyncHealth.Domain.Enums;

namespace PatientSyncHealth.Validators.CustomValidators;

/// <summary>
/// CNP validation helper for FluentValidation rules.
/// Replicates the validation logic from the Cnp value object for use in validators.
/// </summary>
public static class CnpValidator
{
    private static readonly int[] ChecksumWeights = [2, 7, 9, 1, 4, 6, 3, 5, 8, 2, 7, 9];

    public static bool IsValidFormat(string? cnp)
    {
        if (string.IsNullOrWhiteSpace(cnp))
            return false;

        cnp = cnp.Trim();
        return cnp.Length == 13 && cnp.All(char.IsDigit);
    }

    public static bool IsValidChecksum(string? cnp)
    {
        if (!IsValidFormat(cnp))
            return false;

        cnp = cnp!.Trim();

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

    public static bool IsValidDate(string? cnp)
    {
        if (!IsValidFormat(cnp))
            return false;

        cnp = cnp!.Trim();

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

            if (month < 1 || month > 12)
                return false;

            var daysInMonth = DateTime.DaysInMonth(fullYear, month);
            if (day < 1 || day > daysInMonth)
                return false;

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

    public static DateTime? ExtractDateOfBirth(string? cnp)
    {
        if (!IsValidFormat(cnp) || !IsValidDate(cnp))
            return null;

        cnp = cnp!.Trim();

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

    public static Gender? ExtractGender(string? cnp)
    {
        if (!IsValidFormat(cnp))
            return null;

        cnp = cnp!.Trim();

        var genderDigit = int.Parse(cnp[0].ToString());
        return genderDigit switch
        {
            1 or 3 or 5 or 7 => Gender.Male,
            2 or 4 or 6 or 8 => Gender.Female,
            _ => Gender.Other
        };
    }
}
