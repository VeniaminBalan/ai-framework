using PatientSyncHealth.Domain.Aggregates.Patient;
using PatientSyncHealth.Domain.ValueObjects;
using PatientSyncHealth.DTOs.Common;
using PatientSyncHealth.DTOs.Patients;

namespace PatientSyncHealth.Mappings;

public static class PatientMappingExtensions
{
    public static PatientDto ToDto(this Patient patient)
    {
        ArgumentNullException.ThrowIfNull(patient);

        return new PatientDto
        {
            Id = patient.ExternalId,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            FullName = patient.FullName,
            IdentificationNumber = patient.IdentificationNumber.ToDto(),
            DateOfBirth = patient.DateOfBirth,
            Age = patient.Age,
            Gender = patient.Gender,
            Email = patient.Email?.Value,
            Phone = patient.Phone?.Value,
            Address = patient.Address?.ToDto(),
            ExaminationFrequency = patient.ExaminationFrequency,
            LastExaminationDate = patient.LastExaminationDate,
            NextExaminationDate = patient.NextExaminationDate,
            IsOverdueForExamination = patient.IsOverdueForExamination,
            IsActive = patient.IsActive,
            CreatedAt = patient.CreatedAt,
            CreatedBy = patient.CreatedBy,
            UpdatedAt = patient.UpdatedAt,
            UpdatedBy = patient.UpdatedBy
        };
    }

    public static PatientListDto ToListDto(this Patient patient)
    {
        ArgumentNullException.ThrowIfNull(patient);

        return new PatientListDto
        {
            Id = patient.ExternalId,
            FullName = patient.FullName,
            IdentificationNumber = patient.IdentificationNumber.ToDto(),
            Age = patient.Age,
            Phone = patient.Phone?.Value,
            NextExaminationDate = patient.NextExaminationDate,
            IsOverdue = patient.IsOverdueForExamination,
            IsActive = patient.IsActive
        };
    }

    public static IdentificationNumberDto ToDto(this PersonalIdentificationNumber pin)
    {
        ArgumentNullException.ThrowIfNull(pin);

        return new IdentificationNumberDto
        {
            Value = pin.Value,
            Type = pin.Type
        };
    }

    public static PersonalIdentificationNumber ToValueObject(this IdentificationNumberDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return PersonalIdentificationNumber.Create(dto.Value, dto.Type);
    }

    public static AddressDto? ToDto(this Address? address)
    {
        if (address == null)
            return null;

        return new AddressDto
        {
            Street = address.Street,
            City = address.City,
            County = address.County,
            PostalCode = address.PostalCode,
            Country = address.Country
        };
    }

    public static Address? ToValueObject(this AddressDto? dto)
    {
        if (dto == null)
            return null;

        return new Address(
            dto.Street,
            dto.City,
            dto.County,
            dto.PostalCode,
            dto.Country);
    }
}
