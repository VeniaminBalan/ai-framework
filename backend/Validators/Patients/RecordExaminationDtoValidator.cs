using FluentValidation;
using PatientSyncHealth.DTOs.Patients;

namespace PatientSyncHealth.Validators.Patients;

public class RecordExaminationDtoValidator : AbstractValidator<RecordExaminationDto>
{
    public RecordExaminationDtoValidator()
    {
        RuleFor(x => x.ExaminationDate)
            .NotEmpty().WithMessage("Examination date is required")
            .LessThanOrEqualTo(DateTime.Today).WithMessage("Examination date cannot be in the future");
    }
}
