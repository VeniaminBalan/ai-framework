using FluentValidation;
using PatientSyncHealth.DTOs.Nurses;

namespace PatientSyncHealth.Validators.Nurses;

public class UpdateNurseDtoValidator : AbstractValidator<UpdateNurseDto>
{
    public UpdateNurseDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");

        RuleFor(x => x.DepartmentName)
            .NotEmpty().WithMessage("Department name is required")
            .MaximumLength(100).WithMessage("Department name cannot exceed 100 characters");

        RuleFor(x => x.DepartmentCode)
            .MaximumLength(20).WithMessage("Department code cannot exceed 20 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.DepartmentCode));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone)
            .Matches(@"^\+[1-9]\d{6,14}$")
            .WithMessage("Invalid phone number format. Expected E.164 format: +[country code][number] (e.g., +40712345678)")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));
    }
}
