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

        RuleFor(x => x.Cnp)
            .NotEmpty().WithMessage("CNP is required")
            .Length(13).WithMessage("CNP must be exactly 13 digits")
            .Matches(@"^\d{13}$").WithMessage("CNP must contain only digits")
            .Must(CnpValidator.IsValidChecksum).WithMessage("CNP has invalid checksum")
            .Must(CnpValidator.IsValidDate).WithMessage("CNP contains invalid date")
            .MustAsync(BeUniqueCnp).WithMessage("A patient with this CNP already exists");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required")
            .LessThan(DateTime.Today).WithMessage("Date of birth must be in the past")
            .Must((dto, dob) => MatchesCnpDateOfBirth(dto.Cnp, dob))
            .WithMessage("Date of birth does not match the CNP");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Invalid gender value")
            .Must((dto, gender) => MatchesCnpGender(dto.Cnp, gender))
            .WithMessage("Gender does not match the CNP");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone)
            .Matches(@"^(\+40|0)[2-9]\d{8}$")
            .WithMessage("Invalid Romanian phone number format. Expected +40xxxxxxxxx or 0xxxxxxxxx")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.Address)
            .SetValidator(new AddressDtoValidator()!)
            .When(x => x.Address != null);

        RuleFor(x => x.ExaminationFrequency)
            .IsInEnum().WithMessage("Invalid examination frequency");
    }

    private async Task<bool> BeUniqueCnp(string cnp, CancellationToken cancellationToken)
    {
        if (!CnpValidator.IsValidFormat(cnp))
            return true; // Let other validators handle format issues

        return !await _patientRepository.ExistsByCnpAsync(cnp);
    }

    private static bool MatchesCnpDateOfBirth(string cnp, DateTime dateOfBirth)
    {
        var cnpDob = CnpValidator.ExtractDateOfBirth(cnp);
        if (cnpDob == null)
            return true; // Let other validators handle CNP format issues

        return cnpDob.Value.Date == dateOfBirth.Date;
    }

    private static bool MatchesCnpGender(string cnp, Gender gender)
    {
        var cnpGender = CnpValidator.ExtractGender(cnp);
        if (cnpGender == null)
            return true; // Let other validators handle CNP format issues

        // Allow if CNP indicates "Other" or if genders match
        return cnpGender == Gender.Other || cnpGender == gender;
    }
}
