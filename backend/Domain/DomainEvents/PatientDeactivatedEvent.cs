using PatientSyncHealth.Domain.Common;

namespace PatientSyncHealth.Domain.DomainEvents;

public class PatientDeactivatedEvent : DomainEvent
{
    public string PatientExternalId { get; }
    public string FullName { get; }

    public PatientDeactivatedEvent(string patientExternalId, string fullName)
    {
        PatientExternalId = patientExternalId;
        FullName = fullName;
    }
}
