using PatientSyncHealth.DTOs.Common;
using PatientSyncHealth.DTOs.Nurses;

namespace PatientSyncHealth.Services.Interfaces;

public interface INurseService
{
    Task<NurseDto> GetByIdAsync(string externalId);
    Task<PagedResult<NurseListDto>> GetNursesAsync(NurseSearchParameters parameters);
    Task<NurseDto> CreateNurseAsync(CreateNurseDto dto);
    Task<NurseDto> UpdateNurseAsync(string externalId, UpdateNurseDto dto);
    Task DeactivateNurseAsync(string externalId);
    Task ReactivateNurseAsync(string externalId);
}
