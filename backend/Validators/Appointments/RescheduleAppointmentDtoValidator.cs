using FluentValidation;
using PatientSyncHealth.DTOs.Appointments;

namespace PatientSyncHealth.Validators.Appointments;

public class RescheduleAppointmentDtoValidator : AbstractValidator<RescheduleAppointmentDto>
{
    public RescheduleAppointmentDtoValidator()
    {
        RuleFor(x => x.ScheduledDateTime)
            .NotEmpty().WithMessage("Scheduled date/time is required")
            .GreaterThan(DateTime.Now).WithMessage("Appointment must be scheduled in the future");

        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(15, 120).WithMessage("Duration must be between 15 and 120 minutes")
            .When(x => x.DurationMinutes.HasValue);
    }
}
