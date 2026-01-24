using PatientSyncHealth.Domain.Aggregates.Doctor;
using PatientSyncHealth.DTOs.Doctors;

namespace PatientSyncHealth.Mappings;

public static class DoctorMappingExtensions
{
    public static DoctorDto ToDto(this Doctor doctor)
    {
        ArgumentNullException.ThrowIfNull(doctor);

        return new DoctorDto
        {
            Id = doctor.ExternalId,
            KeycloakUserId = doctor.KeycloakUserId,
            FirstName = doctor.FirstName,
            LastName = doctor.LastName,
            FullName = doctor.FullName,
            Specialization = doctor.Specialization.Value,
            LicenseNumber = doctor.LicenseNumber.Value,
            Email = doctor.Email?.Value,
            Phone = doctor.Phone?.Value,
            IsActive = doctor.IsActive,
            CreatedAt = doctor.CreatedAt,
            CreatedBy = doctor.CreatedBy,
            UpdatedAt = doctor.UpdatedAt,
            UpdatedBy = doctor.UpdatedBy
        };
    }

    public static DoctorListDto ToListDto(this Doctor doctor)
    {
        ArgumentNullException.ThrowIfNull(doctor);

        return new DoctorListDto
        {
            Id = doctor.ExternalId,
            FullName = doctor.FullName,
            Specialization = doctor.Specialization.Value,
            LicenseNumber = doctor.LicenseNumber.Value,
            Phone = doctor.Phone?.Value,
            IsActive = doctor.IsActive
        };
    }
}
