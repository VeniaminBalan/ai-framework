using PatientSyncHealth.Domain.Common;

namespace PatientSyncHealth.Domain.DomainEvents;

public class AppointmentCancelledEvent : DomainEvent
{
    public string AppointmentExternalId { get; }
    public string PatientId { get; }
    public string DoctorId { get; }
    public DateTime ScheduledDateTime { get; }
    public string? Reason { get; }

    public AppointmentCancelledEvent(
        string appointmentExternalId,
        string patientId,
        string doctorId,
        DateTime scheduledDateTime,
        string? reason)
    {
        AppointmentExternalId = appointmentExternalId;
        PatientId = patientId;
        DoctorId = doctorId;
        ScheduledDateTime = scheduledDateTime;
        Reason = reason;
    }
}
