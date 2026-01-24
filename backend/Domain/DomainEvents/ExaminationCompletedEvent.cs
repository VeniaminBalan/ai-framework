using PatientSyncHealth.Domain.Common;

namespace PatientSyncHealth.Domain.DomainEvents;

public class ExaminationCompletedEvent : DomainEvent
{
    public string ExaminationExternalId { get; }
    public string PatientId { get; }
    public string DoctorId { get; }
    public DateTime ExaminationDate { get; }
    public string? Diagnosis { get; }

    public ExaminationCompletedEvent(
        string examinationExternalId,
        string patientId,
        string doctorId,
        DateTime examinationDate,
        string? diagnosis)
    {
        ExaminationExternalId = examinationExternalId;
        PatientId = patientId;
        DoctorId = doctorId;
        ExaminationDate = examinationDate;
        Diagnosis = diagnosis;
    }
}
