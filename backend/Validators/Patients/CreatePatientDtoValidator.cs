using FluentValidation;
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
            .LessThan(DateTime.Today).WithMessage("Date of birth must be in the past");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Invalid gender value");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone)
            .Matches(@"^\+[1-9]\d{6,14}$")
            .WithMessage("Invalid phone number format. Expected E.164 format: +[country code][number] (e.g., +40712345678)")
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
}
