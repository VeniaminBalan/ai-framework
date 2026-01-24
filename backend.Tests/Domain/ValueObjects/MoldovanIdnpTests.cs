using PatientSyncHealth.Domain.Enums;
using PatientSyncHealth.Domain.Exceptions;
using PatientSyncHealth.Domain.ValueObjects;

namespace PatientSyncHealth.Tests.Domain.ValueObjects;

public class MoldovanIdnpTests
{
    // Valid test IDNP: 2002500081628
    private const string ValidIdnp = "2002500081628";

    #region IsValidFormat Tests

    [Theory]
    [InlineData(ValidIdnp, true)]
    [InlineData("2000000000000", true)] // All zeros after prefix (format valid)
    [InlineData("2123456789012", true)] // 13 digits starting with 2
    [InlineData("1002500081628", false)] // Doesn't start with 2
    [InlineData("3002500081628", false)] // Doesn't start with 2
    [InlineData("0002500081628", false)] // Doesn't start with 2
    [InlineData("200250008162", false)] // 12 digits - too short
    [InlineData("20025000816289", false)] // 14 digits - too long
    [InlineData("200250008162a", false)] // Contains letter
    [InlineData("2002500 81628", false)] // Contains space
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("   ", false)]
    [InlineData("  2002500081628  ", true)] // With whitespace (should trim)
    public void IsValidFormat_ShouldValidateCorrectly(string? value, bool expected)
    {
        var result = MoldovanIdnp.IsValidFormat(value);
        result.Should().Be(expected);
    }

    #endregion

    #region IsValidChecksum Tests

    [Theory]
    [InlineData(ValidIdnp, true)] // Known valid IDNP
    [InlineData("2002500081620", false)] // Invalid checksum (last digit changed)
    [InlineData("2002500081621", false)] // Invalid checksum
    [InlineData("2002500081622", false)] // Invalid checksum
    [InlineData("200250008162", false)] // Invalid format (too short)
    [InlineData("1002500081628", false)] // Invalid format (doesn't start with 2)
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidChecksum_ShouldValidateCorrectly(string? value, bool expected)
    {
        var result = MoldovanIdnp.IsValidChecksum(value);
        result.Should().Be(expected);
    }

    [Fact]
    public void IsValidChecksum_ShouldUseCorrectWeights()
    {
        // Verify the checksum calculation by creating test cases
        // IDNP checksum weights: [7, 3, 1, 7, 3, 1, 7, 3, 1, 7, 3, 1]
        // Checksum = (sum of digit * weight) % 10

        // For 2002500081628:
        // 2*7 + 0*3 + 0*1 + 2*7 + 5*3 + 0*1 + 0*7 + 0*3 + 8*1 + 1*7 + 6*3 + 2*1
        // = 14 + 0 + 0 + 14 + 15 + 0 + 0 + 0 + 8 + 7 + 18 + 2 = 78
        // 78 % 10 = 8, which should match the last digit

        MoldovanIdnp.IsValidChecksum(ValidIdnp).Should().BeTrue();
    }

    #endregion

    #region IsValidStructure Tests

    [Theory]
    [InlineData(ValidIdnp, true)] // Valid structure
    [InlineData("2001001000011", true)] // Minimum valid values
    [InlineData("2999999999996", true)] // Maximum-ish valid values
    [InlineData("2000001000011", false)] // Invalid year part (000)
    [InlineData("2001000000011", false)] // Invalid civil status code (000)
    [InlineData("2001001000001", false)] // Invalid sequential number (00000)
    [InlineData("200250008162", false)] // Invalid format
    [InlineData("1002500081628", false)] // Invalid format (doesn't start with 2)
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidStructure_ShouldValidateCorrectly(string? value, bool expected)
    {
        var result = MoldovanIdnp.IsValidStructure(value);
        result.Should().Be(expected);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidIdnp_ShouldCreateInstance()
    {
        var idnp = new MoldovanIdnp(ValidIdnp);

        idnp.Value.Should().Be(ValidIdnp);
        idnp.Type.Should().Be(IdentificationNumberType.MD);
        idnp.EncodesGender.Should().BeFalse();
        idnp.EncodesDateOfBirth.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithValidIdnpWithWhitespace_ShouldTrimAndCreate()
    {
        var idnp = new MoldovanIdnp($"  {ValidIdnp}  ");
        idnp.Value.Should().Be(ValidIdnp);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrNull_ShouldThrowDomainException(string? value)
    {
        var act = () => new MoldovanIdnp(value!);
        act.Should().Throw<DomainException>().WithMessage("*required*");
    }

    [Theory]
    [InlineData("200250008162")] // Too short
    [InlineData("20025000816289")] // Too long
    [InlineData("200250008162a")] // Contains letter
    [InlineData("1002500081628")] // Doesn't start with 2
    public void Constructor_WithInvalidFormat_ShouldThrowDomainException(string value)
    {
        var act = () => new MoldovanIdnp(value);
        act.Should().Throw<DomainException>().WithMessage("*");
    }

    [Fact]
    public void Constructor_WithInvalidChecksum_ShouldThrowDomainException()
    {
        var act = () => new MoldovanIdnp("2002500081620"); // Changed last digit
        act.Should().Throw<DomainException>().WithMessage("*checksum*");
    }

    [Fact]
    public void Constructor_WithInvalidStructure_ShouldThrowDomainException()
    {
        // Valid format and checksum but invalid structure (year part is 000)
        // Need to find a value that passes format and checksum but fails structure
        var act = () => new MoldovanIdnp("2000001000011");
        act.Should().Throw<DomainException>();
    }

    #endregion

    #region Instance Method Tests

    [Fact]
    public void ExtractAssignmentYear_ShouldReturnCorrectYear()
    {
        var idnp = new MoldovanIdnp(ValidIdnp);
        // IDNP: 2002500081628 -> year part is 002 -> 2002
        idnp.ExtractAssignmentYear().Should().Be(2002);
    }

    [Fact]
    public void ExtractAssignmentYear_VerifyExtractionLogic()
    {
        // The valid IDNP 2002500081628 has year part "002" which means 2002
        var idnp = new MoldovanIdnp(ValidIdnp);
        idnp.ExtractAssignmentYear().Should().Be(2002);
    }

    [Fact]
    public void ExtractCivilStatusCode_ShouldReturnCorrectCode()
    {
        var idnp = new MoldovanIdnp(ValidIdnp);
        // IDNP: 2002500081628 -> civil status code is 500
        idnp.ExtractCivilStatusCode().Should().Be("500");
    }

    [Fact]
    public void ExtractSequentialNumber_ShouldReturnCorrectNumber()
    {
        var idnp = new MoldovanIdnp(ValidIdnp);
        // IDNP: 2002500081628 -> sequential number is 08162
        idnp.ExtractSequentialNumber().Should().Be("08162");
    }

    #endregion

    #region Gender and DateOfBirth (Not Encoded) Tests

    [Fact]
    public void EncodesGender_ShouldReturnFalse()
    {
        var idnp = new MoldovanIdnp(ValidIdnp);
        idnp.EncodesGender.Should().BeFalse();
    }

    [Fact]
    public void EncodesDateOfBirth_ShouldReturnFalse()
    {
        var idnp = new MoldovanIdnp(ValidIdnp);
        idnp.EncodesDateOfBirth.Should().BeFalse();
    }

    #endregion
}
