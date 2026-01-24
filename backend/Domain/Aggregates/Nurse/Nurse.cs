using PatientSyncHealth.Domain.Common;
using PatientSyncHealth.Domain.DomainEvents;
using PatientSyncHealth.Domain.Exceptions;
using PatientSyncHealth.Domain.ValueObjects;

namespace PatientSyncHealth.Domain.Aggregates.Nurse;

public class Nurse : AggregateRoot
{
    public string? KeycloakUserId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public Department Department { get; private set; } = null!;
    public Email? Email { get; private set; }
    public PhoneNumber? Phone { get; private set; }
    public bool IsActive { get; private set; } = true;

    public string FullName => $"{FirstName} {LastName}";

    private Nurse() { } // EF Core

    public static Nurse Create(
        string firstName,
        string lastName,
        Department department,
        string? keycloakUserId = null,
        Email? email = null,
        PhoneNumber? phone = null)
    {
        ValidatePersonalInfo(firstName, lastName);

        var nurse = new Nurse
        {
            KeycloakUserId = keycloakUserId,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Department = department ?? throw new DomainException("Department is required"),
            Email = email,
            Phone = phone,
            IsActive = true
        };

        nurse.AddDomainEvent(new NurseCreatedEvent(
            nurse.ExternalId,
            nurse.FirstName,
            nurse.LastName,
            nurse.Department.Name));

        return nurse;
    }

    public void UpdatePersonalInfo(string firstName, string lastName)
    {
        EnsureActive();
        ValidatePersonalInfo(firstName, lastName);

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }

    public void UpdateDepartment(Department department)
    {
        EnsureActive();
        Department = department ?? throw new DomainException("Department is required");
    }

    public void UpdateContactInfo(Email? email, PhoneNumber? phone)
    {
        EnsureActive();
        Email = email;
        Phone = phone;
    }

    public void LinkToKeycloakUser(string keycloakUserId)
    {
        EnsureActive();

        if (string.IsNullOrWhiteSpace(keycloakUserId))
            throw new DomainException("Keycloak user ID is required");

        KeycloakUserId = keycloakUserId;
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new DomainException("Nurse is already deactivated");

        IsActive = false;
    }

    public void Reactivate()
    {
        if (IsActive)
            throw new DomainException("Nurse is already active");

        IsActive = true;
    }

    private void EnsureActive()
    {
        if (!IsActive)
            throw new DomainException("Cannot modify an inactive nurse");
    }

    private static void ValidatePersonalInfo(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("First name is required");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("Last name is required");
    }
}
