using PatientSyncHealth.DTOs.Common;
using PatientSyncHealth.DTOs.Doctors;

namespace PatientSyncHealth.Services.Interfaces;

public interface IDoctorService
{
    Task<DoctorDto> GetByIdAsync(string externalId);
    Task<PagedResult<DoctorListDto>> GetDoctorsAsync(DoctorSearchParameters parameters);
    Task<DoctorDto> CreateDoctorAsync(CreateDoctorDto dto);
    Task<DoctorDto> UpdateDoctorAsync(string externalId, UpdateDoctorDto dto);
    Task DeactivateDoctorAsync(string externalId);
    Task ReactivateDoctorAsync(string externalId);
}
