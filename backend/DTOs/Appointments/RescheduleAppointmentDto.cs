using System.ComponentModel.DataAnnotations;

namespace PatientSyncHealth.DTOs.Appointments;

public record RescheduleAppointmentDto
{
    [Required]
    public required DateTime ScheduledDateTime { get; init; }

    public int? DurationMinutes { get; init; }
}
