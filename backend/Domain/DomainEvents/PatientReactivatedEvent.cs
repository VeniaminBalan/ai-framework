using PatientSyncHealth.Domain.Common;

namespace PatientSyncHealth.Domain.DomainEvents;

public class PatientReactivatedEvent : DomainEvent
{
    public string PatientExternalId { get; }
    public string FullName { get; }

    public PatientReactivatedEvent(string patientExternalId, string fullName)
    {
        PatientExternalId = patientExternalId;
        FullName = fullName;
    }
}
