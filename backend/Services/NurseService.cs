using FluentValidation;
using PatientSyncHealth.Domain.Aggregates.Nurse;
using PatientSyncHealth.Domain.Interfaces;
using PatientSyncHealth.Domain.ValueObjects;
using PatientSyncHealth.DTOs.Common;
using PatientSyncHealth.DTOs.Nurses;
using PatientSyncHealth.Infrastructure.Data;
using PatientSyncHealth.Mappings;
using PatientSyncHealth.Middleware.Exceptions;
using PatientSyncHealth.Services.Interfaces;

namespace PatientSyncHealth.Services;

public class NurseService : INurseService
{
    private readonly INurseRepository _nurseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NurseService> _logger;
    private readonly IValidator<CreateNurseDto> _createValidator;
    private readonly IValidator<UpdateNurseDto> _updateValidator;

    public NurseService(
        INurseRepository nurseRepository,
        IUnitOfWork unitOfWork,
        ILogger<NurseService> logger,
        IValidator<CreateNurseDto> createValidator,
        IValidator<UpdateNurseDto> updateValidator)
    {
        _nurseRepository = nurseRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<NurseDto> GetByIdAsync(string externalId)
    {
        var nurse = await _nurseRepository.GetByIdAsync(externalId);

        if (nurse == null)
        {
            _logger.LogWarning("Nurse with ID {NurseId} not found", externalId);
            throw new NotFoundException("Nurse", externalId);
        }

        return nurse.ToDto();
    }

    public async Task<PagedResult<NurseListDto>> GetNursesAsync(NurseSearchParameters parameters)
    {
        return await _nurseRepository.GetPagedAsync(parameters);
    }

    public async Task<NurseDto> CreateNurseAsync(CreateNurseDto dto)
    {
        _logger.LogInformation("Creating nurse {FirstName} {LastName}", dto.FirstName, dto.LastName);

        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for nurse creation: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            throw new ValidationException(validationResult.Errors);
        }

        var department = new Department(dto.DepartmentName, dto.DepartmentCode);
        var email = !string.IsNullOrWhiteSpace(dto.Email) ? new Email(dto.Email) : null;
        var phone = !string.IsNullOrWhiteSpace(dto.Phone) ? new PhoneNumber(dto.Phone) : null;

        var nurse = Nurse.Create(
            dto.FirstName,
            dto.LastName,
            department,
            dto.KeycloakUserId,
            email,
            phone);

        await _nurseRepository.AddAsync(nurse);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Nurse created with ID {NurseId}", nurse.ExternalId);

        return nurse.ToDto();
    }

    public async Task<NurseDto> UpdateNurseAsync(string externalId, UpdateNurseDto dto)
    {
        _logger.LogInformation("Updating nurse {NurseId}", externalId);

        var nurse = await _nurseRepository.GetByIdAsync(externalId);
        if (nurse == null)
        {
            _logger.LogWarning("Nurse with ID {NurseId} not found for update", externalId);
            throw new NotFoundException("Nurse", externalId);
        }

        var validationResult = await _updateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for nurse update: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            throw new ValidationException(validationResult.Errors);
        }

        nurse.UpdatePersonalInfo(dto.FirstName, dto.LastName);
        nurse.UpdateDepartment(new Department(dto.DepartmentName, dto.DepartmentCode));

        var email = !string.IsNullOrWhiteSpace(dto.Email) ? new Email(dto.Email) : null;
        var phone = !string.IsNullOrWhiteSpace(dto.Phone) ? new PhoneNumber(dto.Phone) : null;
        nurse.UpdateContactInfo(email, phone);

        if (!string.IsNullOrWhiteSpace(dto.KeycloakUserId))
        {
            nurse.LinkToKeycloakUser(dto.KeycloakUserId);
        }

        _nurseRepository.Update(nurse);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Nurse {NurseId} updated successfully", externalId);

        return nurse.ToDto();
    }

    public async Task DeactivateNurseAsync(string externalId)
    {
        _logger.LogInformation("Deactivating nurse {NurseId}", externalId);

        var nurse = await _nurseRepository.GetByIdAsync(externalId);
        if (nurse == null)
        {
            _logger.LogWarning("Nurse with ID {NurseId} not found for deactivation", externalId);
            throw new NotFoundException("Nurse", externalId);
        }

        if (!nurse.IsActive)
        {
            throw new BusinessException("Nurse is already deactivated");
        }

        nurse.Deactivate();

        _nurseRepository.Update(nurse);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Nurse {NurseId} deactivated successfully", externalId);
    }

    public async Task ReactivateNurseAsync(string externalId)
    {
        _logger.LogInformation("Reactivating nurse {NurseId}", externalId);

        var nurse = await _nurseRepository.GetByIdAsync(externalId);
        if (nurse == null)
        {
            _logger.LogWarning("Nurse with ID {NurseId} not found for reactivation", externalId);
            throw new NotFoundException("Nurse", externalId);
        }

        if (nurse.IsActive)
        {
            throw new BusinessException("Nurse is already active");
        }

        nurse.Reactivate();

        _nurseRepository.Update(nurse);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Nurse {NurseId} reactivated successfully", externalId);
    }
}
