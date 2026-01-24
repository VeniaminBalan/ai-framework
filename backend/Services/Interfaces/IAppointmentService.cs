using PatientSyncHealth.DTOs.Appointments;
using PatientSyncHealth.DTOs.Common;

namespace PatientSyncHealth.Services.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentDto> GetByIdAsync(string externalId);
    Task<PagedResult<AppointmentListDto>> GetAppointmentsAsync(AppointmentSearchParameters parameters);
    Task<List<AppointmentListDto>> GetDoctorCalendarAsync(string doctorId, DateTime fromDate, DateTime toDate);
    Task<AppointmentDto> ScheduleAppointmentAsync(ScheduleAppointmentDto dto);
    Task<AppointmentDto> RescheduleAppointmentAsync(string externalId, RescheduleAppointmentDto dto);
    Task<AppointmentDto> ConfirmAppointmentAsync(string externalId);
    Task<AppointmentDto> StartAppointmentAsync(string externalId);
    Task<AppointmentDto> CompleteAppointmentAsync(string externalId, CompleteAppointmentDto dto);
    Task<AppointmentDto> CancelAppointmentAsync(string externalId, CancelAppointmentDto dto);
}
