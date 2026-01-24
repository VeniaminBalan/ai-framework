using PatientSyncHealth.Domain.Common;

namespace PatientSyncHealth.Domain.DomainEvents;

public class AppointmentCompletedEvent : DomainEvent
{
    public string AppointmentExternalId { get; }
    public string PatientId { get; }
    public string DoctorId { get; }
    public DateTime ScheduledDateTime { get; }
    public string? ResultingExaminationId { get; }

    public AppointmentCompletedEvent(
        string appointmentExternalId,
        string patientId,
        string doctorId,
        DateTime scheduledDateTime,
        string? resultingExaminationId)
    {
        AppointmentExternalId = appointmentExternalId;
        PatientId = patientId;
        DoctorId = doctorId;
        ScheduledDateTime = scheduledDateTime;
        ResultingExaminationId = resultingExaminationId;
    }
}
