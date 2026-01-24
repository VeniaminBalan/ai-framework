using PatientSyncHealth.Domain.Enums;
using PatientSyncHealth.Validators.CustomValidators;

namespace PatientSyncHealth.Tests.Validators.CustomValidators;

public class IdentificationNumberValidatorTests
{
    // Valid test data (any 13 digits)
    private const string ValidRomanianCnp = "5020813226919";
    private const string ValidMoldovanIdnp = "2002500081628";

    #region IsValidFormat Tests

    [Theory]
    [InlineData(ValidRomanianCnp, IdentificationNumberType.RO, true)]
    [InlineData(ValidMoldovanIdnp, IdentificationNumberType.MD, true)]
    [InlineData("1234567890123", IdentificationNumberType.RO, true)] // Any 13 digits is valid
    [InlineData("9999999999999", IdentificationNumberType.MD, true)] // Any 13 digits is valid
    [InlineData("123456789012", IdentificationNumberType.RO, false)] // Too short
    [InlineData("12345678901234", IdentificationNumberType.MD, false)] // Too long
    [InlineData("123456789012A", IdentificationNumberType.RO, false)] // Contains letter
    [InlineData("12345 6789012", IdentificationNumberType.MD, false)] // Contains space
    [InlineData("", IdentificationNumberType.RO, false)]
    [InlineData(null, IdentificationNumberType.RO, false)]
    [InlineData("   ", IdentificationNumberType.MD, false)]
    public void IsValidFormat_ShouldValidateCorrectly(
        string? value, IdentificationNumberType type, bool expected)
    {
        var result = IdentificationNumberValidator.IsValidFormat(value, type);
        result.Should().Be(expected);
    }

    [Fact]
    public void IsValidFormat_WithUnknownType_ShouldStillValidateFormat()
    {
        // Format validation doesn't care about the type, only that it's 13 digits
        var result = IdentificationNumberValidator.IsValidFormat(ValidRomanianCnp, (IdentificationNumberType)99);
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidFormat_WithWhitespace_ShouldTrimAndValidate()
    {
        var result = IdentificationNumberValidator.IsValidFormat("  5020813226919  ", IdentificationNumberType.RO);
        result.Should().BeTrue();
    }

    #endregion

    #region Cross-Type Validation Tests

    [Fact]
    public void IsValidFormat_RomanianNumberValidatedAsMoldovan_ShouldBeValid()
    {
        // Any 13 digits is valid for both types
        IdentificationNumberValidator.IsValidFormat(ValidRomanianCnp, IdentificationNumberType.MD).Should().BeTrue();
    }

    [Fact]
    public void IsValidFormat_MoldovanNumberValidatedAsRomanian_ShouldBeValid()
    {
        // Any 13 digits is valid for both types
        IdentificationNumberValidator.IsValidFormat(ValidMoldovanIdnp, IdentificationNumberType.RO).Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Theory]
    [InlineData("0000000000000", true)] // All zeros
    [InlineData("1111111111111", true)] // All ones
    [InlineData("9999999999999", true)] // All nines
    public void IsValidFormat_WithEdgeCaseDigits_ShouldBeValid(string value, bool expected)
    {
        var result = IdentificationNumberValidator.IsValidFormat(value, IdentificationNumberType.RO);
        result.Should().Be(expected);
    }

    #endregion
}
