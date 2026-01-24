using PatientSyncHealth.Domain.Common;

namespace PatientSyncHealth.Domain.DomainEvents;

public class PatientCreatedEvent : DomainEvent
{
    public string PatientExternalId { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string IdentificationNumber { get; }

    public PatientCreatedEvent(string patientExternalId, string firstName, string lastName, string identificationNumber)
    {
        PatientExternalId = patientExternalId;
        FirstName = firstName;
        LastName = lastName;
        IdentificationNumber = identificationNumber;
    }
}
