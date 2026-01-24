using PatientSyncHealth.Domain.Aggregates.Appointment;
using PatientSyncHealth.DTOs.Appointments;

namespace PatientSyncHealth.Mappings;

public static class AppointmentMappingExtensions
{
    public static AppointmentDto ToDto(this Appointment appointment)
    {
        ArgumentNullException.ThrowIfNull(appointment);

        return new AppointmentDto
        {
            Id = appointment.ExternalId,
            PatientId = appointment.PatientId,
            DoctorId = appointment.DoctorId,
            ScheduledByNurseId = appointment.ScheduledByNurseId,
            ScheduledDateTime = appointment.ScheduledDateTime,
            Duration = appointment.Duration,
            Purpose = appointment.Purpose,
            Status = appointment.Status,
            ResultingExaminationId = appointment.ResultingExaminationId,
            CancellationReason = appointment.CancellationReason,
            Notes = appointment.Notes,
            CreatedAt = appointment.CreatedAt,
            CreatedBy = appointment.CreatedBy,
            UpdatedAt = appointment.UpdatedAt,
            UpdatedBy = appointment.UpdatedBy
        };
    }

    public static AppointmentListDto ToListDto(this Appointment appointment)
    {
        ArgumentNullException.ThrowIfNull(appointment);

        return new AppointmentListDto
        {
            Id = appointment.ExternalId,
            PatientId = appointment.PatientId,
            DoctorId = appointment.DoctorId,
            ScheduledDateTime = appointment.ScheduledDateTime,
            Duration = appointment.Duration,
            Purpose = appointment.Purpose,
            Status = appointment.Status
        };
    }
}
