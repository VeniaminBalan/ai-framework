using FluentValidation;
using PatientSyncHealth.Domain.Interfaces;
using PatientSyncHealth.DTOs.Examinations;

namespace PatientSyncHealth.Validators.Examinations;

public class CreateExaminationDtoValidator : AbstractValidator<CreateExaminationDto>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IDoctorRepository _doctorRepository;

    public CreateExaminationDtoValidator(
        IPatientRepository patientRepository,
        IDoctorRepository doctorRepository)
    {
        _patientRepository = patientRepository;
        _doctorRepository = doctorRepository;

        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("Patient ID is required")
            .MustAsync(PatientExists).WithMessage("Patient not found")
            .MustAsync(PatientIsActive).WithMessage("Patient is not active");

        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("Doctor ID is required")
            .MustAsync(DoctorExists).WithMessage("Doctor not found")
            .MustAsync(DoctorIsActive).WithMessage("Doctor is not active");

        RuleFor(x => x.ExaminationDate)
            .NotEmpty().WithMessage("Examination date is required")
            .LessThanOrEqualTo(DateTime.Today).WithMessage("Examination date cannot be in the future");

        RuleFor(x => x.Diagnosis)
            .MaximumLength(1000).WithMessage("Diagnosis cannot exceed 1000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Diagnosis));

        RuleFor(x => x.Notes)
            .MaximumLength(4000).WithMessage("Notes cannot exceed 4000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }

    private async Task<bool> PatientExists(string patientId, CancellationToken cancellationToken)
    {
        return await _patientRepository.ExistsAsync(patientId);
    }

    private async Task<bool> PatientIsActive(string patientId, CancellationToken cancellationToken)
    {
        var patient = await _patientRepository.GetByIdAsync(patientId);
        return patient?.IsActive ?? false;
    }

    private async Task<bool> DoctorExists(string doctorId, CancellationToken cancellationToken)
    {
        return await _doctorRepository.ExistsAsync(doctorId);
    }

    private async Task<bool> DoctorIsActive(string doctorId, CancellationToken cancellationToken)
    {
        return await _doctorRepository.IsActiveAsync(doctorId);
    }
}
