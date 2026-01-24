using PatientSyncHealth.Domain.Common;
using PatientSyncHealth.Domain.DomainEvents;
using PatientSyncHealth.Domain.Enums;
using PatientSyncHealth.Domain.Exceptions;
using PatientSyncHealth.Domain.ValueObjects;

namespace PatientSyncHealth.Domain.Aggregates.Patient;

public class Patient : AggregateRoot
{
    // Backing fields for EF Core mapping
    private string _identificationNumberValue = string.Empty;
    private IdentificationNumberType _identificationNumberType;
    private PersonalIdentificationNumber? _identificationNumber;

    // Personal Information
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;

    public PersonalIdentificationNumber IdentificationNumber
    {
        get => _identificationNumber ??= PersonalIdentificationNumber.Reconstruct(
            _identificationNumberValue, _identificationNumberType);
        private set
        {
            _identificationNumber = value;
            _identificationNumberValue = value.Value;
            _identificationNumberType = value.Type;
        }
    }

    public DateTime DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }

    // Contact Information
    public Email? Email { get; private set; }
    public PhoneNumber? Phone { get; private set; }
    public Address? Address { get; private set; }

    // Examination Schedule
    public ExaminationFrequency ExaminationFrequency { get; private set; }
    public DateTime? LastExaminationDate { get; private set; }
    public DateTime? NextExaminationDate { get; private set; }

    // Status
    public bool IsActive { get; private set; } = true;

    // Computed Properties
    public string FullName => $"{FirstName} {LastName}";

    public int Age
    {
        get
        {
            var today = DateTime.Today;
            var age = today.Year - DateOfBirth.Year;
            if (DateOfBirth.Date > today.AddYears(-age))
                age--;
            return age;
        }
    }

    public bool IsOverdueForExamination =>
        IsActive &&
        NextExaminationDate.HasValue &&
        NextExaminationDate.Value.Date < DateTime.Today;

    private Patient() { } // EF Core

    public static Patient Create(
        string firstName,
        string lastName,
        PersonalIdentificationNumber identificationNumber,
        DateTime dateOfBirth,
        Gender gender,
        ExaminationFrequency examinationFrequency,
        Email? email = null,
        PhoneNumber? phone = null,
        Address? address = null)
    {
        ValidatePersonalInfo(firstName, lastName, identificationNumber, dateOfBirth, gender);

        var patient = new Patient
        {
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            IdentificationNumber = identificationNumber,
            DateOfBirth = dateOfBirth.Date,
            Gender = gender,
            ExaminationFrequency = examinationFrequency,
            Email = email,
            Phone = phone,
            Address = address,
            IsActive = true
        };

        // Calculate initial next examination date (from today if no last examination)
        patient.NextExaminationDate = examinationFrequency.CalculateNextExaminationDate(DateTime.Today);

        patient.AddDomainEvent(new PatientCreatedEvent(
            patient.ExternalId,
            patient.FirstName,
            patient.LastName,
            patient.IdentificationNumber.Value));

        return patient;
    }

    public void UpdatePersonalInfo(string firstName, string lastName, DateTime dateOfBirth, Gender gender)
    {
        EnsureActive();

        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("First name is required");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("Last name is required");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        DateOfBirth = dateOfBirth.Date;
        Gender = gender;

        AddDomainEvent(new PatientUpdatedEvent(ExternalId, FullName));
    }

    public void UpdateContactInfo(Email? email, PhoneNumber? phone, Address? address)
    {
        EnsureActive();

        Email = email;
        Phone = phone;
        Address = address;

        AddDomainEvent(new PatientUpdatedEvent(ExternalId, FullName));
    }

    public void UpdateExaminationSchedule(ExaminationFrequency frequency)
    {
        EnsureActive();

        ExaminationFrequency = frequency;
        RecalculateNextExaminationDate();

        AddDomainEvent(new PatientUpdatedEvent(ExternalId, FullName));
    }

    public void RecordExamination(DateTime examinationDate)
    {
        EnsureActive();

        if (examinationDate.Date > DateTime.Today)
            throw new DomainException("Examination date cannot be in the future");

        if (LastExaminationDate.HasValue && examinationDate.Date < LastExaminationDate.Value.Date)
            throw new DomainException("Examination date cannot be before the last examination date");

        LastExaminationDate = examinationDate.Date;
        RecalculateNextExaminationDate();

        AddDomainEvent(new PatientExaminationRecordedEvent(
            ExternalId,
            FullName,
            examinationDate,
            NextExaminationDate!.Value));
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new DomainException("Patient is already deactivated");

        IsActive = false;

        AddDomainEvent(new PatientDeactivatedEvent(ExternalId, FullName));
    }

    public void Reactivate()
    {
        if (IsActive)
            throw new DomainException("Patient is already active");

        IsActive = true;
        RecalculateNextExaminationDate();

        AddDomainEvent(new PatientReactivatedEvent(ExternalId, FullName));
    }

    private void RecalculateNextExaminationDate()
    {
        var baseDate = LastExaminationDate ?? DateTime.Today;
        NextExaminationDate = ExaminationFrequency.CalculateNextExaminationDate(baseDate);
    }

    private void EnsureActive()
    {
        if (!IsActive)
            throw new DomainException("Cannot modify an inactive patient");
    }

    private static void ValidatePersonalInfo(
        string firstName,
        string lastName,
        PersonalIdentificationNumber identificationNumber,
        DateTime dateOfBirth,
        Gender gender)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("First name is required");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("Last name is required");

        // Only validate DateOfBirth match if the identification number encodes it
        if (identificationNumber.EncodesDateOfBirth)
        {
            var extractedDateOfBirth = identificationNumber.ExtractDateOfBirth();
            if (extractedDateOfBirth.HasValue && extractedDateOfBirth.Value.Date != dateOfBirth.Date)
                throw new DomainException(
                    $"Date of birth ({dateOfBirth:yyyy-MM-dd}) does not match identification number ({extractedDateOfBirth.Value:yyyy-MM-dd})");
        }

        // Only validate Gender match if the identification number encodes it
        if (identificationNumber.EncodesGender)
        {
            var extractedGender = identificationNumber.ExtractGender();
            if (extractedGender.HasValue && extractedGender.Value != gender && extractedGender.Value != Gender.Other)
                throw new DomainException(
                    $"Gender ({gender}) does not match identification number ({extractedGender.Value})");
        }
    }
}
