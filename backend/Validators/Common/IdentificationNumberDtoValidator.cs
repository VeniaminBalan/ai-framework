using FluentValidation;
using PatientSyncHealth.DTOs.Common;

namespace PatientSyncHealth.Validators.Common;

public class IdentificationNumberDtoValidator : AbstractValidator<IdentificationNumberDto>
{
    public IdentificationNumberDtoValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid identification number type");

        RuleFor(x => x.Value)
            .NotEmpty().WithMessage("Identification number is required")
            .Length(13).WithMessage("Identification number must be exactly 13 digits")
            .Matches(@"^\d{13}$").WithMessage("Identification number must contain only digits");
    }
}
