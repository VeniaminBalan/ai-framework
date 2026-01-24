using PatientSyncHealth.Domain.Enums;
using PatientSyncHealth.Domain.Exceptions;
using PatientSyncHealth.Domain.ValueObjects;

namespace PatientSyncHealth.Tests.Domain.ValueObjects;

public class PersonalIdentificationNumberTests
{
    private const string ValidRomanianCnp = "5020813226919";
    private const string ValidMoldovanIdnp = "2002500081628";

    #region Factory Method - Create

    [Fact]
    public void Create_WithRomanianType_ShouldReturnRomanianCnp()
    {
        var pin = PersonalIdentificationNumber.Create(ValidRomanianCnp, IdentificationNumberType.RO);

        pin.Should().BeOfType<RomanianCnp>();
        pin.Value.Should().Be(ValidRomanianCnp);
        pin.Type.Should().Be(IdentificationNumberType.RO);
    }

    [Fact]
    public void Create_WithMoldovanType_ShouldReturnMoldovanIdnp()
    {
        var pin = PersonalIdentificationNumber.Create(ValidMoldovanIdnp, IdentificationNumberType.MD);

        pin.Should().BeOfType<MoldovanIdnp>();
        pin.Value.Should().Be(ValidMoldovanIdnp);
        pin.Type.Should().Be(IdentificationNumberType.MD);
    }

    [Fact]
    public void Create_WithInvalidRomanianCnp_ShouldThrowDomainException()
    {
        var act = () => PersonalIdentificationNumber.Create("invalid", IdentificationNumberType.RO);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithInvalidMoldovanIdnp_ShouldThrowDomainException()
    {
        var act = () => PersonalIdentificationNumber.Create("invalid", IdentificationNumberType.MD);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithUnsupportedType_ShouldThrowDomainException()
    {
        var act = () => PersonalIdentificationNumber.Create("1234567890123", (IdentificationNumberType)99);
        act.Should().Throw<DomainException>().WithMessage("*Unsupported*");
    }

    #endregion

    #region Type Property

    [Fact]
    public void Type_ForRomanianCnp_ShouldReturnRO()
    {
        PersonalIdentificationNumber pin = new RomanianCnp(ValidRomanianCnp);
        pin.Type.Should().Be(IdentificationNumberType.RO);
    }

    [Fact]
    public void Type_ForMoldovanIdnp_ShouldReturnMD()
    {
        PersonalIdentificationNumber pin = new MoldovanIdnp(ValidMoldovanIdnp);
        pin.Type.Should().Be(IdentificationNumberType.MD);
    }

    #endregion

    #region EncodesGender Property

    [Fact]
    public void EncodesGender_ForRomanianCnp_ShouldReturnTrue()
    {
        PersonalIdentificationNumber pin = new RomanianCnp(ValidRomanianCnp);
        pin.EncodesGender.Should().BeTrue();
    }

    [Fact]
    public void EncodesGender_ForMoldovanIdnp_ShouldReturnFalse()
    {
        PersonalIdentificationNumber pin = new MoldovanIdnp(ValidMoldovanIdnp);
        pin.EncodesGender.Should().BeFalse();
    }

    #endregion

    #region EncodesDateOfBirth Property

    [Fact]
    public void EncodesDateOfBirth_ForRomanianCnp_ShouldReturnTrue()
    {
        PersonalIdentificationNumber pin = new RomanianCnp(ValidRomanianCnp);
        pin.EncodesDateOfBirth.Should().BeTrue();
    }

    [Fact]
    public void EncodesDateOfBirth_ForMoldovanIdnp_ShouldReturnFalse()
    {
        PersonalIdentificationNumber pin = new MoldovanIdnp(ValidMoldovanIdnp);
        pin.EncodesDateOfBirth.Should().BeFalse();
    }

    #endregion

    #region ExtractGender Method

    [Fact]
    public void ExtractGender_ForRomanianCnp_ShouldReturnGender()
    {
        PersonalIdentificationNumber pin = new RomanianCnp(ValidRomanianCnp);
        pin.ExtractGender().Should().Be(Gender.Male);
    }

    [Fact]
    public void ExtractGender_ForMoldovanIdnp_ShouldReturnNull()
    {
        PersonalIdentificationNumber pin = new MoldovanIdnp(ValidMoldovanIdnp);
        pin.ExtractGender().Should().BeNull();
    }

    #endregion

    #region ExtractDateOfBirth Method

    [Fact]
    public void ExtractDateOfBirth_ForRomanianCnp_ShouldReturnDate()
    {
        PersonalIdentificationNumber pin = new RomanianCnp(ValidRomanianCnp);
        var dob = pin.ExtractDateOfBirth();

        dob.Should().NotBeNull();
        dob!.Value.Should().Be(new DateTime(2002, 8, 13));
    }

    [Fact]
    public void ExtractDateOfBirth_ForMoldovanIdnp_ShouldReturnNull()
    {
        PersonalIdentificationNumber pin = new MoldovanIdnp(ValidMoldovanIdnp);
        pin.ExtractDateOfBirth().Should().BeNull();
    }

    #endregion

    #region Implicit Conversion

    [Fact]
    public void ImplicitConversion_ToString_ShouldReturnValue()
    {
        PersonalIdentificationNumber pin = new RomanianCnp(ValidRomanianCnp);
        string result = pin;
        result.Should().Be(ValidRomanianCnp);
    }

    #endregion

    #region ToString Method

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        PersonalIdentificationNumber pin = new RomanianCnp(ValidRomanianCnp);
        pin.ToString().Should().Be(ValidRomanianCnp);
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_WithSameValueAndType_ShouldBeEqual()
    {
        PersonalIdentificationNumber pin1 = new RomanianCnp(ValidRomanianCnp);
        PersonalIdentificationNumber pin2 = new RomanianCnp(ValidRomanianCnp);

        pin1.Should().Be(pin2);
        (pin1 == pin2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentTypes_ShouldNotBeEqual()
    {
        // Even if we could somehow create a value that passes both validations,
        // different types should not be equal
        PersonalIdentificationNumber pin1 = new RomanianCnp(ValidRomanianCnp);
        PersonalIdentificationNumber pin2 = new MoldovanIdnp(ValidMoldovanIdnp);

        pin1.Should().NotBe(pin2);
    }

    [Fact]
    public void GetHashCode_WithSameValueAndType_ShouldBeSame()
    {
        PersonalIdentificationNumber pin1 = new RomanianCnp(ValidRomanianCnp);
        PersonalIdentificationNumber pin2 = new RomanianCnp(ValidRomanianCnp);

        pin1.GetHashCode().Should().Be(pin2.GetHashCode());
    }

    #endregion

    #region Polymorphism Tests

    [Fact]
    public void CanStoreInCollection_WithMixedTypes()
    {
        var pins = new List<PersonalIdentificationNumber>
        {
            new RomanianCnp(ValidRomanianCnp),
            new MoldovanIdnp(ValidMoldovanIdnp)
        };

        pins.Should().HaveCount(2);
        pins[0].Type.Should().Be(IdentificationNumberType.RO);
        pins[1].Type.Should().Be(IdentificationNumberType.MD);
    }

    [Fact]
    public void CanIterateAndExtractInfo_WithMixedTypes()
    {
        var pins = new List<PersonalIdentificationNumber>
        {
            new RomanianCnp(ValidRomanianCnp),
            new MoldovanIdnp(ValidMoldovanIdnp)
        };

        var results = pins.Select(p => new
        {
            p.Value,
            p.Type,
            p.EncodesGender,
            p.EncodesDateOfBirth,
            Gender = p.ExtractGender(),
            DateOfBirth = p.ExtractDateOfBirth()
        }).ToList();

        // Romanian CNP
        results[0].EncodesGender.Should().BeTrue();
        results[0].EncodesDateOfBirth.Should().BeTrue();
        results[0].Gender.Should().Be(Gender.Male);
        results[0].DateOfBirth.Should().Be(new DateTime(2002, 8, 13));

        // Moldovan IDNP
        results[1].EncodesGender.Should().BeFalse();
        results[1].EncodesDateOfBirth.Should().BeFalse();
        results[1].Gender.Should().BeNull();
        results[1].DateOfBirth.Should().BeNull();
    }

    #endregion

    #region Factory Create vs Direct Constructor

    [Fact]
    public void Create_ShouldProduceSameResultAsDirectConstructor_ForRomanianCnp()
    {
        var fromFactory = PersonalIdentificationNumber.Create(ValidRomanianCnp, IdentificationNumberType.RO);
        var fromConstructor = new RomanianCnp(ValidRomanianCnp);

        fromFactory.Should().Be(fromConstructor);
        fromFactory.Value.Should().Be(fromConstructor.Value);
        fromFactory.Type.Should().Be(fromConstructor.Type);
    }

    [Fact]
    public void Create_ShouldProduceSameResultAsDirectConstructor_ForMoldovanIdnp()
    {
        var fromFactory = PersonalIdentificationNumber.Create(ValidMoldovanIdnp, IdentificationNumberType.MD);
        var fromConstructor = new MoldovanIdnp(ValidMoldovanIdnp);

        fromFactory.Should().Be(fromConstructor);
        fromFactory.Value.Should().Be(fromConstructor.Value);
        fromFactory.Type.Should().Be(fromConstructor.Type);
    }

    #endregion
}
