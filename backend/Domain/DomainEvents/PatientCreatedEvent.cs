using PatientSyncHealth.Domain.Common;

namespace PatientSyncHealth.Domain.DomainEvents;

public class PatientCreatedEvent : DomainEvent
{
    public string PatientExternalId { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string Cnp { get; }

    public PatientCreatedEvent(string patientExternalId, string firstName, string lastName, string cnp)
    {
        PatientExternalId = patientExternalId;
        FirstName = firstName;
        LastName = lastName;
        Cnp = cnp;
    }
}
