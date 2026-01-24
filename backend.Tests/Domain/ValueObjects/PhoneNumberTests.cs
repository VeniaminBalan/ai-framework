using PatientSyncHealth.Domain.Exceptions;
using PatientSyncHealth.Domain.ValueObjects;

namespace PatientSyncHealth.Tests.Domain.ValueObjects;

public class PhoneNumberTests
{
    #region Constructor - Valid Phone Numbers (E.164 Format)

    [Theory]
    [InlineData("+40712345678")] // Romania mobile
    [InlineData("+40212345678")] // Romania landline
    [InlineData("+37360123456")] // Moldova
    [InlineData("+14155551234")] // USA
    [InlineData("+447911123456")] // UK
    [InlineData("+33612345678")] // France
    [InlineData("+49151234567")] // Germany
    [InlineData("+81312345678")] // Japan
    [InlineData("+861391234567")] // China
    [InlineData("+1234567")] // Minimum length (7 digits after +)
    [InlineData("+123456789012345")] // Maximum length (15 digits after +)
    public void Constructor_WithValidE164Format_ShouldPreserve(string input)
    {
        var phone = new PhoneNumber(input);
        phone.Value.Should().Be(input);
    }

    [Theory]
    [InlineData("+40 712 345 678", "+40712345678")]
    [InlineData("+40-712-345-678", "+40712345678")]
    [InlineData("  +40712345678  ", "+40712345678")]
    [InlineData("+1 (415) 555-1234", "+14155551234")]
    [InlineData("+44 7911 123456", "+447911123456")]
    public void Constructor_WithFormattedInput_ShouldSanitize(string input, string expected)
    {
        var phone = new PhoneNumber(input);
        phone.Value.Should().Be(expected);
    }

    #endregion

    #region Constructor - Invalid Phone Numbers

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrNull_ShouldThrowDomainException(string? value)
    {
        var act = () => new PhoneNumber(value!);
        act.Should().Throw<DomainException>().WithMessage("*required*");
    }

    [Theory]
    [InlineData("0712345678")] // Missing + prefix (local format)
    [InlineData("40712345678")] // Missing + prefix
    [InlineData("+123456")] // Too short (6 digits - min is 7)
    [InlineData("+1234567890123456")] // Too long (16 digits - max is 15)
    [InlineData("+0123456789")] // Country code starts with 0
    [InlineData("abcdefghij")] // Letters
    [InlineData("+4071234ab")] // Mixed letters
    [InlineData("++40712345678")] // Double plus
    [InlineData("712345678")] // No prefix at all
    public void Constructor_WithInvalidFormat_ShouldThrowDomainException(string value)
    {
        var act = () => new PhoneNumber(value);
        act.Should().Throw<DomainException>().WithMessage("*E.164*");
    }

    #endregion

    #region Implicit Conversion

    [Fact]
    public void ImplicitConversion_ToString_ShouldReturnValue()
    {
        var phone = new PhoneNumber("+40712345678");
        string result = phone;
        result.Should().Be("+40712345678");
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        var phone = new PhoneNumber("+40712345678");
        phone.ToString().Should().Be("+40712345678");
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSamePhoneNumber_ShouldBeEqual()
    {
        var phone1 = new PhoneNumber("+40712345678");
        var phone2 = new PhoneNumber("+40712345678");

        phone1.Should().Be(phone2);
        (phone1 == phone2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithSamePhoneNumberDifferentFormatting_ShouldBeEqual()
    {
        var phone1 = new PhoneNumber("+40712345678");
        var phone2 = new PhoneNumber("+40 712 345 678");

        phone1.Should().Be(phone2);
    }

    [Fact]
    public void Equals_WithDifferentPhoneNumber_ShouldNotBeEqual()
    {
        var phone1 = new PhoneNumber("+40712345678");
        var phone2 = new PhoneNumber("+40712345679");

        phone1.Should().NotBe(phone2);
        (phone1 != phone2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentCountryCode_ShouldNotBeEqual()
    {
        var phone1 = new PhoneNumber("+40712345678");
        var phone2 = new PhoneNumber("+37312345678");

        phone1.Should().NotBe(phone2);
    }

    [Fact]
    public void GetHashCode_WithSamePhoneNumber_ShouldBeSame()
    {
        var phone1 = new PhoneNumber("+40712345678");
        var phone2 = new PhoneNumber("+40 712 345 678");

        phone1.GetHashCode().Should().Be(phone2.GetHashCode());
    }

    #endregion

    #region International Numbers

    [Theory]
    [InlineData("+40712345678")] // Romania
    [InlineData("+37360123456")] // Moldova
    [InlineData("+14155551234")] // USA
    [InlineData("+447911123456")] // UK
    public void Constructor_WithVariousCountryCodes_ShouldBeValid(string number)
    {
        var phone = new PhoneNumber(number);
        phone.Value.Should().Be(number);
    }

    #endregion
}
