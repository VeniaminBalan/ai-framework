using PatientSyncHealth.Domain.Aggregates.Patient;
using PatientSyncHealth.DTOs.Common;
using PatientSyncHealth.DTOs.Patients;

namespace PatientSyncHealth.Domain.Interfaces;

public interface IPatientRepository
{
    // Single entity queries (may return tracked entity for updates)
    Task<Patient?> GetByIdAsync(string externalId);
    Task<Patient?> GetByIdentificationNumberAsync(string identificationNumber);

    // Collection queries (use explicit projection to DTOs)
    Task<PagedResult<PatientListDto>> GetPagedAsync(PatientSearchParameters parameters);
    Task<List<PatientListDto>> GetPatientsWithOverdueExaminationsAsync();

    // Existence checks
    Task<bool> ExistsAsync(string externalId);
    Task<bool> ExistsByIdentificationNumberAsync(string identificationNumber);

    // Commands
    Task AddAsync(Patient patient);
    void Update(Patient patient);
    void Remove(Patient patient);
}
