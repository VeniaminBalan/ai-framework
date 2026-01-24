using Microsoft.EntityFrameworkCore;
using PatientSyncHealth.Domain.Aggregates.Appointment;
using PatientSyncHealth.Domain.Enums;
using PatientSyncHealth.Domain.Interfaces;
using PatientSyncHealth.DTOs.Appointments;
using PatientSyncHealth.DTOs.Common;
using PatientSyncHealth.Infrastructure.Data;

namespace PatientSyncHealth.Infrastructure.Repositories;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly AppDbContext _context;

    public AppointmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Appointment?> GetByIdAsync(string externalId)
    {
        return await _context.Appointments
            .FirstOrDefaultAsync(a => a.ExternalId == externalId);
    }

    public async Task<PagedResult<AppointmentListDto>> GetPagedAsync(AppointmentSearchParameters parameters)
    {
        var query = _context.Appointments.AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.PatientId))
        {
            query = query.Where(a => a.PatientId == parameters.PatientId);
        }

        if (!string.IsNullOrWhiteSpace(parameters.DoctorId))
        {
            query = query.Where(a => a.DoctorId == parameters.DoctorId);
        }

        if (parameters.Status.HasValue)
        {
            query = query.Where(a => a.Status == parameters.Status.Value);
        }

        if (parameters.Purpose.HasValue)
        {
            query = query.Where(a => a.Purpose == parameters.Purpose.Value);
        }

        if (parameters.FromDate.HasValue)
        {
            query = query.Where(a => a.ScheduledDateTime >= parameters.FromDate.Value);
        }

        if (parameters.ToDate.HasValue)
        {
            query = query.Where(a => a.ScheduledDateTime <= parameters.ToDate.Value);
        }

        query = parameters.SortBy?.ToLower() switch
        {
            "patientid" => parameters.SortDescending
                ? query.OrderByDescending(a => a.PatientId)
                : query.OrderBy(a => a.PatientId),
            "doctorid" => parameters.SortDescending
                ? query.OrderByDescending(a => a.DoctorId)
                : query.OrderBy(a => a.DoctorId),
            "status" => parameters.SortDescending
                ? query.OrderByDescending(a => a.Status)
                : query.OrderBy(a => a.Status),
            _ => parameters.SortDescending
                ? query.OrderByDescending(a => a.ScheduledDateTime)
                : query.OrderBy(a => a.ScheduledDateTime)
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(a => new AppointmentListDto
            {
                Id = a.ExternalId,
                PatientId = a.PatientId,
                DoctorId = a.DoctorId,
                ScheduledDateTime = a.ScheduledDateTime,
                Duration = a.Duration,
                Purpose = a.Purpose,
                Status = a.Status
            })
            .ToListAsync();

        return new PagedResult<AppointmentListDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize
        };
    }

    public async Task<List<AppointmentListDto>> GetDoctorCalendarAsync(string doctorId, DateTime fromDate, DateTime toDate)
    {
        return await _context.Appointments
            .Where(a => a.DoctorId == doctorId &&
                        a.ScheduledDateTime >= fromDate &&
                        a.ScheduledDateTime <= toDate &&
                        a.Status != AppointmentStatus.Cancelled)
            .OrderBy(a => a.ScheduledDateTime)
            .Select(a => new AppointmentListDto
            {
                Id = a.ExternalId,
                PatientId = a.PatientId,
                DoctorId = a.DoctorId,
                ScheduledDateTime = a.ScheduledDateTime,
                Duration = a.Duration,
                Purpose = a.Purpose,
                Status = a.Status
            })
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(string externalId)
    {
        return await _context.Appointments.AnyAsync(a => a.ExternalId == externalId);
    }

    public async Task<bool> HasConflictAsync(string doctorId, DateTime startTime, DateTime endTime, string? excludeAppointmentId = null)
    {
        var query = _context.Appointments
            .Where(a => a.DoctorId == doctorId &&
                        a.Status != AppointmentStatus.Cancelled &&
                        a.Status != AppointmentStatus.NoShow);

        if (!string.IsNullOrWhiteSpace(excludeAppointmentId))
        {
            query = query.Where(a => a.ExternalId != excludeAppointmentId);
        }

        // Check for overlapping appointments
        // An overlap exists if: existing.Start < new.End AND existing.End > new.Start
        return await query.AnyAsync(a =>
            a.ScheduledDateTime < endTime &&
            a.ScheduledDateTime.Add(a.Duration) > startTime);
    }

    public async Task<bool> HasPendingAppointmentsForDoctorAsync(string doctorId)
    {
        return await _context.Appointments
            .AnyAsync(a => a.DoctorId == doctorId &&
                          (a.Status == AppointmentStatus.Scheduled ||
                           a.Status == AppointmentStatus.Confirmed ||
                           a.Status == AppointmentStatus.InProgress));
    }

    public async Task AddAsync(Appointment appointment)
    {
        await _context.Appointments.AddAsync(appointment);
    }

    public void Update(Appointment appointment)
    {
        _context.Appointments.Update(appointment);
    }

    public void Remove(Appointment appointment)
    {
        _context.Appointments.Remove(appointment);
    }
}
