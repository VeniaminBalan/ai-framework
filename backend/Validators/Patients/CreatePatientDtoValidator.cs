using FluentValidation;
using PatientSyncHealth.Domain.Enums;
using PatientSyncHealth.Domain.Interfaces;
using PatientSyncHealth.DTOs.Common;
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

        RuleFor(x => x.IdentificationNumber)
            .NotNull().WithMessage("Identification number is required")
            .SetValidator(new IdentificationNumberDtoValidator()!)
            .MustAsync(BeUniqueIdentificationNumber).WithMessage("A patient with this identification number already exists");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required")
            .LessThan(DateTime.Today).WithMessage("Date of birth must be in the past")
            .Must((dto, dob) => MatchesIdentificationNumberDateOfBirth(dto.IdentificationNumber, dob))
            .WithMessage("Date of birth does not match the identification number")
            .When(dto => dto.IdentificationNumber != null && IdentificationNumberValidator.EncodesDateOfBirth(dto.IdentificationNumber.Type));

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Invalid gender value")
            .Must((dto, gender) => MatchesIdentificationNumberGender(dto.IdentificationNumber, gender))
            .WithMessage("Gender does not match the identification number")
            .When(dto => dto.IdentificationNumber != null && IdentificationNumberValidator.EncodesGender(dto.IdentificationNumber.Type));

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

    private async Task<bool> BeUniqueIdentificationNumber(CreatePatientDto dto, IdentificationNumberDto idNumber, CancellationToken cancellationToken)
    {
        if (idNumber == null || !IdentificationNumberValidator.IsValidFormat(idNumber.Value, idNumber.Type))
            return true; // Let other validators handle format issues

        return !await _patientRepository.ExistsByIdentificationNumberAsync(idNumber.Value);
    }

    private static bool MatchesIdentificationNumberDateOfBirth(IdentificationNumberDto? idNumber, DateTime dateOfBirth)
    {
        if (idNumber == null)
            return true;

        var extractedDob = IdentificationNumberValidator.ExtractDateOfBirth(idNumber.Value, idNumber.Type);
        if (extractedDob == null)
            return true; // Let other validators handle format issues or type doesn't encode DOB

        return extractedDob.Value.Date == dateOfBirth.Date;
    }

    private static bool MatchesIdentificationNumberGender(IdentificationNumberDto? idNumber, Gender gender)
    {
        if (idNumber == null)
            return true;

        var extractedGender = IdentificationNumberValidator.ExtractGender(idNumber.Value, idNumber.Type);
        if (extractedGender == null)
            return true; // Let other validators handle format issues or type doesn't encode gender

        // Allow if extracted indicates "Other" or if genders match
        return extractedGender == Gender.Other || extractedGender == gender;
    }
}
