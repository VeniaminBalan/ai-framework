using PatientSyncHealth.Domain.Enums;

namespace PatientSyncHealth.DTOs.Appointments;

public record AppointmentListDto
{
    public required string Id { get; init; }
    public required string PatientId { get; init; }
    public required string DoctorId { get; init; }
    public required DateTime ScheduledDateTime { get; init; }
    public required TimeSpan Duration { get; init; }
    public required AppointmentPurpose Purpose { get; init; }
    public required AppointmentStatus Status { get; init; }
}
