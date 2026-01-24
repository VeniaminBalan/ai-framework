using PatientSyncHealth.DTOs.Common;
using PatientSyncHealth.DTOs.Examinations;

namespace PatientSyncHealth.Services.Interfaces;

public interface IExaminationService
{
    Task<ExaminationDto> GetByIdAsync(string externalId);
    Task<PagedResult<ExaminationListDto>> GetExaminationsAsync(ExaminationSearchParameters parameters);
    Task<List<ExaminationListDto>> GetByPatientIdAsync(string patientId);
    Task<List<ExaminationListDto>> GetByDoctorIdAsync(string doctorId);
    Task<ExaminationDto> CreateExaminationAsync(CreateExaminationDto dto);
    Task<ExaminationDto> CompleteExaminationAsync(string externalId, CompleteExaminationDto dto);
}
