using PatientSyncHealth.Domain.Aggregates.Nurse;
using PatientSyncHealth.DTOs.Common;
using PatientSyncHealth.DTOs.Nurses;

namespace PatientSyncHealth.Domain.Interfaces;

public interface INurseRepository
{
    Task<Nurse?> GetByIdAsync(string externalId);
    Task<PagedResult<NurseListDto>> GetPagedAsync(NurseSearchParameters parameters);
    Task<bool> ExistsAsync(string externalId);
    Task<bool> IsActiveAsync(string externalId);
    Task AddAsync(Nurse nurse);
    void Update(Nurse nurse);
    void Remove(Nurse nurse);
}
