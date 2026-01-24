using Microsoft.EntityFrameworkCore;
using PatientSyncHealth.Domain.Aggregates.Patient;
using PatientSyncHealth.Domain.Enums;
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

    public async Task<Patient?> GetByIdentificationNumberAsync(string identificationNumber)
    {
        return await _context.Patients
            .FirstOrDefaultAsync(p => EF.Property<string>(p, "_identificationNumberValue") == identificationNumber);
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
                EF.Property<string>(p, "_identificationNumberValue").Contains(searchTerm));
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
            "identificationnumber" => parameters.SortDescending
                ? query.OrderByDescending(p => EF.Property<string>(p, "_identificationNumberValue"))
                : query.OrderBy(p => EF.Property<string>(p, "_identificationNumberValue")),
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
                IdentificationNumber = EF.Property<string>(p, "_identificationNumberValue"),
                IdentificationNumberType = EF.Property<IdentificationNumberType>(p, "_identificationNumberType"),
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
                IdentificationNumber = EF.Property<string>(p, "_identificationNumberValue"),
                IdentificationNumberType = EF.Property<IdentificationNumberType>(p, "_identificationNumberType"),
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

    public async Task<bool> ExistsByIdentificationNumberAsync(string identificationNumber)
    {
        return await _context.Patients
            .AnyAsync(p => EF.Property<string>(p, "_identificationNumberValue") == identificationNumber);
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
