using PatientSyncHealth.Domain.Enums;
using PatientSyncHealth.Validators.CustomValidators;

namespace PatientSyncHealth.Tests.Validators.CustomValidators;

public class IdentificationNumberValidatorTests
{
    // Valid test data
    private const string ValidRomanianCnp = "5020813226919"; // male - 13.08.2002
    private const string ValidMoldovanIdnp = "2002500081628";

    #region IsValidFormat Tests

    [Theory]
    [InlineData(ValidRomanianCnp, IdentificationNumberType.RO, true)]
    [InlineData(ValidMoldovanIdnp, IdentificationNumberType.MD, true)]
    [InlineData("1234567890123", IdentificationNumberType.RO, true)] // Valid format (may fail checksum)
    [InlineData("2123456789012", IdentificationNumberType.MD, true)] // Valid format
    [InlineData("123456789012", IdentificationNumberType.RO, false)] // Too short
    [InlineData("123456789012", IdentificationNumberType.MD, false)] // Too short
    [InlineData("1002500081628", IdentificationNumberType.MD, false)] // Doesn't start with 2
    [InlineData("", IdentificationNumberType.RO, false)]
    [InlineData(null, IdentificationNumberType.RO, false)]
    [InlineData("   ", IdentificationNumberType.MD, false)]
    public void IsValidFormat_ShouldDelegateToCorrectValueObject(
        string? value, IdentificationNumberType type, bool expected)
    {
        var result = IdentificationNumberValidator.IsValidFormat(value, type);
        result.Should().Be(expected);
    }

    [Fact]
    public void IsValidFormat_WithUnknownType_ShouldReturnFalse()
    {
        var result = IdentificationNumberValidator.IsValidFormat(ValidRomanianCnp, (IdentificationNumberType)99);
        result.Should().BeFalse();
    }

    #endregion

    #region IsValidChecksum Tests

    [Theory]
    [InlineData(ValidRomanianCnp, IdentificationNumberType.RO, true)]
    [InlineData(ValidMoldovanIdnp, IdentificationNumberType.MD, true)]
    [InlineData("5020813226910", IdentificationNumberType.RO, false)] // Invalid checksum
    [InlineData("2002500081620", IdentificationNumberType.MD, false)] // Invalid checksum
    [InlineData("", IdentificationNumberType.RO, false)]
    [InlineData(null, IdentificationNumberType.MD, false)]
    public void IsValidChecksum_ShouldDelegateToCorrectValueObject(
        string? value, IdentificationNumberType type, bool expected)
    {
        var result = IdentificationNumberValidator.IsValidChecksum(value, type);
        result.Should().Be(expected);
    }

    [Fact]
    public void IsValidChecksum_WithUnknownType_ShouldReturnFalse()
    {
        var result = IdentificationNumberValidator.IsValidChecksum(ValidRomanianCnp, (IdentificationNumberType)99);
        result.Should().BeFalse();
    }

    #endregion

    #region IsValidStructure Tests

    [Theory]
    [InlineData(ValidRomanianCnp, IdentificationNumberType.RO, true)]
    [InlineData(ValidMoldovanIdnp, IdentificationNumberType.MD, true)]
    [InlineData("5001301010016", IdentificationNumberType.RO, false)] // Invalid month (13)
    [InlineData("2000001000011", IdentificationNumberType.MD, false)] // Invalid year part
    [InlineData("", IdentificationNumberType.RO, false)]
    [InlineData(null, IdentificationNumberType.MD, false)]
    public void IsValidStructure_ShouldDelegateToCorrectValueObject(
        string? value, IdentificationNumberType type, bool expected)
    {
        var result = IdentificationNumberValidator.IsValidStructure(value, type);
        result.Should().Be(expected);
    }

    [Fact]
    public void IsValidStructure_WithUnknownType_ShouldReturnFalse()
    {
        var result = IdentificationNumberValidator.IsValidStructure(ValidRomanianCnp, (IdentificationNumberType)99);
        result.Should().BeFalse();
    }

    #endregion

    #region ExtractDateOfBirth Tests

    [Fact]
    public void ExtractDateOfBirth_WithValidRomanianCnp_ShouldReturnDate()
    {
        var result = IdentificationNumberValidator.ExtractDateOfBirth(ValidRomanianCnp, IdentificationNumberType.RO);

        result.Should().NotBeNull();
        result!.Value.Year.Should().Be(2002);
        result.Value.Month.Should().Be(8);
        result.Value.Day.Should().Be(13);
    }

    [Fact]
    public void ExtractDateOfBirth_WithValidMoldovanIdnp_ShouldReturnNull()
    {
        // IDNP does not encode date of birth
        var result = IdentificationNumberValidator.ExtractDateOfBirth(ValidMoldovanIdnp, IdentificationNumberType.MD);
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("", IdentificationNumberType.RO)]
    [InlineData(null, IdentificationNumberType.RO)]
    [InlineData("   ", IdentificationNumberType.RO)]
    public void ExtractDateOfBirth_WithInvalidValue_ShouldReturnNull(string? value, IdentificationNumberType type)
    {
        var result = IdentificationNumberValidator.ExtractDateOfBirth(value, type);
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractDateOfBirth_WithUnknownType_ShouldReturnNull()
    {
        var result = IdentificationNumberValidator.ExtractDateOfBirth(ValidRomanianCnp, (IdentificationNumberType)99);
        result.Should().BeNull();
    }

    #endregion

    #region ExtractGender Tests

    [Fact]
    public void ExtractGender_WithValidRomanianCnp_ShouldReturnGender()
    {
        var result = IdentificationNumberValidator.ExtractGender(ValidRomanianCnp, IdentificationNumberType.RO);
        result.Should().Be(Gender.Male);
    }

    [Theory]
    [InlineData("1020813226912", Gender.Male)] // 1 = Male 1900s
    [InlineData("2020813226911", Gender.Female)] // 2 = Female 1900s
    [InlineData("5020813226919", Gender.Male)] // 5 = Male 2000s
    [InlineData("6020813226918", Gender.Female)] // 6 = Female 2000s
    public void ExtractGender_WithDifferentGenderDigits_ShouldReturnCorrectGender(string cnp, Gender expected)
    {
        var result = IdentificationNumberValidator.ExtractGender(cnp, IdentificationNumberType.RO);
        result.Should().Be(expected);
    }

    [Fact]
    public void ExtractGender_WithValidMoldovanIdnp_ShouldReturnNull()
    {
        // IDNP does not encode gender
        var result = IdentificationNumberValidator.ExtractGender(ValidMoldovanIdnp, IdentificationNumberType.MD);
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("", IdentificationNumberType.RO)]
    [InlineData(null, IdentificationNumberType.RO)]
    [InlineData("   ", IdentificationNumberType.RO)]
    public void ExtractGender_WithInvalidValue_ShouldReturnNull(string? value, IdentificationNumberType type)
    {
        var result = IdentificationNumberValidator.ExtractGender(value, type);
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractGender_WithUnknownType_ShouldReturnNull()
    {
        var result = IdentificationNumberValidator.ExtractGender(ValidRomanianCnp, (IdentificationNumberType)99);
        result.Should().BeNull();
    }

    #endregion

    #region EncodesDateOfBirth Tests

    [Theory]
    [InlineData(IdentificationNumberType.RO, true)]
    [InlineData(IdentificationNumberType.MD, false)]
    public void EncodesDateOfBirth_ShouldReturnCorrectValue(IdentificationNumberType type, bool expected)
    {
        var result = IdentificationNumberValidator.EncodesDateOfBirth(type);
        result.Should().Be(expected);
    }

    #endregion

    #region EncodesGender Tests

    [Theory]
    [InlineData(IdentificationNumberType.RO, true)]
    [InlineData(IdentificationNumberType.MD, false)]
    public void EncodesGender_ShouldReturnCorrectValue(IdentificationNumberType type, bool expected)
    {
        var result = IdentificationNumberValidator.EncodesGender(type);
        result.Should().Be(expected);
    }

    #endregion

    #region Cross-Type Validation Tests

    [Fact]
    public void ValidatingRomanianCnpAsMoldovanIdnp_ShouldFail()
    {
        // A valid Romanian CNP should not be valid as Moldovan IDNP (unless it happens to start with 2)
        // CNP 5020813226919 starts with 5, so it should fail MD format check
        IdentificationNumberValidator.IsValidFormat(ValidRomanianCnp, IdentificationNumberType.MD).Should().BeFalse();
    }

    [Fact]
    public void ValidatingMoldovanIdnpAsRomanianCnp_MayPassFormatButFailStructure()
    {
        // IDNP starts with 2, which is valid for CNP format, but structure may differ
        var formatValid = IdentificationNumberValidator.IsValidFormat(ValidMoldovanIdnp, IdentificationNumberType.RO);
        // Format might pass (13 digits), but checksum/structure will likely fail
        if (formatValid)
        {
            var checksumValid = IdentificationNumberValidator.IsValidChecksum(ValidMoldovanIdnp, IdentificationNumberType.RO);
            var structureValid = IdentificationNumberValidator.IsValidStructure(ValidMoldovanIdnp, IdentificationNumberType.RO);

            // At least one of these should fail for a valid IDNP being validated as CNP
            (checksumValid && structureValid).Should().BeFalse();
        }
    }

    #endregion

    #region Integration Tests - Full Validation Flow

    [Fact]
    public void FullValidation_RomanianCnp_AllChecksShouldPass()
    {
        var value = ValidRomanianCnp;
        var type = IdentificationNumberType.RO;

        IdentificationNumberValidator.IsValidFormat(value, type).Should().BeTrue();
        IdentificationNumberValidator.IsValidChecksum(value, type).Should().BeTrue();
        IdentificationNumberValidator.IsValidStructure(value, type).Should().BeTrue();
        IdentificationNumberValidator.ExtractGender(value, type).Should().Be(Gender.Male);
        IdentificationNumberValidator.ExtractDateOfBirth(value, type).Should().Be(new DateTime(2002, 8, 13));
    }

    [Fact]
    public void FullValidation_MoldovanIdnp_AllChecksShouldPass()
    {
        var value = ValidMoldovanIdnp;
        var type = IdentificationNumberType.MD;

        IdentificationNumberValidator.IsValidFormat(value, type).Should().BeTrue();
        IdentificationNumberValidator.IsValidChecksum(value, type).Should().BeTrue();
        IdentificationNumberValidator.IsValidStructure(value, type).Should().BeTrue();
        IdentificationNumberValidator.ExtractGender(value, type).Should().BeNull(); // IDNP doesn't encode gender
        IdentificationNumberValidator.ExtractDateOfBirth(value, type).Should().BeNull(); // IDNP doesn't encode DOB
    }

    #endregion
}
