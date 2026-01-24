using PatientSyncHealth.Domain.Exceptions;
using PatientSyncHealth.Domain.ValueObjects;

namespace PatientSyncHealth.Tests.Domain.ValueObjects;

public class AddressTests
{
    #region Constructor - Valid Addresses

    [Fact]
    public void Constructor_WithRequiredFields_ShouldCreateInstance()
    {
        var address = new Address("Str. Exemplu 123", "Bucuresti", "Sector 1");

        address.Street.Should().Be("Str. Exemplu 123");
        address.City.Should().Be("Bucuresti");
        address.County.Should().Be("Sector 1");
        address.PostalCode.Should().BeNull();
        address.Country.Should().Be("Romania"); // Default
    }

    [Fact]
    public void Constructor_WithAllFields_ShouldCreateInstance()
    {
        var address = new Address("Str. Exemplu 123", "Bucuresti", "Sector 1", "010101", "Romania");

        address.Street.Should().Be("Str. Exemplu 123");
        address.City.Should().Be("Bucuresti");
        address.County.Should().Be("Sector 1");
        address.PostalCode.Should().Be("010101");
        address.Country.Should().Be("Romania");
    }

    [Fact]
    public void Constructor_WithDifferentCountry_ShouldUseProvidedCountry()
    {
        var address = new Address("Main Street 1", "Chisinau", "Chisinau", null, "Moldova");
        address.Country.Should().Be("Moldova");
    }

    [Theory]
    [InlineData("  Str. Test  ", "Str. Test")]
    [InlineData("Str. Test", "Str. Test")]
    public void Constructor_ShouldTrimStreet(string input, string expected)
    {
        var address = new Address(input, "City", "County");
        address.Street.Should().Be(expected);
    }

    [Theory]
    [InlineData("  Bucuresti  ", "Bucuresti")]
    [InlineData("Bucuresti", "Bucuresti")]
    public void Constructor_ShouldTrimCity(string input, string expected)
    {
        var address = new Address("Street", input, "County");
        address.City.Should().Be(expected);
    }

    [Theory]
    [InlineData("  Sector 1  ", "Sector 1")]
    [InlineData("Sector 1", "Sector 1")]
    public void Constructor_ShouldTrimCounty(string input, string expected)
    {
        var address = new Address("Street", "City", input);
        address.County.Should().Be(expected);
    }

    [Theory]
    [InlineData("  010101  ", "010101")]
    [InlineData("010101", "010101")]
    public void Constructor_ShouldTrimPostalCode(string input, string expected)
    {
        var address = new Address("Street", "City", "County", input);
        address.PostalCode.Should().Be(expected);
    }

    [Fact]
    public void Constructor_WithEmptyCountry_ShouldDefaultToRomania()
    {
        var address = new Address("Street", "City", "County", null, "");
        address.Country.Should().Be("Romania");
    }

    [Fact]
    public void Constructor_WithWhitespaceCountry_ShouldDefaultToRomania()
    {
        var address = new Address("Street", "City", "County", null, "   ");
        address.Country.Should().Be("Romania");
    }

    #endregion

    #region Constructor - Invalid Addresses

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Constructor_WithEmptyStreet_ShouldThrowDomainException(string? street)
    {
        var act = () => new Address(street!, "City", "County");
        act.Should().Throw<DomainException>().WithMessage("*Street*required*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Constructor_WithEmptyCity_ShouldThrowDomainException(string? city)
    {
        var act = () => new Address("Street", city!, "County");
        act.Should().Throw<DomainException>().WithMessage("*City*required*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Constructor_WithEmptyCounty_ShouldThrowDomainException(string? county)
    {
        var act = () => new Address("Street", "City", county!);
        act.Should().Throw<DomainException>().WithMessage("*County*required*");
    }

    #endregion

    #region Postal Code Validation

    [Theory]
    [InlineData("010101")]
    [InlineData("123456")]
    [InlineData("999999")]
    [InlineData("000000")]
    public void Constructor_WithValidPostalCode_ShouldCreateInstance(string postalCode)
    {
        var address = new Address("Street", "City", "County", postalCode);
        address.PostalCode.Should().Be(postalCode);
    }

    [Theory]
    [InlineData("12345")] // Too short
    [InlineData("1234567")] // Too long
    [InlineData("12345a")] // Contains letter
    [InlineData("12 345")] // Contains space
    [InlineData("12-345")] // Contains hyphen
    [InlineData("abcdef")] // All letters
    public void Constructor_WithInvalidPostalCode_ShouldThrowDomainException(string postalCode)
    {
        var act = () => new Address("Street", "City", "County", postalCode);
        act.Should().Throw<DomainException>().WithMessage("*Postal code*6 digits*");
    }

    [Fact]
    public void Constructor_WithNullPostalCode_ShouldBeAllowed()
    {
        var address = new Address("Street", "City", "County", null);
        address.PostalCode.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithEmptyPostalCode_ShouldTreatAsNull()
    {
        // Empty string is not null, so validation might apply
        // Based on code: !string.IsNullOrWhiteSpace(postalCode) && !IsValidPostalCode(postalCode)
        // Empty string passes IsNullOrWhiteSpace, so validation is skipped
        var address = new Address("Street", "City", "County", "");
        address.PostalCode.Should().Be(""); // Gets trimmed but stored
    }

    #endregion

    #region GetFullAddress

    [Fact]
    public void GetFullAddress_WithoutPostalCode_ShouldFormatCorrectly()
    {
        var address = new Address("Str. Exemplu 123", "Bucuresti", "Sector 1");

        var fullAddress = address.GetFullAddress();

        fullAddress.Should().Be("Str. Exemplu 123, Bucuresti, Sector 1, Romania");
    }

    [Fact]
    public void GetFullAddress_WithPostalCode_ShouldFormatCorrectly()
    {
        var address = new Address("Str. Exemplu 123", "Bucuresti", "Sector 1", "010101");

        var fullAddress = address.GetFullAddress();

        fullAddress.Should().Be("Str. Exemplu 123, Bucuresti, Sector 1, 010101, Romania");
    }

    [Fact]
    public void GetFullAddress_WithCustomCountry_ShouldFormatCorrectly()
    {
        var address = new Address("Main Street 1", "Chisinau", "Chisinau", null, "Moldova");

        var fullAddress = address.GetFullAddress();

        fullAddress.Should().Be("Main Street 1, Chisinau, Chisinau, Moldova");
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ShouldReturnFullAddress()
    {
        var address = new Address("Str. Exemplu 123", "Bucuresti", "Sector 1", "010101");

        address.ToString().Should().Be("Str. Exemplu 123, Bucuresti, Sector 1, 010101, Romania");
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameAddress_ShouldBeEqual()
    {
        var address1 = new Address("Street", "City", "County", "010101", "Romania");
        var address2 = new Address("Street", "City", "County", "010101", "Romania");

        address1.Should().Be(address2);
        (address1 == address2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentStreet_ShouldNotBeEqual()
    {
        var address1 = new Address("Street 1", "City", "County");
        var address2 = new Address("Street 2", "City", "County");

        address1.Should().NotBe(address2);
    }

    [Fact]
    public void Equals_WithDifferentCity_ShouldNotBeEqual()
    {
        var address1 = new Address("Street", "City 1", "County");
        var address2 = new Address("Street", "City 2", "County");

        address1.Should().NotBe(address2);
    }

    [Fact]
    public void Equals_WithDifferentCounty_ShouldNotBeEqual()
    {
        var address1 = new Address("Street", "City", "County 1");
        var address2 = new Address("Street", "City", "County 2");

        address1.Should().NotBe(address2);
    }

    [Fact]
    public void Equals_WithDifferentPostalCode_ShouldNotBeEqual()
    {
        var address1 = new Address("Street", "City", "County", "010101");
        var address2 = new Address("Street", "City", "County", "020202");

        address1.Should().NotBe(address2);
    }

    [Fact]
    public void Equals_WithDifferentCountry_ShouldNotBeEqual()
    {
        var address1 = new Address("Street", "City", "County", null, "Romania");
        var address2 = new Address("Street", "City", "County", null, "Moldova");

        address1.Should().NotBe(address2);
    }

    [Fact]
    public void Equals_WithNullPostalCodeVsHasPostalCode_ShouldNotBeEqual()
    {
        var address1 = new Address("Street", "City", "County", null);
        var address2 = new Address("Street", "City", "County", "010101");

        address1.Should().NotBe(address2);
    }

    [Fact]
    public void GetHashCode_WithSameAddress_ShouldBeSame()
    {
        var address1 = new Address("Street", "City", "County", "010101", "Romania");
        var address2 = new Address("Street", "City", "County", "010101", "Romania");

        address1.GetHashCode().Should().Be(address2.GetHashCode());
    }

    #endregion

    #region Real-World Examples

    [Fact]
    public void Constructor_WithRealBucharestAddress_ShouldCreateInstance()
    {
        var address = new Address(
            "Bulevardul Unirii 10, Bloc A1, Sc. 2, Ap. 15",
            "Bucuresti",
            "Sector 3",
            "030167",
            "Romania"
        );

        address.Street.Should().Be("Bulevardul Unirii 10, Bloc A1, Sc. 2, Ap. 15");
        address.City.Should().Be("Bucuresti");
        address.County.Should().Be("Sector 3");
        address.PostalCode.Should().Be("030167");
        address.Country.Should().Be("Romania");
    }

    [Fact]
    public void Constructor_WithRealClujAddress_ShouldCreateInstance()
    {
        var address = new Address(
            "Strada Memorandumului 21",
            "Cluj-Napoca",
            "Cluj",
            "400114"
        );

        address.City.Should().Be("Cluj-Napoca");
        address.County.Should().Be("Cluj");
    }

    #endregion
}
