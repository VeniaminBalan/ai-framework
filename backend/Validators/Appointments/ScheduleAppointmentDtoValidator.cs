using FluentValidation;
using PatientSyncHealth.Domain.Interfaces;
using PatientSyncHealth.DTOs.Appointments;

namespace PatientSyncHealth.Validators.Appointments;

public class ScheduleAppointmentDtoValidator : AbstractValidator<ScheduleAppointmentDto>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly INurseRepository _nurseRepository;
    private readonly IAppointmentRepository _appointmentRepository;

    public ScheduleAppointmentDtoValidator(
        IPatientRepository patientRepository,
        IDoctorRepository doctorRepository,
        INurseRepository nurseRepository,
        IAppointmentRepository appointmentRepository)
    {
        _patientRepository = patientRepository;
        _doctorRepository = doctorRepository;
        _nurseRepository = nurseRepository;
        _appointmentRepository = appointmentRepository;

        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("Patient ID is required")
            .MustAsync(PatientExists).WithMessage("Patient not found")
            .MustAsync(PatientIsActive).WithMessage("Patient is not active");

        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("Doctor ID is required")
            .MustAsync(DoctorExists).WithMessage("Doctor not found")
            .MustAsync(DoctorIsActive).WithMessage("Doctor is not active");

        RuleFor(x => x.ScheduledByNurseId)
            .MustAsync(NurseExistsIfProvided).WithMessage("Nurse not found")
            .MustAsync(NurseIsActiveIfProvided).WithMessage("Nurse is not active")
            .When(x => !string.IsNullOrWhiteSpace(x.ScheduledByNurseId));

        RuleFor(x => x.ScheduledDateTime)
            .NotEmpty().WithMessage("Scheduled date/time is required")
            .GreaterThan(DateTime.Now).WithMessage("Appointment must be scheduled in the future");

        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(15, 120).WithMessage("Duration must be between 15 and 120 minutes");

        RuleFor(x => x.Purpose)
            .IsInEnum().WithMessage("Invalid appointment purpose");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes cannot exceed 2000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));

        RuleFor(x => x)
            .MustAsync(NoConflictingAppointment)
            .WithMessage("There is already an appointment scheduled for this doctor at the specified time");
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

    private async Task<bool> NurseExistsIfProvided(string? nurseId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nurseId)) return true;
        return await _nurseRepository.ExistsAsync(nurseId);
    }

    private async Task<bool> NurseIsActiveIfProvided(string? nurseId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nurseId)) return true;
        return await _nurseRepository.IsActiveAsync(nurseId);
    }

    private async Task<bool> NoConflictingAppointment(ScheduleAppointmentDto dto, CancellationToken cancellationToken)
    {
        var startTime = dto.ScheduledDateTime;
        var endTime = startTime.AddMinutes(dto.DurationMinutes);
        return !await _appointmentRepository.HasConflictAsync(dto.DoctorId, startTime, endTime);
    }
}
