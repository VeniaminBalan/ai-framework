using FluentValidation;
using PatientSyncHealth.Domain.Aggregates.Doctor;
using PatientSyncHealth.Domain.Interfaces;
using PatientSyncHealth.Domain.ValueObjects;
using PatientSyncHealth.DTOs.Common;
using PatientSyncHealth.DTOs.Doctors;
using PatientSyncHealth.Infrastructure.Data;
using PatientSyncHealth.Mappings;
using PatientSyncHealth.Middleware.Exceptions;
using PatientSyncHealth.Services.Interfaces;

namespace PatientSyncHealth.Services;

public class DoctorService : IDoctorService
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DoctorService> _logger;
    private readonly IValidator<CreateDoctorDto> _createValidator;
    private readonly IValidator<UpdateDoctorDto> _updateValidator;

    public DoctorService(
        IDoctorRepository doctorRepository,
        IUnitOfWork unitOfWork,
        ILogger<DoctorService> logger,
        IValidator<CreateDoctorDto> createValidator,
        IValidator<UpdateDoctorDto> updateValidator)
    {
        _doctorRepository = doctorRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<DoctorDto> GetByIdAsync(string externalId)
    {
        var doctor = await _doctorRepository.GetByIdAsync(externalId);

        if (doctor == null)
        {
            _logger.LogWarning("Doctor with ID {DoctorId} not found", externalId);
            throw new NotFoundException("Doctor", externalId);
        }

        return doctor.ToDto();
    }

    public async Task<PagedResult<DoctorListDto>> GetDoctorsAsync(DoctorSearchParameters parameters)
    {
        return await _doctorRepository.GetPagedAsync(parameters);
    }

    public async Task<DoctorDto> CreateDoctorAsync(CreateDoctorDto dto)
    {
        _logger.LogInformation("Creating doctor with license number {LicenseNumber}", dto.LicenseNumber);

        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for doctor creation: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            throw new ValidationException(validationResult.Errors);
        }

        var specialization = new Specialization(dto.Specialization);
        var licenseNumber = new LicenseNumber(dto.LicenseNumber);
        var email = !string.IsNullOrWhiteSpace(dto.Email) ? new Email(dto.Email) : null;
        var phone = !string.IsNullOrWhiteSpace(dto.Phone) ? new PhoneNumber(dto.Phone) : null;

        var doctor = Doctor.Create(
            dto.FirstName,
            dto.LastName,
            specialization,
            licenseNumber,
            dto.KeycloakUserId,
            email,
            phone);

        await _doctorRepository.AddAsync(doctor);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Doctor created with ID {DoctorId}", doctor.ExternalId);

        return doctor.ToDto();
    }

    public async Task<DoctorDto> UpdateDoctorAsync(string externalId, UpdateDoctorDto dto)
    {
        _logger.LogInformation("Updating doctor {DoctorId}", externalId);

        var doctor = await _doctorRepository.GetByIdAsync(externalId);
        if (doctor == null)
        {
            _logger.LogWarning("Doctor with ID {DoctorId} not found for update", externalId);
            throw new NotFoundException("Doctor", externalId);
        }

        var validationResult = await _updateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for doctor update: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            throw new ValidationException(validationResult.Errors);
        }

        doctor.UpdatePersonalInfo(dto.FirstName, dto.LastName);
        doctor.UpdateSpecialization(new Specialization(dto.Specialization));

        var email = !string.IsNullOrWhiteSpace(dto.Email) ? new Email(dto.Email) : null;
        var phone = !string.IsNullOrWhiteSpace(dto.Phone) ? new PhoneNumber(dto.Phone) : null;
        doctor.UpdateContactInfo(email, phone);

        if (!string.IsNullOrWhiteSpace(dto.KeycloakUserId))
        {
            doctor.LinkToKeycloakUser(dto.KeycloakUserId);
        }

        _doctorRepository.Update(doctor);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Doctor {DoctorId} updated successfully", externalId);

        return doctor.ToDto();
    }

    public async Task DeactivateDoctorAsync(string externalId)
    {
        _logger.LogInformation("Deactivating doctor {DoctorId}", externalId);

        var doctor = await _doctorRepository.GetByIdAsync(externalId);
        if (doctor == null)
        {
            _logger.LogWarning("Doctor with ID {DoctorId} not found for deactivation", externalId);
            throw new NotFoundException("Doctor", externalId);
        }

        if (!doctor.IsActive)
        {
            throw new BusinessException("Doctor is already deactivated");
        }

        doctor.Deactivate();

        _doctorRepository.Update(doctor);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Doctor {DoctorId} deactivated successfully", externalId);
    }

    public async Task ReactivateDoctorAsync(string externalId)
    {
        _logger.LogInformation("Reactivating doctor {DoctorId}", externalId);

        var doctor = await _doctorRepository.GetByIdAsync(externalId);
        if (doctor == null)
        {
            _logger.LogWarning("Doctor with ID {DoctorId} not found for reactivation", externalId);
            throw new NotFoundException("Doctor", externalId);
        }

        if (doctor.IsActive)
        {
            throw new BusinessException("Doctor is already active");
        }

        doctor.Reactivate();

        _doctorRepository.Update(doctor);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Doctor {DoctorId} reactivated successfully", externalId);
    }
}
