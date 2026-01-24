using Microsoft.EntityFrameworkCore;
using PatientSyncHealth.Domain.Aggregates.Doctor;
using PatientSyncHealth.Domain.Interfaces;
using PatientSyncHealth.DTOs.Common;
using PatientSyncHealth.DTOs.Doctors;
using PatientSyncHealth.Infrastructure.Data;

namespace PatientSyncHealth.Infrastructure.Repositories;

public class DoctorRepository : IDoctorRepository
{
    private readonly AppDbContext _context;

    public DoctorRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Doctor?> GetByIdAsync(string externalId)
    {
        return await _context.Doctors
            .FirstOrDefaultAsync(d => d.ExternalId == externalId);
    }

    public async Task<Doctor?> GetByLicenseNumberAsync(string licenseNumber)
    {
        return await _context.Doctors
            .FirstOrDefaultAsync(d => d.LicenseNumber.Value == licenseNumber);
    }

    public async Task<PagedResult<DoctorListDto>> GetPagedAsync(DoctorSearchParameters parameters)
    {
        var query = _context.Doctors.AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm.ToLower();
            query = query.Where(d =>
                d.FirstName.ToLower().Contains(searchTerm) ||
                d.LastName.ToLower().Contains(searchTerm) ||
                d.LicenseNumber.Value.ToLower().Contains(searchTerm));
        }

        if (parameters.IsActive.HasValue)
        {
            query = query.Where(d => d.IsActive == parameters.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Specialization))
        {
            var specialization = parameters.Specialization.ToLower();
            query = query.Where(d => d.Specialization.Value.ToLower().Contains(specialization));
        }

        query = parameters.SortBy?.ToLower() switch
        {
            "firstname" => parameters.SortDescending
                ? query.OrderByDescending(d => d.FirstName)
                : query.OrderBy(d => d.FirstName),
            "specialization" => parameters.SortDescending
                ? query.OrderByDescending(d => d.Specialization.Value)
                : query.OrderBy(d => d.Specialization.Value),
            "licensenumber" => parameters.SortDescending
                ? query.OrderByDescending(d => d.LicenseNumber.Value)
                : query.OrderBy(d => d.LicenseNumber.Value),
            _ => parameters.SortDescending
                ? query.OrderByDescending(d => d.LastName)
                : query.OrderBy(d => d.LastName)
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(d => new DoctorListDto
            {
                Id = d.ExternalId,
                FullName = d.FirstName + " " + d.LastName,
                Specialization = d.Specialization.Value,
                LicenseNumber = d.LicenseNumber.Value,
                Phone = d.Phone != null ? d.Phone.Value : null,
                IsActive = d.IsActive
            })
            .ToListAsync();

        return new PagedResult<DoctorListDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize
        };
    }

    public async Task<bool> ExistsAsync(string externalId)
    {
        return await _context.Doctors.AnyAsync(d => d.ExternalId == externalId);
    }

    public async Task<bool> ExistsByLicenseNumberAsync(string licenseNumber)
    {
        return await _context.Doctors.AnyAsync(d => d.LicenseNumber.Value == licenseNumber);
    }

    public async Task<bool> IsActiveAsync(string externalId)
    {
        return await _context.Doctors.AnyAsync(d => d.ExternalId == externalId && d.IsActive);
    }

    public async Task AddAsync(Doctor doctor)
    {
        await _context.Doctors.AddAsync(doctor);
    }

    public void Update(Doctor doctor)
    {
        _context.Doctors.Update(doctor);
    }

    public void Remove(Doctor doctor)
    {
        _context.Doctors.Remove(doctor);
    }
}
