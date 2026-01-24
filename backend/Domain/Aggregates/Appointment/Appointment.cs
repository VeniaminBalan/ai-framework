using PatientSyncHealth.Domain.Common;
using PatientSyncHealth.Domain.DomainEvents;
using PatientSyncHealth.Domain.Enums;
using PatientSyncHealth.Domain.Exceptions;

namespace PatientSyncHealth.Domain.Aggregates.Appointment;

public class Appointment : AggregateRoot
{
    public string PatientId { get; private set; } = string.Empty;
    public string DoctorId { get; private set; } = string.Empty;
    public string? ScheduledByNurseId { get; private set; }
    public DateTime ScheduledDateTime { get; private set; }
    public TimeSpan Duration { get; private set; }
    public AppointmentPurpose Purpose { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public string? ResultingExaminationId { get; private set; }
    public string? CancellationReason { get; private set; }
    public string? Notes { get; private set; }

    public DateTime EndDateTime => ScheduledDateTime.Add(Duration);

    private Appointment() { } // EF Core

    public static Appointment Schedule(
        string patientId,
        string doctorId,
        DateTime scheduledDateTime,
        TimeSpan duration,
        AppointmentPurpose purpose,
        string? scheduledByNurseId = null,
        string? notes = null)
    {
        ValidateScheduleParameters(patientId, doctorId, scheduledDateTime, duration);

        var appointment = new Appointment
        {
            PatientId = patientId,
            DoctorId = doctorId,
            ScheduledByNurseId = scheduledByNurseId,
            ScheduledDateTime = scheduledDateTime,
            Duration = duration,
            Purpose = purpose,
            Status = AppointmentStatus.Scheduled,
            Notes = notes?.Trim()
        };

        appointment.AddDomainEvent(new AppointmentScheduledEvent(
            appointment.ExternalId,
            patientId,
            doctorId,
            scheduledDateTime,
            purpose));

        return appointment;
    }

    public void Reschedule(DateTime newDateTime, TimeSpan? newDuration = null)
    {
        EnsureCanBeModified();

        if (newDateTime <= DateTime.Now)
            throw new DomainException("Appointment must be scheduled in the future");

        var oldDateTime = ScheduledDateTime;
        ScheduledDateTime = newDateTime;

        if (newDuration.HasValue)
        {
            ValidateDuration(newDuration.Value);
            Duration = newDuration.Value;
        }

        AddDomainEvent(new AppointmentRescheduledEvent(
            ExternalId,
            PatientId,
            DoctorId,
            oldDateTime,
            newDateTime));
    }

    public void Confirm()
    {
        if (Status != AppointmentStatus.Scheduled)
            throw new DomainException("Only scheduled appointments can be confirmed");

        Status = AppointmentStatus.Confirmed;
    }

    public void Start()
    {
        if (Status != AppointmentStatus.Confirmed && Status != AppointmentStatus.Scheduled)
            throw new DomainException("Only scheduled or confirmed appointments can be started");

        Status = AppointmentStatus.InProgress;
    }

    public void Complete(string? resultingExaminationId = null)
    {
        if (Status != AppointmentStatus.InProgress)
            throw new DomainException("Only in-progress appointments can be completed");

        Status = AppointmentStatus.Completed;
        ResultingExaminationId = resultingExaminationId;

        AddDomainEvent(new AppointmentCompletedEvent(
            ExternalId,
            PatientId,
            DoctorId,
            ScheduledDateTime,
            resultingExaminationId));
    }

    public void Cancel(string? reason = null)
    {
        if (Status == AppointmentStatus.Completed)
            throw new DomainException("Completed appointments cannot be cancelled");

        if (Status == AppointmentStatus.Cancelled)
            throw new DomainException("Appointment is already cancelled");

        Status = AppointmentStatus.Cancelled;
        CancellationReason = reason?.Trim();

        AddDomainEvent(new AppointmentCancelledEvent(
            ExternalId,
            PatientId,
            DoctorId,
            ScheduledDateTime,
            reason));
    }

    public void MarkNoShow()
    {
        if (Status == AppointmentStatus.Completed)
            throw new DomainException("Completed appointments cannot be marked as no-show");

        if (Status == AppointmentStatus.Cancelled)
            throw new DomainException("Cancelled appointments cannot be marked as no-show");

        Status = AppointmentStatus.NoShow;
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes?.Trim();
    }

    public void LinkExamination(string examinationId)
    {
        if (string.IsNullOrWhiteSpace(examinationId))
            throw new DomainException("Examination ID is required");

        ResultingExaminationId = examinationId;
    }

    private void EnsureCanBeModified()
    {
        if (Status == AppointmentStatus.Completed)
            throw new DomainException("Completed appointments cannot be modified");

        if (Status == AppointmentStatus.Cancelled)
            throw new DomainException("Cancelled appointments cannot be modified");

        if (Status == AppointmentStatus.NoShow)
            throw new DomainException("No-show appointments cannot be modified");
    }

    private static void ValidateScheduleParameters(
        string patientId,
        string doctorId,
        DateTime scheduledDateTime,
        TimeSpan duration)
    {
        if (string.IsNullOrWhiteSpace(patientId))
            throw new DomainException("Patient ID is required");

        if (string.IsNullOrWhiteSpace(doctorId))
            throw new DomainException("Doctor ID is required");

        if (scheduledDateTime <= DateTime.Now)
            throw new DomainException("Appointment must be scheduled in the future");

        ValidateDuration(duration);
    }

    private static void ValidateDuration(TimeSpan duration)
    {
        if (duration < TimeSpan.FromMinutes(15))
            throw new DomainException("Appointment duration must be at least 15 minutes");

        if (duration > TimeSpan.FromHours(2))
            throw new DomainException("Appointment duration cannot exceed 2 hours");
    }
}
