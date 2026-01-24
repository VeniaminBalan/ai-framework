using Microsoft.EntityFrameworkCore;
using PatientSyncHealth.Domain.Aggregates.Examination;
using PatientSyncHealth.Domain.Interfaces;
using PatientSyncHealth.DTOs.Common;
using PatientSyncHealth.DTOs.Examinations;
using PatientSyncHealth.Infrastructure.Data;

namespace PatientSyncHealth.Infrastructure.Repositories;

public class ExaminationRepository : IExaminationRepository
{
    private readonly AppDbContext _context;

    public ExaminationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Examination?> GetByIdAsync(string externalId)
    {
        return await _context.Examinations
            .FirstOrDefaultAsync(e => e.ExternalId == externalId);
    }

    public async Task<PagedResult<ExaminationListDto>> GetPagedAsync(ExaminationSearchParameters parameters)
    {
        var query = _context.Examinations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.PatientId))
        {
            query = query.Where(e => e.PatientId == parameters.PatientId);
        }

        if (!string.IsNullOrWhiteSpace(parameters.DoctorId))
        {
            query = query.Where(e => e.DoctorId == parameters.DoctorId);
        }

        if (parameters.IsCompleted.HasValue)
        {
            query = query.Where(e => e.IsCompleted == parameters.IsCompleted.Value);
        }

        if (parameters.FromDate.HasValue)
        {
            query = query.Where(e => e.ExaminationDate >= parameters.FromDate.Value.Date);
        }

        if (parameters.ToDate.HasValue)
        {
            query = query.Where(e => e.ExaminationDate <= parameters.ToDate.Value.Date);
        }

        query = parameters.SortBy?.ToLower() switch
        {
            "patientid" => parameters.SortDescending
                ? query.OrderByDescending(e => e.PatientId)
                : query.OrderBy(e => e.PatientId),
            "doctorid" => parameters.SortDescending
                ? query.OrderByDescending(e => e.DoctorId)
                : query.OrderBy(e => e.DoctorId),
            _ => parameters.SortDescending
                ? query.OrderByDescending(e => e.ExaminationDate)
                : query.OrderBy(e => e.ExaminationDate)
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(e => new ExaminationListDto
            {
                Id = e.ExternalId,
                PatientId = e.PatientId,
                DoctorId = e.DoctorId,
                ExaminationDate = e.ExaminationDate,
                Diagnosis = e.Diagnosis,
                IsCompleted = e.IsCompleted
            })
            .ToListAsync();

        return new PagedResult<ExaminationListDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize
        };
    }

    public async Task<List<ExaminationListDto>> GetByPatientIdAsync(string patientId)
    {
        return await _context.Examinations
            .Where(e => e.PatientId == patientId)
            .OrderByDescending(e => e.ExaminationDate)
            .Select(e => new ExaminationListDto
            {
                Id = e.ExternalId,
                PatientId = e.PatientId,
                DoctorId = e.DoctorId,
                ExaminationDate = e.ExaminationDate,
                Diagnosis = e.Diagnosis,
                IsCompleted = e.IsCompleted
            })
            .ToListAsync();
    }

    public async Task<List<ExaminationListDto>> GetByDoctorIdAsync(string doctorId)
    {
        return await _context.Examinations
            .Where(e => e.DoctorId == doctorId)
            .OrderByDescending(e => e.ExaminationDate)
            .Select(e => new ExaminationListDto
            {
                Id = e.ExternalId,
                PatientId = e.PatientId,
                DoctorId = e.DoctorId,
                ExaminationDate = e.ExaminationDate,
                Diagnosis = e.Diagnosis,
                IsCompleted = e.IsCompleted
            })
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(string externalId)
    {
        return await _context.Examinations.AnyAsync(e => e.ExternalId == externalId);
    }

    public async Task AddAsync(Examination examination)
    {
        await _context.Examinations.AddAsync(examination);
    }

    public void Update(Examination examination)
    {
        _context.Examinations.Update(examination);
    }

    public void Remove(Examination examination)
    {
        _context.Examinations.Remove(examination);
    }
}
