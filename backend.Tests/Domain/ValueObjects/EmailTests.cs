using PatientSyncHealth.Domain.Exceptions;
using PatientSyncHealth.Domain.ValueObjects;

namespace PatientSyncHealth.Tests.Domain.ValueObjects;

public class EmailTests
{
    #region Constructor - Valid Emails

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@example.com")]
    [InlineData("user+tag@example.com")]
    [InlineData("user@subdomain.example.com")]
    [InlineData("user@example.co.uk")]
    [InlineData("a@b.com")]
    [InlineData("123@example.com")]
    [InlineData("user_name@example.com")]
    [InlineData("user-name@example.com")]
    public void Constructor_WithValidEmail_ShouldCreateInstance(string value)
    {
        var email = new Email(value);
        email.Value.Should().Be(value.ToLowerInvariant());
    }

    [Theory]
    [InlineData("  test@example.com  ", "test@example.com")]
    [InlineData("TEST@EXAMPLE.COM", "test@example.com")]
    [InlineData("Test@Example.Com", "test@example.com")]
    public void Constructor_ShouldTrimAndLowercase(string input, string expected)
    {
        var email = new Email(input);
        email.Value.Should().Be(expected);
    }

    #endregion

    #region Constructor - Invalid Emails

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrNull_ShouldThrowDomainException(string? value)
    {
        var act = () => new Email(value!);
        act.Should().Throw<DomainException>().WithMessage("*required*");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user@.com")]
    [InlineData("user space@example.com")]
    [InlineData("user@exam ple.com")]
    [InlineData("user@@example.com")]
    [InlineData(".user@example.com")]
    [InlineData("user..name@example.com")]
    public void Constructor_WithInvalidFormat_ShouldThrowDomainException(string value)
    {
        var act = () => new Email(value);
        act.Should().Throw<DomainException>().WithMessage("*Invalid email*");
    }

    // Note: MailAddress class accepts some formats that might seem invalid:
    // - "user@example" (no TLD) is valid per MailAddress
    // - "user.@example.com" (trailing dot in local part) is valid per MailAddress

    #endregion

    #region Implicit Conversion

    [Fact]
    public void ImplicitConversion_ToString_ShouldReturnValue()
    {
        var email = new Email("test@example.com");
        string result = email;
        result.Should().Be("test@example.com");
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        var email = new Email("test@example.com");
        email.ToString().Should().Be("test@example.com");
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameEmail_ShouldBeEqual()
    {
        var email1 = new Email("test@example.com");
        var email2 = new Email("test@example.com");

        email1.Should().Be(email2);
        (email1 == email2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithSameEmailDifferentCase_ShouldBeEqual()
    {
        var email1 = new Email("test@example.com");
        var email2 = new Email("TEST@EXAMPLE.COM");

        email1.Should().Be(email2);
    }

    [Fact]
    public void Equals_WithDifferentEmail_ShouldNotBeEqual()
    {
        var email1 = new Email("test1@example.com");
        var email2 = new Email("test2@example.com");

        email1.Should().NotBe(email2);
        (email1 != email2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_WithSameEmail_ShouldBeSame()
    {
        var email1 = new Email("test@example.com");
        var email2 = new Email("TEST@EXAMPLE.COM");

        email1.GetHashCode().Should().Be(email2.GetHashCode());
    }

    #endregion

    #region Edge Cases

    [Theory]
    [InlineData("very.long.email.address.that.is.still.valid@subdomain.example.com")]
    [InlineData("user@123.123.123.123")]
    public void Constructor_WithEdgeCaseEmails_ShouldCreateInstance(string value)
    {
        var email = new Email(value);
        email.Value.Should().Be(value.ToLowerInvariant());
    }

    #endregion
}
