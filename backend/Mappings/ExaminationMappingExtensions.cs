using PatientSyncHealth.Domain.Aggregates.Examination;
using PatientSyncHealth.DTOs.Examinations;

namespace PatientSyncHealth.Mappings;

public static class ExaminationMappingExtensions
{
    public static ExaminationDto ToDto(this Examination examination)
    {
        ArgumentNullException.ThrowIfNull(examination);

        return new ExaminationDto
        {
            Id = examination.ExternalId,
            PatientId = examination.PatientId,
            DoctorId = examination.DoctorId,
            ExaminationDate = examination.ExaminationDate,
            Diagnosis = examination.Diagnosis,
            Notes = examination.Notes,
            IsCompleted = examination.IsCompleted,
            CreatedAt = examination.CreatedAt,
            CreatedBy = examination.CreatedBy,
            UpdatedAt = examination.UpdatedAt,
            UpdatedBy = examination.UpdatedBy
        };
    }

    public static ExaminationListDto ToListDto(this Examination examination)
    {
        ArgumentNullException.ThrowIfNull(examination);

        return new ExaminationListDto
        {
            Id = examination.ExternalId,
            PatientId = examination.PatientId,
            DoctorId = examination.DoctorId,
            ExaminationDate = examination.ExaminationDate,
            Diagnosis = examination.Diagnosis,
            IsCompleted = examination.IsCompleted
        };
    }
}
