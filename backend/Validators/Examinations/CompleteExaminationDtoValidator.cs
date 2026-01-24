using FluentValidation;
using PatientSyncHealth.DTOs.Examinations;

namespace PatientSyncHealth.Validators.Examinations;

public class CompleteExaminationDtoValidator : AbstractValidator<CompleteExaminationDto>
{
    public CompleteExaminationDtoValidator()
    {
        RuleFor(x => x.Diagnosis)
            .MaximumLength(1000).WithMessage("Diagnosis cannot exceed 1000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Diagnosis));

        RuleFor(x => x.Notes)
            .MaximumLength(4000).WithMessage("Notes cannot exceed 4000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }
}
