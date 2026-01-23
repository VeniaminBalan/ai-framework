using PatientSyncHealth.DTOs.Common;
using PatientSyncHealth.DTOs.Patients;

namespace PatientSyncHealth.Services.Interfaces;

public interface IPatientService
{
    /// <summary>
    /// Gets a patient by their external ID.
    /// </summary>
    Task<PatientDto> GetByIdAsync(string externalId);

    /// <summary>
    /// Gets a paginated list of patients with optional filtering and sorting.
    /// </summary>
    Task<PagedResult<PatientListDto>> GetPatientsAsync(PatientSearchParameters parameters);

    /// <summary>
    /// Gets all patients with overdue examinations.
    /// </summary>
    Task<List<PatientListDto>> GetOverdueExaminationsAsync();

    /// <summary>
    /// Creates a new patient.
    /// </summary>
    Task<PatientDto> CreatePatientAsync(CreatePatientDto dto);

    /// <summary>
    /// Updates an existing patient.
    /// </summary>
    Task<PatientDto> UpdatePatientAsync(string externalId, UpdatePatientDto dto);

    /// <summary>
    /// Soft-deletes a patient by setting IsActive to false.
    /// </summary>
    Task DeactivatePatientAsync(string externalId);

    /// <summary>
    /// Reactivates a previously deactivated patient.
    /// </summary>
    Task ReactivatePatientAsync(string externalId);

    /// <summary>
    /// Records a completed examination and updates the next examination date.
    /// </summary>
    Task RecordExaminationAsync(string externalId, RecordExaminationDto dto);
}
