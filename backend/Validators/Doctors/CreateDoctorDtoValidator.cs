using FluentValidation;
using PatientSyncHealth.Domain.Interfaces;
using PatientSyncHealth.DTOs.Doctors;

namespace PatientSyncHealth.Validators.Doctors;

public class CreateDoctorDtoValidator : AbstractValidator<CreateDoctorDto>
{
    private readonly IDoctorRepository _doctorRepository;

    public CreateDoctorDtoValidator(IDoctorRepository doctorRepository)
    {
        _doctorRepository = doctorRepository;

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");

        RuleFor(x => x.Specialization)
            .NotEmpty().WithMessage("Specialization is required")
            .MaximumLength(100).WithMessage("Specialization cannot exceed 100 characters");

        RuleFor(x => x.LicenseNumber)
            .NotEmpty().WithMessage("License number is required")
            .Matches(@"^[A-Za-z0-9]{5,20}$").WithMessage("License number must be alphanumeric and between 5-20 characters")
            .MustAsync(BeUniqueLicenseNumber).WithMessage("A doctor with this license number already exists");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone)
            .Matches(@"^\+[1-9]\d{6,14}$")
            .WithMessage("Invalid phone number format. Expected E.164 format: +[country code][number] (e.g., +40712345678)")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }

    private async Task<bool> BeUniqueLicenseNumber(string licenseNumber, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(licenseNumber))
            return true;

        return !await _doctorRepository.ExistsByLicenseNumberAsync(licenseNumber.ToUpperInvariant());
    }
}
