using PatientSyncHealth.Domain.Aggregates.Examination;
using PatientSyncHealth.DTOs.Common;
using PatientSyncHealth.DTOs.Examinations;

namespace PatientSyncHealth.Domain.Interfaces;

public interface IExaminationRepository
{
    Task<Examination?> GetByIdAsync(string externalId);
    Task<PagedResult<ExaminationListDto>> GetPagedAsync(ExaminationSearchParameters parameters);
    Task<List<ExaminationListDto>> GetByPatientIdAsync(string patientId);
    Task<List<ExaminationListDto>> GetByDoctorIdAsync(string doctorId);
    Task<bool> ExistsAsync(string externalId);
    Task AddAsync(Examination examination);
    void Update(Examination examination);
    void Remove(Examination examination);
}
