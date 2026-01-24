using PatientSyncHealth.Domain.Enums;
using PatientSyncHealth.Domain.Exceptions;
using PatientSyncHealth.Domain.ValueObjects;

namespace PatientSyncHealth.Tests.Domain.ValueObjects;

public class IdentificationNumberTests
{
    private const string ValidRomanianCnp = "5020813226919";
    private const string ValidMoldovanIdnp = "2002500081628";

    #region Constructor

    [Fact]
    public void Constructor_WithValidRomanianNumber_ShouldCreateIdentificationNumber()
    {
        var idNumber = new IdentificationNumber(ValidRomanianCnp, IdentificationNumberType.RO);

        idNumber.Value.Should().Be(ValidRomanianCnp);
        idNumber.Type.Should().Be(IdentificationNumberType.RO);
    }

    [Fact]
    public void Constructor_WithValidMoldovanNumber_ShouldCreateIdentificationNumber()
    {
        var idNumber = new IdentificationNumber(ValidMoldovanIdnp, IdentificationNumberType.MD);

        idNumber.Value.Should().Be(ValidMoldovanIdnp);
        idNumber.Type.Should().Be(IdentificationNumberType.MD);
    }

    [Fact]
    public void Constructor_WithNullValue_ShouldThrowDomainException()
    {
        var act = () => new IdentificationNumber(null!, IdentificationNumberType.RO);
        act.Should().Throw<DomainException>()
            .WithMessage("Identification number is required");
    }

    [Fact]
    public void Constructor_WithEmptyValue_ShouldThrowDomainException()
    {
        var act = () => new IdentificationNumber("", IdentificationNumberType.RO);
        act.Should().Throw<DomainException>()
            .WithMessage("Identification number is required");
    }

    [Fact]
    public void Constructor_WithWhitespaceValue_ShouldThrowDomainException()
    {
        var act = () => new IdentificationNumber("   ", IdentificationNumberType.RO);
        act.Should().Throw<DomainException>()
            .WithMessage("Identification number is required");
    }

    [Theory]
    [InlineData("123456789012")] // Too short (12 digits)
    [InlineData("12345678901234")] // Too long (14 digits)
    [InlineData("123")] // Too short
    public void Constructor_WithInvalidLength_ShouldThrowDomainException(string value)
    {
        var act = () => new IdentificationNumber(value, IdentificationNumberType.RO);
        act.Should().Throw<DomainException>()
            .WithMessage("Identification number must be exactly 13 digits");
    }

    [Theory]
    [InlineData("123456789012A")] // Contains letter
    [InlineData("12345 6789012")] // Contains space
    [InlineData("1234567890-12")] // Contains dash
    [InlineData("1234567890.12")] // Contains dot
    public void Constructor_WithNonDigitCharacters_ShouldThrowDomainException(string value)
    {
        var act = () => new IdentificationNumber(value, IdentificationNumberType.RO);
        act.Should().Throw<DomainException>()
            .WithMessage("Identification number must be exactly 13 digits");
    }

    [Fact]
    public void Constructor_WithValidNumberAndWhitespace_ShouldTrimAndCreate()
    {
        var idNumber = new IdentificationNumber("  5020813226919  ", IdentificationNumberType.RO);

        idNumber.Value.Should().Be(ValidRomanianCnp);
    }

    #endregion

    #region Type Property

    [Fact]
    public void Type_ForRomanianCnp_ShouldReturnRO()
    {
        var idNumber = new IdentificationNumber(ValidRomanianCnp, IdentificationNumberType.RO);
        idNumber.Type.Should().Be(IdentificationNumberType.RO);
    }

    [Fact]
    public void Type_ForMoldovanIdnp_ShouldReturnMD()
    {
        var idNumber = new IdentificationNumber(ValidMoldovanIdnp, IdentificationNumberType.MD);
        idNumber.Type.Should().Be(IdentificationNumberType.MD);
    }

    #endregion

    #region ToString Method

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        var idNumber = new IdentificationNumber(ValidRomanianCnp, IdentificationNumberType.RO);
        idNumber.ToString().Should().Be(ValidRomanianCnp);
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameValueAndType_ShouldBeEqual()
    {
        var idNumber1 = new IdentificationNumber(ValidRomanianCnp, IdentificationNumberType.RO);
        var idNumber2 = new IdentificationNumber(ValidRomanianCnp, IdentificationNumberType.RO);

        idNumber1.Should().Be(idNumber2);
        (idNumber1 == idNumber2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValues_ShouldNotBeEqual()
    {
        var idNumber1 = new IdentificationNumber(ValidRomanianCnp, IdentificationNumberType.RO);
        var idNumber2 = new IdentificationNumber(ValidMoldovanIdnp, IdentificationNumberType.MD);

        idNumber1.Should().NotBe(idNumber2);
    }

    [Fact]
    public void Equals_WithSameValueButDifferentTypes_ShouldNotBeEqual()
    {
        var idNumber1 = new IdentificationNumber("1234567890123", IdentificationNumberType.RO);
        var idNumber2 = new IdentificationNumber("1234567890123", IdentificationNumberType.MD);

        idNumber1.Should().NotBe(idNumber2);
    }

    [Fact]
    public void GetHashCode_WithSameValueAndType_ShouldBeSame()
    {
        var idNumber1 = new IdentificationNumber(ValidRomanianCnp, IdentificationNumberType.RO);
        var idNumber2 = new IdentificationNumber(ValidRomanianCnp, IdentificationNumberType.RO);

        idNumber1.GetHashCode().Should().Be(idNumber2.GetHashCode());
    }

    #endregion

    #region Collection Tests

    [Fact]
    public void CanStoreInCollection_WithMixedTypes()
    {
        var idNumbers = new List<IdentificationNumber>
        {
            new IdentificationNumber(ValidRomanianCnp, IdentificationNumberType.RO),
            new IdentificationNumber(ValidMoldovanIdnp, IdentificationNumberType.MD)
        };

        idNumbers.Should().HaveCount(2);
        idNumbers[0].Type.Should().Be(IdentificationNumberType.RO);
        idNumbers[1].Type.Should().Be(IdentificationNumberType.MD);
    }

    #endregion
}
