using PatientSyncHealth.Domain.Common;

namespace PatientSyncHealth.Domain.DomainEvents;

public class AppointmentRescheduledEvent : DomainEvent
{
    public string AppointmentExternalId { get; }
    public string PatientId { get; }
    public string DoctorId { get; }
    public DateTime OldDateTime { get; }
    public DateTime NewDateTime { get; }

    public AppointmentRescheduledEvent(
        string appointmentExternalId,
        string patientId,
        string doctorId,
        DateTime oldDateTime,
        DateTime newDateTime)
    {
        AppointmentExternalId = appointmentExternalId;
        PatientId = patientId;
        DoctorId = doctorId;
        OldDateTime = oldDateTime;
        NewDateTime = newDateTime;
    }
}
