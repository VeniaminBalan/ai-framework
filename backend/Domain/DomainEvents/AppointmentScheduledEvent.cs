using PatientSyncHealth.Domain.Common;
using PatientSyncHealth.Domain.Enums;

namespace PatientSyncHealth.Domain.DomainEvents;

public class AppointmentScheduledEvent : DomainEvent
{
    public string AppointmentExternalId { get; }
    public string PatientId { get; }
    public string DoctorId { get; }
    public DateTime ScheduledDateTime { get; }
    public AppointmentPurpose Purpose { get; }

    public AppointmentScheduledEvent(
        string appointmentExternalId,
        string patientId,
        string doctorId,
        DateTime scheduledDateTime,
        AppointmentPurpose purpose)
    {
        AppointmentExternalId = appointmentExternalId;
        PatientId = patientId;
        DoctorId = doctorId;
        ScheduledDateTime = scheduledDateTime;
        Purpose = purpose;
    }
}
