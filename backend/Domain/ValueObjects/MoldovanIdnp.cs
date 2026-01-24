using PatientSyncHealth.Domain.Enums;
using PatientSyncHealth.Domain.Exceptions;

namespace PatientSyncHealth.Domain.ValueObjects;

/// <summary>
/// Moldovan IDNP (Identificator Numeric Personal) - 13-digit personal identification number.
/// Format: 2TTTXXXYYYYYK
/// 2 = Fixed prefix for individuals
/// TTT = Last 3 digits of year when IDNP was assigned
/// XXX = Civil status office code
/// YYYYY = Sequential registration number
/// K = Control digit
///
/// Note: Unlike Romanian CNP, Moldovan IDNP does NOT encode gender or date of birth.
/// </summary>
public sealed class MoldovanIdnp : IdentificationNumber
{
    private static readonly int[] ChecksumWeights = [7, 3, 1, 7, 3, 1, 7, 3, 1, 7, 3, 1];

    public override IdentificationNumberType Type => IdentificationNumberType.MD;

    public override bool EncodesGender => false;

    public override bool EncodesDateOfBirth => false;

    private MoldovanIdnp() { } // EF Core

    public MoldovanIdnp(string value)
    {
        Validate(value);
        Value = value.Trim();
    }

    protected override void Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("IDNP is required");

        value = value.Trim();

        if (!IsValidFormat(value))
            throw new DomainException("IDNP must be exactly 13 digits starting with '2'");

        if (!IsValidChecksum(value))
            throw new DomainException("IDNP has invalid checksum");

        if (!IsValidStructure(value))
            throw new DomainException("IDNP has invalid structure");
    }

    /// <summary>
    /// Checks if the value has valid IDNP format (13 digits starting with '2').
    /// </summary>
    public static bool IsValidFormat(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        value = value.Trim();
        return value.Length == 13 && value.All(char.IsDigit) && value[0] == '2';
    }

    /// <summary>
    /// Extracts the year when the IDNP was assigned (last 3 digits represent the year).
    /// </summary>
    public int ExtractAssignmentYear()
    {
        var yearPart = int.Parse(Value.Substring(1, 3));
        // Determine century: if < 100, it's 2000+, otherwise 1000+ (though IDNP started after 2000)
        return 2000 + yearPart;
    }

    /// <summary>
    /// Extracts the civil status office code.
    /// </summary>
    public string ExtractCivilStatusCode()
    {
        return Value.Substring(4, 3);
    }

    /// <summary>
    /// Extracts the sequential registration number.
    /// </summary>
    public string ExtractSequentialNumber()
    {
        return Value.Substring(7, 5);
    }

    /// <summary>
    /// Validates the IDNP checksum digit.
    /// </summary>
    public static bool IsValidChecksum(string? value)
    {
        if (!IsValidFormat(value))
            return false;

        var idnp = value!.Trim();

        // Control digit calculation using weighted sum modulo 10
        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            sum += int.Parse(idnp[i].ToString()) * ChecksumWeights[i];
        }

        var expectedChecksum = sum % 10;
        var actualChecksum = int.Parse(idnp[12].ToString());

        return expectedChecksum == actualChecksum;
    }

    /// <summary>
    /// Validates the IDNP structure (year, civil status code, sequential number).
    /// </summary>
    public static bool IsValidStructure(string? value)
    {
        if (!IsValidFormat(value))
            return false;

        var idnp = value!.Trim();

        try
        {
            // Validate year part (001-999, representing years since 2000)
            var yearPart = int.Parse(idnp.Substring(1, 3));
            if (yearPart < 1)
                return false;

            // Civil status code should be valid (001-999)
            var civilStatusCode = int.Parse(idnp.Substring(4, 3));
            if (civilStatusCode < 1)
                return false;

            // Sequential number should be valid (00001-99999)
            var sequentialNumber = int.Parse(idnp.Substring(7, 5));
            if (sequentialNumber < 1)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Reconstructs a MoldovanIdnp without validation (for EF Core loading).
    /// </summary>
    internal static MoldovanIdnp Reconstruct(string value)
    {
        return new MoldovanIdnp { Value = value };
    }
}
