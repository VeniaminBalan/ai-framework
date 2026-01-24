using PatientSyncHealth.Domain.Enums;

namespace PatientSyncHealth.DTOs.Appointments;

public record AppointmentDto
{
    public required string Id { get; init; }
    public required string PatientId { get; init; }
    public required string DoctorId { get; init; }
    public string? ScheduledByNurseId { get; init; }
    public required DateTime ScheduledDateTime { get; init; }
    public required TimeSpan Duration { get; init; }
    public required AppointmentPurpose Purpose { get; init; }
    public required AppointmentStatus Status { get; init; }
    public string? ResultingExaminationId { get; init; }
    public string? CancellationReason { get; init; }
    public string? Notes { get; init; }
    public required DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
}
