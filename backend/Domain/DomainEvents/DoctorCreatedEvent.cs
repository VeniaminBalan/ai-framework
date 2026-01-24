using PatientSyncHealth.Domain.Common;

namespace PatientSyncHealth.Domain.DomainEvents;

public class DoctorCreatedEvent : DomainEvent
{
    public string DoctorExternalId { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string LicenseNumber { get; }

    public DoctorCreatedEvent(string doctorExternalId, string firstName, string lastName, string licenseNumber)
    {
        DoctorExternalId = doctorExternalId;
        FirstName = firstName;
        LastName = lastName;
        LicenseNumber = licenseNumber;
    }
}
