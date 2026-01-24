using PatientSyncHealth.Domain.Common;

namespace PatientSyncHealth.Domain.DomainEvents;

public class NurseCreatedEvent : DomainEvent
{
    public string NurseExternalId { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string Department { get; }

    public NurseCreatedEvent(string nurseExternalId, string firstName, string lastName, string department)
    {
        NurseExternalId = nurseExternalId;
        FirstName = firstName;
        LastName = lastName;
        Department = department;
    }
}
