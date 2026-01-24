using PatientSyncHealth.Domain.Common;

namespace PatientSyncHealth.Domain.DomainEvents;

public class DoctorDeactivatedEvent : DomainEvent
{
    public string DoctorExternalId { get; }
    public string FullName { get; }

    public DoctorDeactivatedEvent(string doctorExternalId, string fullName)
    {
        DoctorExternalId = doctorExternalId;
        FullName = fullName;
    }
}
