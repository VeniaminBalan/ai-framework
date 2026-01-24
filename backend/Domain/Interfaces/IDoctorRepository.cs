using PatientSyncHealth.Domain.Aggregates.Doctor;
using PatientSyncHealth.DTOs.Common;
using PatientSyncHealth.DTOs.Doctors;

namespace PatientSyncHealth.Domain.Interfaces;

public interface IDoctorRepository
{
    Task<Doctor?> GetByIdAsync(string externalId);
    Task<Doctor?> GetByLicenseNumberAsync(string licenseNumber);
    Task<PagedResult<DoctorListDto>> GetPagedAsync(DoctorSearchParameters parameters);
    Task<bool> ExistsAsync(string externalId);
    Task<bool> ExistsByLicenseNumberAsync(string licenseNumber);
    Task<bool> IsActiveAsync(string externalId);
    Task AddAsync(Doctor doctor);
    void Update(Doctor doctor);
    void Remove(Doctor doctor);
}
