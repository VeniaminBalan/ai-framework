using FluentValidation;
using PatientSyncHealth.Domain.Enums;
using PatientSyncHealth.Domain.Interfaces;
using PatientSyncHealth.DTOs.Patients;
using PatientSyncHealth.Validators.Common;
using PatientSyncHealth.Validators.CustomValidators;

namespace PatientSyncHealth.Validators.Patients;

public class CreatePatientDtoValidator : AbstractValidator<CreatePatientDto>
{
    private readonly IPatientRepository _patientRepository;

    public CreatePatientDtoValidator(IPatientRepository patientRepository)
    {
        _patientRepository = patientRepository;

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");

        RuleFor(x => x.IdentificationNumberType)
            .IsInEnum().WithMessage("Invalid identification number type");

        RuleFor(x => x.IdentificationNumber)
            .NotEmpty().WithMessage("Identification number is required")
            .Length(13).WithMessage("Identification number must be exactly 13 digits")
            .Matches(@"^\d{13}$").WithMessage("Identification number must contain only digits")
            .Must((dto, value) => IdentificationNumberValidator.IsValidFormat(value, dto.IdentificationNumberType))
            .WithMessage((dto, _) => GetFormatErrorMessage(dto.IdentificationNumberType))
            .Must((dto, value) => IdentificationNumberValidator.IsValidChecksum(value, dto.IdentificationNumberType))
            .WithMessage((dto, _) => GetChecksumErrorMessage(dto.IdentificationNumberType))
            .Must((dto, value) => IdentificationNumberValidator.IsValidStructure(value, dto.IdentificationNumberType))
            .WithMessage((dto, _) => GetStructureErrorMessage(dto.IdentificationNumberType))
            .MustAsync(BeUniqueIdentificationNumber).WithMessage("A patient with this identification number already exists");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required")
            .LessThan(DateTime.Today).WithMessage("Date of birth must be in the past")
            .Must((dto, dob) => MatchesIdentificationNumberDateOfBirth(dto.IdentificationNumber, dto.IdentificationNumberType, dob))
            .WithMessage("Date of birth does not match the identification number")
            .When(dto => IdentificationNumberValidator.EncodesDateOfBirth(dto.IdentificationNumberType));

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Invalid gender value")
            .Must((dto, gender) => MatchesIdentificationNumberGender(dto.IdentificationNumber, dto.IdentificationNumberType, gender))
            .WithMessage("Gender does not match the identification number")
            .When(dto => IdentificationNumberValidator.EncodesGender(dto.IdentificationNumberType));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone)
            .Matches(@"^(\+40|0)[2-9]\d{8}$|^(\+373|0)[0-9]\d{7}$")
            .WithMessage("Invalid phone number format. Expected Romanian (+40xxxxxxxxx or 0xxxxxxxxx) or Moldovan (+373xxxxxxxx or 0xxxxxxxx)")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.Address)
            .SetValidator(new AddressDtoValidator()!)
            .When(x => x.Address != null);

        RuleFor(x => x.ExaminationFrequency)
            .IsInEnum().WithMessage("Invalid examination frequency");
    }

    private async Task<bool> BeUniqueIdentificationNumber(CreatePatientDto dto, string value, CancellationToken cancellationToken)
    {
        if (!IdentificationNumberValidator.IsValidFormat(value, dto.IdentificationNumberType))
            return true; // Let other validators handle format issues

        return !await _patientRepository.ExistsByIdentificationNumberAsync(value);
    }

    private static bool MatchesIdentificationNumberDateOfBirth(string value, IdentificationNumberType type, DateTime dateOfBirth)
    {
        var extractedDob = IdentificationNumberValidator.ExtractDateOfBirth(value, type);
        if (extractedDob == null)
            return true; // Let other validators handle format issues or type doesn't encode DOB

        return extractedDob.Value.Date == dateOfBirth.Date;
    }

    private static bool MatchesIdentificationNumberGender(string value, IdentificationNumberType type, Gender gender)
    {
        var extractedGender = IdentificationNumberValidator.ExtractGender(value, type);
        if (extractedGender == null)
            return true; // Let other validators handle format issues or type doesn't encode gender

        // Allow if extracted indicates "Other" or if genders match
        return extractedGender == Gender.Other || extractedGender == gender;
    }

    private static string GetFormatErrorMessage(IdentificationNumberType type)
    {
        return type switch
        {
            IdentificationNumberType.RO => "Invalid Romanian CNP format",
            IdentificationNumberType.MD => "Invalid Moldovan IDNP format. IDNP must start with '2'",
            _ => "Invalid identification number format"
        };
    }

    private static string GetChecksumErrorMessage(IdentificationNumberType type)
    {
        return type switch
        {
            IdentificationNumberType.RO => "CNP has invalid checksum",
            IdentificationNumberType.MD => "IDNP has invalid checksum",
            _ => "Identification number has invalid checksum"
        };
    }

    private static string GetStructureErrorMessage(IdentificationNumberType type)
    {
        return type switch
        {
            IdentificationNumberType.RO => "CNP contains invalid date or county code",
            IdentificationNumberType.MD => "IDNP has invalid structure",
            _ => "Identification number has invalid structure"
        };
    }
}
