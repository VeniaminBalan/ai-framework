using PatientSyncHealth.Domain.Common;

namespace PatientSyncHealth.Domain.DomainEvents;

public class ExaminationCreatedEvent : DomainEvent
{
    public string ExaminationExternalId { get; }
    public string PatientId { get; }
    public string DoctorId { get; }
    public DateTime ExaminationDate { get; }

    public ExaminationCreatedEvent(
        string examinationExternalId,
        string patientId,
        string doctorId,
        DateTime examinationDate)
    {
        ExaminationExternalId = examinationExternalId;
        PatientId = patientId;
        DoctorId = doctorId;
        ExaminationDate = examinationDate;
    }
}
