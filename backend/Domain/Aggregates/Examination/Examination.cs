using PatientSyncHealth.Domain.Common;
using PatientSyncHealth.Domain.DomainEvents;
using PatientSyncHealth.Domain.Exceptions;

namespace PatientSyncHealth.Domain.Aggregates.Examination;

public class Examination : AggregateRoot
{
    public string PatientId { get; private set; } = string.Empty;
    public string DoctorId { get; private set; } = string.Empty;
    public DateTime ExaminationDate { get; private set; }
    public string? Diagnosis { get; private set; }
    public string? Notes { get; private set; }
    public bool IsCompleted { get; private set; }

    private Examination() { } // EF Core

    public static Examination Create(
        string patientId,
        string doctorId,
        DateTime examinationDate,
        string? diagnosis = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(patientId))
            throw new DomainException("Patient ID is required");

        if (string.IsNullOrWhiteSpace(doctorId))
            throw new DomainException("Doctor ID is required");

        if (examinationDate.Date > DateTime.Today)
            throw new DomainException("Examination date cannot be in the future");

        var examination = new Examination
        {
            PatientId = patientId,
            DoctorId = doctorId,
            ExaminationDate = examinationDate.Date,
            Diagnosis = diagnosis?.Trim(),
            Notes = notes?.Trim(),
            IsCompleted = false
        };

        examination.AddDomainEvent(new ExaminationCreatedEvent(
            examination.ExternalId,
            patientId,
            doctorId,
            examinationDate));

        return examination;
    }

    public void Complete(string? diagnosis = null, string? notes = null)
    {
        if (IsCompleted)
            throw new DomainException("Examination is already completed");

        Diagnosis = diagnosis?.Trim();
        Notes = notes?.Trim();
        IsCompleted = true;

        AddDomainEvent(new ExaminationCompletedEvent(
            ExternalId,
            PatientId,
            DoctorId,
            ExaminationDate,
            Diagnosis));
    }

    public void UpdateDiagnosis(string? diagnosis)
    {
        if (!IsCompleted)
            throw new DomainException("Cannot update diagnosis on an incomplete examination");

        Diagnosis = diagnosis?.Trim();
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes?.Trim();
    }
}
