using PatientSyncHealth.Domain.Enums;
using PatientSyncHealth.Domain.Exceptions;
using PatientSyncHealth.Domain.ValueObjects;

namespace PatientSyncHealth.Tests.Domain.ValueObjects;

public class RomanianCnpTests
{
    // Valid test CNP: 5020813226919 - male - 13.08.2002
    private const string ValidCnp = "5020813226919";

    #region IsValidFormat Tests

    [Theory]
    [InlineData(ValidCnp, true)]
    [InlineData("1234567890123", true)] // 13 digits (format valid, may fail checksum)
    [InlineData("0000000000000", true)] // All zeros (format valid)
    [InlineData("123456789012", false)] // 12 digits - too short
    [InlineData("12345678901234", false)] // 14 digits - too long
    [InlineData("123456789012a", false)] // Contains letter
    [InlineData("12345678901 3", false)] // Contains space
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("   ", false)]
    [InlineData("  5020813226919  ", true)] // With whitespace (should trim)
    public void IsValidFormat_ShouldValidateCorrectly(string? value, bool expected)
    {
        var result = RomanianCnp.IsValidFormat(value);
        result.Should().Be(expected);
    }

    #endregion

    #region IsValidChecksum Tests

    [Theory]
    [InlineData(ValidCnp, true)] // Known valid CNP: 5020813226919
    [InlineData("5020813226910", false)] // Invalid checksum (last digit changed)
    [InlineData("5020813226911", false)] // Invalid checksum
    [InlineData("5020813226912", false)] // Invalid checksum
    [InlineData("123456789012", false)] // Invalid format
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidChecksum_ShouldValidateCorrectly(string? value, bool expected)
    {
        var result = RomanianCnp.IsValidChecksum(value);
        result.Should().Be(expected);
    }

    #endregion

    #region IsValidStructure Tests

    [Theory]
    [InlineData(ValidCnp, true)] // Valid structure
    [InlineData("5001301010016", false)] // Invalid month (13)
    [InlineData("5000001010013", false)] // Invalid month (00)
    [InlineData("5000132010018", false)] // Invalid day (32)
    [InlineData("5000100010011", false)] // Invalid day (00)
    [InlineData("1000229010014", false)] // Feb 29 in 1900 (not a leap year - divisible by 100 but not 400)
    [InlineData("5040229010015", true)] // Feb 29 in 2004 (leap year)
    [InlineData("5000229010011", true)] // Feb 29 in 2000 (leap year - divisible by 400)
    [InlineData("0020813010016", false)] // Invalid gender digit (0)
    [InlineData("9020813010015", false)] // Invalid gender digit (9)
    [InlineData("5020813000019", false)] // Invalid county code (00)
    [InlineData("5020813530016", false)] // Invalid county code (53)
    [InlineData("5020813700013", true)] // Valid county code (70 - new system)
    [InlineData("123456789012", false)] // Invalid format
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidStructure_ShouldValidateCorrectly(string? value, bool expected)
    {
        var result = RomanianCnp.IsValidStructure(value);
        result.Should().Be(expected);
    }

    #endregion

    #region TryExtractGender Tests

    [Theory]
    [InlineData("1020813226912", Gender.Male)] // 1 = Male 1900s
    [InlineData("2020813226911", Gender.Female)] // 2 = Female 1900s
    [InlineData("3020813226910", Gender.Male)] // 3 = Male 1800s
    [InlineData("4020813226919", Gender.Female)] // 4 = Female 1800s
    [InlineData(ValidCnp, Gender.Male)] // 5 = Male 2000s
    [InlineData("6020813226918", Gender.Female)] // 6 = Female 2000s
    [InlineData("7020813226917", Gender.Male)] // 7 = Male resident foreigner
    [InlineData("8020813226916", Gender.Female)] // 8 = Female resident foreigner
    public void TryExtractGender_WithValidCnp_ShouldReturnCorrectGender(string cnp, Gender expected)
    {
        var result = RomanianCnp.TryExtractGender(cnp);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("123456789012")] // Invalid format
    public void TryExtractGender_WithInvalidCnp_ShouldReturnNull(string? cnp)
    {
        var result = RomanianCnp.TryExtractGender(cnp);
        result.Should().BeNull();
    }

    #endregion

    #region TryExtractDateOfBirth Tests

    [Fact]
    public void TryExtractDateOfBirth_WithValidCnp_ShouldReturnCorrectDate()
    {
        // CNP: 5020813226919 = 13.08.2002
        var result = RomanianCnp.TryExtractDateOfBirth(ValidCnp);

        result.Should().NotBeNull();
        result!.Value.Year.Should().Be(2002);
        result.Value.Month.Should().Be(8);
        result.Value.Day.Should().Be(13);
    }

    [Theory]
    [InlineData("1850101010019", 1985, 1, 1)] // 1900s
    [InlineData("3850101010018", 1885, 1, 1)] // 1800s
    [InlineData("5000101010011", 2000, 1, 1)] // 2000s
    [InlineData("7990101010018", 2099, 1, 1)] // Resident foreigner 2000s
    public void TryExtractDateOfBirth_WithDifferentCenturies_ShouldReturnCorrectDate(
        string cnp, int expectedYear, int expectedMonth, int expectedDay)
    {
        var result = RomanianCnp.TryExtractDateOfBirth(cnp);

        result.Should().NotBeNull();
        result!.Value.Year.Should().Be(expectedYear);
        result.Value.Month.Should().Be(expectedMonth);
        result.Value.Day.Should().Be(expectedDay);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("123456789012")] // Invalid format
    [InlineData("5001301010016")] // Invalid structure (month 13)
    public void TryExtractDateOfBirth_WithInvalidCnp_ShouldReturnNull(string? cnp)
    {
        var result = RomanianCnp.TryExtractDateOfBirth(cnp);
        result.Should().BeNull();
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidCnp_ShouldCreateInstance()
    {
        var cnp = new RomanianCnp(ValidCnp);

        cnp.Value.Should().Be(ValidCnp);
        cnp.Type.Should().Be(IdentificationNumberType.RO);
        cnp.EncodesGender.Should().BeTrue();
        cnp.EncodesDateOfBirth.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithValidCnpWithWhitespace_ShouldTrimAndCreate()
    {
        var cnp = new RomanianCnp($"  {ValidCnp}  ");
        cnp.Value.Should().Be(ValidCnp);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrNull_ShouldThrowDomainException(string? value)
    {
        var act = () => new RomanianCnp(value!);
        act.Should().Throw<DomainException>().WithMessage("*required*");
    }

    [Theory]
    [InlineData("123456789012")] // Too short
    [InlineData("12345678901234")] // Too long
    [InlineData("123456789012a")] // Contains letter
    public void Constructor_WithInvalidFormat_ShouldThrowDomainException(string value)
    {
        var act = () => new RomanianCnp(value);
        act.Should().Throw<DomainException>().WithMessage("*13 digits*");
    }

    [Fact]
    public void Constructor_WithInvalidChecksum_ShouldThrowDomainException()
    {
        var act = () => new RomanianCnp("5020813226910"); // Changed last digit
        act.Should().Throw<DomainException>().WithMessage("*checksum*");
    }

    [Fact]
    public void Constructor_WithInvalidStructure_ShouldThrowDomainException()
    {
        // Valid format and checksum but invalid date (month 13)
        var act = () => new RomanianCnp("5001300010011");
        act.Should().Throw<DomainException>().WithMessage("*invalid*");
    }

    #endregion

    #region Instance Method Tests

    [Fact]
    public void ExtractGender_ShouldReturnCorrectGender()
    {
        var cnp = new RomanianCnp(ValidCnp);
        cnp.ExtractGender().Should().Be(Gender.Male);
    }

    [Fact]
    public void ExtractDateOfBirth_ShouldReturnCorrectDate()
    {
        var cnp = new RomanianCnp(ValidCnp);
        var dob = cnp.ExtractDateOfBirth();

        dob.Should().NotBeNull();
        dob!.Value.Should().Be(new DateTime(2002, 8, 13));
    }

    [Fact]
    public void ExtractCountyCode_ShouldReturnCorrectCode()
    {
        var cnp = new RomanianCnp(ValidCnp);
        cnp.ExtractCountyCode().Should().Be("22");
    }

    #endregion

    #region County Code Tests

    [Theory]
    [InlineData("5020813010016", true)] // County 01
    [InlineData("5020813520014", true)] // County 52
    [InlineData("5020813700013", true)] // County 70 (new system)
    [InlineData("5020813530016", false)] // County 53 (invalid)
    [InlineData("5020813690012", false)] // County 69 (invalid)
    [InlineData("5020813710010", false)] // County 71 (invalid)
    public void IsValidStructure_ShouldValidateCountyCode(string cnp, bool expected)
    {
        RomanianCnp.IsValidStructure(cnp).Should().Be(expected);
    }

    #endregion
}
