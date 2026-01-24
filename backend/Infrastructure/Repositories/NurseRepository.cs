using Microsoft.EntityFrameworkCore;
using PatientSyncHealth.Domain.Aggregates.Nurse;
using PatientSyncHealth.Domain.Interfaces;
using PatientSyncHealth.DTOs.Common;
using PatientSyncHealth.DTOs.Nurses;
using PatientSyncHealth.Infrastructure.Data;

namespace PatientSyncHealth.Infrastructure.Repositories;

public class NurseRepository : INurseRepository
{
    private readonly AppDbContext _context;

    public NurseRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Nurse?> GetByIdAsync(string externalId)
    {
        return await _context.Nurses
            .FirstOrDefaultAsync(n => n.ExternalId == externalId);
    }

    public async Task<PagedResult<NurseListDto>> GetPagedAsync(NurseSearchParameters parameters)
    {
        var query = _context.Nurses.AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm.ToLower();
            query = query.Where(n =>
                n.FirstName.ToLower().Contains(searchTerm) ||
                n.LastName.ToLower().Contains(searchTerm));
        }

        if (parameters.IsActive.HasValue)
        {
            query = query.Where(n => n.IsActive == parameters.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Department))
        {
            var department = parameters.Department.ToLower();
            query = query.Where(n => n.Department.Name.ToLower().Contains(department));
        }

        query = parameters.SortBy?.ToLower() switch
        {
            "firstname" => parameters.SortDescending
                ? query.OrderByDescending(n => n.FirstName)
                : query.OrderBy(n => n.FirstName),
            "department" => parameters.SortDescending
                ? query.OrderByDescending(n => n.Department.Name)
                : query.OrderBy(n => n.Department.Name),
            _ => parameters.SortDescending
                ? query.OrderByDescending(n => n.LastName)
                : query.OrderBy(n => n.LastName)
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(n => new NurseListDto
            {
                Id = n.ExternalId,
                FullName = n.FirstName + " " + n.LastName,
                DepartmentName = n.Department.Name,
                DepartmentCode = n.Department.Code,
                Phone = n.Phone != null ? n.Phone.Value : null,
                IsActive = n.IsActive
            })
            .ToListAsync();

        return new PagedResult<NurseListDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize
        };
    }

    public async Task<bool> ExistsAsync(string externalId)
    {
        return await _context.Nurses.AnyAsync(n => n.ExternalId == externalId);
    }

    public async Task<bool> IsActiveAsync(string externalId)
    {
        return await _context.Nurses.AnyAsync(n => n.ExternalId == externalId && n.IsActive);
    }

    public async Task AddAsync(Nurse nurse)
    {
        await _context.Nurses.AddAsync(nurse);
    }

    public void Update(Nurse nurse)
    {
        _context.Nurses.Update(nurse);
    }

    public void Remove(Nurse nurse)
    {
        _context.Nurses.Remove(nurse);
    }
}
