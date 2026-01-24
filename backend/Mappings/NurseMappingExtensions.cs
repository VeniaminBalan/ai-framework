using PatientSyncHealth.Domain.Aggregates.Nurse;
using PatientSyncHealth.DTOs.Nurses;

namespace PatientSyncHealth.Mappings;

public static class NurseMappingExtensions
{
    public static NurseDto ToDto(this Nurse nurse)
    {
        ArgumentNullException.ThrowIfNull(nurse);

        return new NurseDto
        {
            Id = nurse.ExternalId,
            KeycloakUserId = nurse.KeycloakUserId,
            FirstName = nurse.FirstName,
            LastName = nurse.LastName,
            FullName = nurse.FullName,
            DepartmentName = nurse.Department.Name,
            DepartmentCode = nurse.Department.Code,
            Email = nurse.Email?.Value,
            Phone = nurse.Phone?.Value,
            IsActive = nurse.IsActive,
            CreatedAt = nurse.CreatedAt,
            CreatedBy = nurse.CreatedBy,
            UpdatedAt = nurse.UpdatedAt,
            UpdatedBy = nurse.UpdatedBy
        };
    }

    public static NurseListDto ToListDto(this Nurse nurse)
    {
        ArgumentNullException.ThrowIfNull(nurse);

        return new NurseListDto
        {
            Id = nurse.ExternalId,
            FullName = nurse.FullName,
            DepartmentName = nurse.Department.Name,
            DepartmentCode = nurse.Department.Code,
            Phone = nurse.Phone?.Value,
            IsActive = nurse.IsActive
        };
    }
}
