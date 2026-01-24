using FluentValidation;
using PatientSyncHealth.Domain.Aggregates.Appointment;
using PatientSyncHealth.Domain.Interfaces;
using PatientSyncHealth.DTOs.Appointments;
using PatientSyncHealth.DTOs.Common;
using PatientSyncHealth.Infrastructure.Data;
using PatientSyncHealth.Mappings;
using PatientSyncHealth.Middleware.Exceptions;
using PatientSyncHealth.Services.Interfaces;

namespace PatientSyncHealth.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AppointmentService> _logger;
    private readonly IValidator<ScheduleAppointmentDto> _scheduleValidator;
    private readonly IValidator<RescheduleAppointmentDto> _rescheduleValidator;

    public AppointmentService(
        IAppointmentRepository appointmentRepository,
        IUnitOfWork unitOfWork,
        ILogger<AppointmentService> logger,
        IValidator<ScheduleAppointmentDto> scheduleValidator,
        IValidator<RescheduleAppointmentDto> rescheduleValidator)
    {
        _appointmentRepository = appointmentRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _scheduleValidator = scheduleValidator;
        _rescheduleValidator = rescheduleValidator;
    }

    public async Task<AppointmentDto> GetByIdAsync(string externalId)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(externalId);

        if (appointment == null)
        {
            _logger.LogWarning("Appointment with ID {AppointmentId} not found", externalId);
            throw new NotFoundException("Appointment", externalId);
        }

        return appointment.ToDto();
    }

    public async Task<PagedResult<AppointmentListDto>> GetAppointmentsAsync(AppointmentSearchParameters parameters)
    {
        return await _appointmentRepository.GetPagedAsync(parameters);
    }

    public async Task<List<AppointmentListDto>> GetDoctorCalendarAsync(string doctorId, DateTime fromDate, DateTime toDate)
    {
        return await _appointmentRepository.GetDoctorCalendarAsync(doctorId, fromDate, toDate);
    }

    public async Task<AppointmentDto> ScheduleAppointmentAsync(ScheduleAppointmentDto dto)
    {
        _logger.LogInformation("Scheduling appointment for patient {PatientId} with doctor {DoctorId} at {DateTime}",
            dto.PatientId, dto.DoctorId, dto.ScheduledDateTime);

        var validationResult = await _scheduleValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for appointment scheduling: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            throw new ValidationException(validationResult.Errors);
        }

        var appointment = Appointment.Schedule(
            dto.PatientId,
            dto.DoctorId,
            dto.ScheduledDateTime,
            TimeSpan.FromMinutes(dto.DurationMinutes),
            dto.Purpose,
            dto.ScheduledByNurseId,
            dto.Notes);

        await _appointmentRepository.AddAsync(appointment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Appointment scheduled with ID {AppointmentId}", appointment.ExternalId);

        return appointment.ToDto();
    }

    public async Task<AppointmentDto> RescheduleAppointmentAsync(string externalId, RescheduleAppointmentDto dto)
    {
        _logger.LogInformation("Rescheduling appointment {AppointmentId} to {DateTime}", externalId, dto.ScheduledDateTime);

        var appointment = await _appointmentRepository.GetByIdAsync(externalId);
        if (appointment == null)
        {
            _logger.LogWarning("Appointment with ID {AppointmentId} not found", externalId);
            throw new NotFoundException("Appointment", externalId);
        }

        var validationResult = await _rescheduleValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for appointment rescheduling: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            throw new ValidationException(validationResult.Errors);
        }

        // Check for conflicts at the new time
        var newDuration = dto.DurationMinutes.HasValue
            ? TimeSpan.FromMinutes(dto.DurationMinutes.Value)
            : appointment.Duration;
        var endTime = dto.ScheduledDateTime.Add(newDuration);

        var hasConflict = await _appointmentRepository.HasConflictAsync(
            appointment.DoctorId,
            dto.ScheduledDateTime,
            endTime,
            externalId);

        if (hasConflict)
        {
            throw new BusinessException("There is already an appointment scheduled for this doctor at the specified time");
        }

        appointment.Reschedule(dto.ScheduledDateTime, dto.DurationMinutes.HasValue ? newDuration : null);

        _appointmentRepository.Update(appointment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Appointment {AppointmentId} rescheduled successfully", externalId);

        return appointment.ToDto();
    }

    public async Task<AppointmentDto> ConfirmAppointmentAsync(string externalId)
    {
        _logger.LogInformation("Confirming appointment {AppointmentId}", externalId);

        var appointment = await _appointmentRepository.GetByIdAsync(externalId);
        if (appointment == null)
        {
            _logger.LogWarning("Appointment with ID {AppointmentId} not found", externalId);
            throw new NotFoundException("Appointment", externalId);
        }

        appointment.Confirm();

        _appointmentRepository.Update(appointment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Appointment {AppointmentId} confirmed successfully", externalId);

        return appointment.ToDto();
    }

    public async Task<AppointmentDto> StartAppointmentAsync(string externalId)
    {
        _logger.LogInformation("Starting appointment {AppointmentId}", externalId);

        var appointment = await _appointmentRepository.GetByIdAsync(externalId);
        if (appointment == null)
        {
            _logger.LogWarning("Appointment with ID {AppointmentId} not found", externalId);
            throw new NotFoundException("Appointment", externalId);
        }

        appointment.Start();

        _appointmentRepository.Update(appointment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Appointment {AppointmentId} started successfully", externalId);

        return appointment.ToDto();
    }

    public async Task<AppointmentDto> CompleteAppointmentAsync(string externalId, CompleteAppointmentDto dto)
    {
        _logger.LogInformation("Completing appointment {AppointmentId}", externalId);

        var appointment = await _appointmentRepository.GetByIdAsync(externalId);
        if (appointment == null)
        {
            _logger.LogWarning("Appointment with ID {AppointmentId} not found", externalId);
            throw new NotFoundException("Appointment", externalId);
        }

        appointment.Complete(dto.ResultingExaminationId);

        _appointmentRepository.Update(appointment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Appointment {AppointmentId} completed successfully", externalId);

        return appointment.ToDto();
    }

    public async Task<AppointmentDto> CancelAppointmentAsync(string externalId, CancelAppointmentDto dto)
    {
        _logger.LogInformation("Cancelling appointment {AppointmentId}", externalId);

        var appointment = await _appointmentRepository.GetByIdAsync(externalId);
        if (appointment == null)
        {
            _logger.LogWarning("Appointment with ID {AppointmentId} not found", externalId);
            throw new NotFoundException("Appointment", externalId);
        }

        appointment.Cancel(dto.Reason);

        _appointmentRepository.Update(appointment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Appointment {AppointmentId} cancelled successfully", externalId);

        return appointment.ToDto();
    }
}
