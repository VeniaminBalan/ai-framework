using PatientSyncHealth.Domain.Common;
using PatientSyncHealth.Domain.DomainEvents;
using PatientSyncHealth.Domain.Exceptions;
using PatientSyncHealth.Domain.ValueObjects;

namespace PatientSyncHealth.Domain.Aggregates.Doctor;

public class Doctor : AggregateRoot
{
    public string? KeycloakUserId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public Specialization Specialization { get; private set; } = null!;
    public LicenseNumber LicenseNumber { get; private set; } = null!;
    public Email? Email { get; private set; }
    public PhoneNumber? Phone { get; private set; }
    public bool IsActive { get; private set; } = true;

    public string FullName => $"{FirstName} {LastName}";

    private Doctor() { } // EF Core

    public static Doctor Create(
        string firstName,
        string lastName,
        Specialization specialization,
        LicenseNumber licenseNumber,
        string? keycloakUserId = null,
        Email? email = null,
        PhoneNumber? phone = null)
    {
        ValidatePersonalInfo(firstName, lastName);

        var doctor = new Doctor
        {
            KeycloakUserId = keycloakUserId,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Specialization = specialization,
            LicenseNumber = licenseNumber,
            Email = email,
            Phone = phone,
            IsActive = true
        };

        doctor.AddDomainEvent(new DoctorCreatedEvent(
            doctor.ExternalId,
            doctor.FirstName,
            doctor.LastName,
            doctor.LicenseNumber.Value));

        return doctor;
    }

    public void UpdatePersonalInfo(string firstName, string lastName)
    {
        EnsureActive();
        ValidatePersonalInfo(firstName, lastName);

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }

    public void UpdateSpecialization(Specialization specialization)
    {
        EnsureActive();
        Specialization = specialization ?? throw new DomainException("Specialization is required");
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
            throw new DomainException("Doctor is already deactivated");

        IsActive = false;

        AddDomainEvent(new DoctorDeactivatedEvent(ExternalId, FullName));
    }

    public void Reactivate()
    {
        if (IsActive)
            throw new DomainException("Doctor is already active");

        IsActive = true;
    }

    private void EnsureActive()
    {
        if (!IsActive)
            throw new DomainException("Cannot modify an inactive doctor");
    }

    private static void ValidatePersonalInfo(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("First name is required");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("Last name is required");
    }
}
