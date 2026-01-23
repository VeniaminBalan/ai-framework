using FluentValidation;
using PatientSyncHealth.DTOs.Common;

namespace PatientSyncHealth.Validators.Common;

public class AddressDtoValidator : AbstractValidator<AddressDto>
{
    public AddressDtoValidator()
    {
        RuleFor(x => x.Street)
            .NotEmpty().WithMessage("Street is required")
            .MaximumLength(200).WithMessage("Street cannot exceed 200 characters");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required")
            .MaximumLength(100).WithMessage("City cannot exceed 100 characters");

        RuleFor(x => x.County)
            .NotEmpty().WithMessage("County is required")
            .MaximumLength(100).WithMessage("County cannot exceed 100 characters");

        RuleFor(x => x.PostalCode)
            .Matches(@"^\d{6}$")
            .WithMessage("Postal code must be exactly 6 digits")
            .When(x => !string.IsNullOrWhiteSpace(x.PostalCode));

        RuleFor(x => x.Country)
            .MaximumLength(100).WithMessage("Country cannot exceed 100 characters");
    }
}
