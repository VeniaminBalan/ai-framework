using PatientSyncHealth.Domain.Aggregates.Appointment;
using PatientSyncHealth.DTOs.Appointments;
using PatientSyncHealth.DTOs.Common;

namespace PatientSyncHealth.Domain.Interfaces;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(string externalId);
    Task<PagedResult<AppointmentListDto>> GetPagedAsync(AppointmentSearchParameters parameters);
    Task<List<AppointmentListDto>> GetDoctorCalendarAsync(string doctorId, DateTime fromDate, DateTime toDate);
    Task<bool> ExistsAsync(string externalId);
    Task<bool> HasConflictAsync(string doctorId, DateTime startTime, DateTime endTime, string? excludeAppointmentId = null);
    Task<bool> HasPendingAppointmentsForDoctorAsync(string doctorId);
    Task AddAsync(Appointment appointment);
    void Update(Appointment appointment);
    void Remove(Appointment appointment);
}
