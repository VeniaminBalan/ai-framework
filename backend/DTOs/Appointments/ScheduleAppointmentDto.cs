using System.ComponentModel.DataAnnotations;
using PatientSyncHealth.Domain.Enums;

namespace PatientSyncHealth.DTOs.Appointments;

public record ScheduleAppointmentDto
{
    [Required]
    public required string PatientId { get; init; }

    [Required]
    public required string DoctorId { get; init; }

    public string? ScheduledByNurseId { get; init; }

    [Required]
    public required DateTime ScheduledDateTime { get; init; }

    [Required]
    public required int DurationMinutes { get; init; }

    [Required]
    public required AppointmentPurpose Purpose { get; init; }

    [MaxLength(2000)]
    public string? Notes { get; init; }
}
