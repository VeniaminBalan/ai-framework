using PatientSyncHealth.Domain.Exceptions;
using PatientSyncHealth.Domain.ValueObjects;

namespace PatientSyncHealth.Tests.Domain.ValueObjects;

public class PhoneNumberTests
{
    #region Constructor - Valid Phone Numbers

    [Theory]
    [InlineData("0712345678", "+40712345678")] // Mobile starting with 07
    [InlineData("0723456789", "+40723456789")] // Mobile
    [InlineData("0734567890", "+40734567890")] // Mobile
    [InlineData("0745678901", "+40745678901")] // Mobile
    [InlineData("0756789012", "+40756789012")] // Mobile
    [InlineData("0767890123", "+40767890123")] // Mobile
    [InlineData("0778901234", "+40778901234")] // Mobile
    [InlineData("0789012345", "+40789012345")] // Mobile
    [InlineData("0212345678", "+40212345678")] // Landline Bucharest (02)
    [InlineData("0312345678", "+40312345678")] // Landline (03)
    [InlineData("0412345678", "+40412345678")] // Landline (04)
    public void Constructor_WithValidLocalFormat_ShouldNormalizeToInternational(string input, string expected)
    {
        var phone = new PhoneNumber(input);
        phone.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("+40712345678", "+40712345678")]
    [InlineData("+40723456789", "+40723456789")]
    [InlineData("+40212345678", "+40212345678")]
    public void Constructor_WithValidInternationalFormat_ShouldPreserve(string input, string expected)
    {
        var phone = new PhoneNumber(input);
        phone.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("  0712345678  ", "+40712345678")]
    [InlineData("0712 345 678", "+40712345678")]
    [InlineData("0712-345-678", "+40712345678")]
    [InlineData("(0712)345678", "+40712345678")]
    [InlineData("+40 712 345 678", "+40712345678")]
    [InlineData("+40-712-345-678", "+40712345678")]
    public void Constructor_WithFormattedInput_ShouldNormalize(string input, string expected)
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
    [InlineData("071234567")] // Too short (9 digits after 0)
    [InlineData("07123456789")] // Too long (10 digits after 0)
    [InlineData("0123456789")] // Invalid prefix (01)
    [InlineData("1234567890")] // Missing leading 0 or +40
    [InlineData("+41712345678")] // Wrong country code
    [InlineData("+40012345678")] // Invalid second digit (0)
    [InlineData("+40112345678")] // Invalid second digit (1)
    [InlineData("abcdefghij")] // Letters
    [InlineData("07123456ab")] // Mixed letters
    public void Constructor_WithInvalidFormat_ShouldThrowDomainException(string value)
    {
        var act = () => new PhoneNumber(value);
        act.Should().Throw<DomainException>().WithMessage("*Invalid Romanian phone number*");
    }

    #endregion

    #region Implicit Conversion

    [Fact]
    public void ImplicitConversion_ToString_ShouldReturnNormalizedValue()
    {
        var phone = new PhoneNumber("0712345678");
        string result = phone;
        result.Should().Be("+40712345678");
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ShouldReturnNormalizedValue()
    {
        var phone = new PhoneNumber("0712345678");
        phone.ToString().Should().Be("+40712345678");
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSamePhoneNumber_ShouldBeEqual()
    {
        var phone1 = new PhoneNumber("0712345678");
        var phone2 = new PhoneNumber("0712345678");

        phone1.Should().Be(phone2);
        (phone1 == phone2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithSamePhoneNumberDifferentFormats_ShouldBeEqual()
    {
        var phone1 = new PhoneNumber("0712345678");
        var phone2 = new PhoneNumber("+40712345678");

        phone1.Should().Be(phone2);
    }

    [Fact]
    public void Equals_WithDifferentPhoneNumber_ShouldNotBeEqual()
    {
        var phone1 = new PhoneNumber("0712345678");
        var phone2 = new PhoneNumber("0712345679");

        phone1.Should().NotBe(phone2);
        (phone1 != phone2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_WithSamePhoneNumber_ShouldBeSame()
    {
        var phone1 = new PhoneNumber("0712345678");
        var phone2 = new PhoneNumber("+40712345678");

        phone1.GetHashCode().Should().Be(phone2.GetHashCode());
    }

    #endregion

    #region Mobile vs Landline

    [Theory]
    [InlineData("0721234567")] // Orange
    [InlineData("0731234567")] // Orange
    [InlineData("0741234567")] // Vodafone
    [InlineData("0751234567")] // Vodafone
    [InlineData("0761234567")] // Telekom
    [InlineData("0771234567")] // Telekom
    [InlineData("0781234567")] // Various MVNOs
    public void Constructor_WithMobileNumber_ShouldBeValid(string number)
    {
        var phone = new PhoneNumber(number);
        phone.Value.Should().StartWith("+407");
    }

    [Theory]
    [InlineData("0212345678")] // Bucharest
    [InlineData("0232345678")] // Iasi
    [InlineData("0242345678")] // Constanta
    [InlineData("0252345678")] // Timisoara
    [InlineData("0262345678")] // Cluj
    public void Constructor_WithLandlineNumber_ShouldBeValid(string number)
    {
        var phone = new PhoneNumber(number);
        phone.Value.Should().StartWith("+402");
    }

    #endregion
}
