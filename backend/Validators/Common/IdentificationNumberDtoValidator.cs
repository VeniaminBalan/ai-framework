using FluentValidation;
using PatientSyncHealth.Domain.Enums;
using PatientSyncHealth.DTOs.Common;
using PatientSyncHealth.Validators.CustomValidators;

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
            .Matches(@"^\d{13}$").WithMessage("Identification number must contain only digits")
            .Must((dto, value) => IdentificationNumberValidator.IsValidFormat(value, dto.Type))
            .WithMessage((dto, _) => GetFormatErrorMessage(dto.Type))
            .Must((dto, value) => IdentificationNumberValidator.IsValidChecksum(value, dto.Type))
            .WithMessage((dto, _) => GetChecksumErrorMessage(dto.Type))
            .Must((dto, value) => IdentificationNumberValidator.IsValidStructure(value, dto.Type))
            .WithMessage((dto, _) => GetStructureErrorMessage(dto.Type));
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
