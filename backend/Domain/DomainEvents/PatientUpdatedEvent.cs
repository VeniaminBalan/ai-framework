using PatientSyncHealth.Domain.Common;

namespace PatientSyncHealth.Domain.DomainEvents;

public class PatientUpdatedEvent : DomainEvent
{
    public string PatientExternalId { get; }
    public string FullName { get; }

    public PatientUpdatedEvent(string patientExternalId, string fullName)
    {
        PatientExternalId = patientExternalId;
        FullName = fullName;
    }
}
