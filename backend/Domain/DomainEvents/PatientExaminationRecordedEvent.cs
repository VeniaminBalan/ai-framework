using PatientSyncHealth.Domain.Common;

namespace PatientSyncHealth.Domain.DomainEvents;

public class PatientExaminationRecordedEvent : DomainEvent
{
    public string PatientExternalId { get; }
    public string FullName { get; }
    public DateTime ExaminationDate { get; }
    public DateTime NextExaminationDate { get; }

    public PatientExaminationRecordedEvent(
        string patientExternalId,
        string fullName,
        DateTime examinationDate,
        DateTime nextExaminationDate)
    {
        PatientExternalId = patientExternalId;
        FullName = fullName;
        ExaminationDate = examinationDate;
        NextExaminationDate = nextExaminationDate;
    }
}
