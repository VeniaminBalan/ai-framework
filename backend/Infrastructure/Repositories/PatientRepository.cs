using Microsoft.EntityFrameworkCore;
using PatientSyncHealth.Domain.Aggregates.Patient;
using PatientSyncHealth.Domain.Interfaces;
using PatientSyncHealth.DTOs.Common;
using PatientSyncHealth.DTOs.Patients;
using PatientSyncHealth.Infrastructure.Data;

namespace PatientSyncHealth.Infrastructure.Repositories;

public class PatientRepository : IPatientRepository
{
    private readonly AppDbContext _context;

    public PatientRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Patient?> GetByIdAsync(string externalId)
    {
        return await _context.Patients
            .FirstOrDefaultAsync(p => p.ExternalId == externalId);
    }

    public async Task<Patient?> GetByCnpAsync(string cnp)
    {
        return await _context.Patients
            .FirstOrDefaultAsync(p => p.Cnp.Value == cnp);
    }

    public async Task<PagedResult<PatientListDto>> GetPagedAsync(PatientSearchParameters parameters)
    {
        var query = _context.Patients.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm.ToLower();
            query = query.Where(p =>
                p.FirstName.ToLower().Contains(searchTerm) ||
                p.LastName.ToLower().Contains(searchTerm) ||
                p.Cnp.Value.Contains(searchTerm));
        }

        if (parameters.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == parameters.IsActive.Value);
        }

        var today = DateTime.Today;

        if (parameters.IsOverdue.HasValue && parameters.IsOverdue.Value)
        {
            query = query.Where(p =>
                p.IsActive &&
                p.NextExaminationDate.HasValue &&
                p.NextExaminationDate.Value < today);
        }

        // Apply sorting
        query = parameters.SortBy?.ToLower() switch
        {
            "firstname" => parameters.SortDescending
                ? query.OrderByDescending(p => p.FirstName)
                : query.OrderBy(p => p.FirstName),
            "cnp" => parameters.SortDescending
                ? query.OrderByDescending(p => p.Cnp.Value)
                : query.OrderBy(p => p.Cnp.Value),
            "nextexaminationdate" => parameters.SortDescending
                ? query.OrderByDescending(p => p.NextExaminationDate)
                : query.OrderBy(p => p.NextExaminationDate),
            "dateofbirth" => parameters.SortDescending
                ? query.OrderByDescending(p => p.DateOfBirth)
                : query.OrderBy(p => p.DateOfBirth),
            _ => parameters.SortDescending
                ? query.OrderByDescending(p => p.LastName)
                : query.OrderBy(p => p.LastName)
        };

        var totalCount = await query.CountAsync();

        // Project to DTO with explicit Select
        var items = await query
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(p => new PatientListDto
            {
                Id = p.ExternalId,
                FullName = p.FirstName + " " + p.LastName,
                Cnp = p.Cnp.Value,
                Age = today.Year - p.DateOfBirth.Year -
                    (p.DateOfBirth.Date > today.AddYears(-(today.Year - p.DateOfBirth.Year)) ? 1 : 0),
                Phone = p.Phone != null ? p.Phone.Value : null,
                NextExaminationDate = p.NextExaminationDate,
                IsOverdue = p.IsActive &&
                    p.NextExaminationDate.HasValue &&
                    p.NextExaminationDate.Value < today,
                IsActive = p.IsActive
            })
            .ToListAsync();

        return new PagedResult<PatientListDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize
        };
    }

    public async Task<List<PatientListDto>> GetPatientsWithOverdueExaminationsAsync()
    {
        var today = DateTime.Today;

        return await _context.Patients
            .Where(p =>
                p.IsActive &&
                p.NextExaminationDate.HasValue &&
                p.NextExaminationDate.Value < today)
            .OrderBy(p => p.NextExaminationDate)
            .Select(p => new PatientListDto
            {
                Id = p.ExternalId,
                FullName = p.FirstName + " " + p.LastName,
                Cnp = p.Cnp.Value,
                Age = today.Year - p.DateOfBirth.Year -
                    (p.DateOfBirth.Date > today.AddYears(-(today.Year - p.DateOfBirth.Year)) ? 1 : 0),
                Phone = p.Phone != null ? p.Phone.Value : null,
                NextExaminationDate = p.NextExaminationDate,
                IsOverdue = true,
                IsActive = p.IsActive
            })
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(string externalId)
    {
        return await _context.Patients
            .AnyAsync(p => p.ExternalId == externalId);
    }

    public async Task<bool> ExistsByCnpAsync(string cnp)
    {
        return await _context.Patients
            .AnyAsync(p => p.Cnp.Value == cnp);
    }

    public async Task AddAsync(Patient patient)
    {
        await _context.Patients.AddAsync(patient);
    }

    public void Update(Patient patient)
    {
        _context.Patients.Update(patient);
    }

    public void Remove(Patient patient)
    {
        _context.Patients.Remove(patient);
    }
}
