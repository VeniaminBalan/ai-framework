using FluentValidation;
using PatientSyncHealth.Domain.Aggregates.Patient;
using PatientSyncHealth.Domain.Interfaces;
using PatientSyncHealth.Domain.ValueObjects;
using PatientSyncHealth.DTOs.Common;
using PatientSyncHealth.DTOs.Patients;
using PatientSyncHealth.Infrastructure.Data;
using PatientSyncHealth.Mappings;
using PatientSyncHealth.Middleware.Exceptions;
using PatientSyncHealth.Services.Interfaces;

namespace PatientSyncHealth.Services;

public class PatientService : IPatientService
{
    private readonly IPatientRepository _patientRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PatientService> _logger;
    private readonly IValidator<CreatePatientDto> _createValidator;
    private readonly IValidator<UpdatePatientDto> _updateValidator;
    private readonly IValidator<RecordExaminationDto> _recordExaminationValidator;

    public PatientService(
        IPatientRepository patientRepository,
        IUnitOfWork unitOfWork,
        ILogger<PatientService> logger,
        IValidator<CreatePatientDto> createValidator,
        IValidator<UpdatePatientDto> updateValidator,
        IValidator<RecordExaminationDto> recordExaminationValidator)
    {
        _patientRepository = patientRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _recordExaminationValidator = recordExaminationValidator;
    }

    /// <inheritdoc />
    public async Task<PatientDto> GetByIdAsync(string externalId)
    {
        var patient = await _patientRepository.GetByIdAsync(externalId);

        if (patient == null)
        {
            _logger.LogWarning("Patient with ID {PatientId} not found", externalId);
            throw new NotFoundException("Patient", externalId);
        }

        return patient.ToDto();
    }

    /// <inheritdoc />
    public async Task<PagedResult<PatientListDto>> GetPatientsAsync(PatientSearchParameters parameters)
    {
        return await _patientRepository.GetPagedAsync(parameters);
    }

    /// <inheritdoc />
    public async Task<List<PatientListDto>> GetOverdueExaminationsAsync()
    {
        return await _patientRepository.GetPatientsWithOverdueExaminationsAsync();
    }

    /// <inheritdoc />
    public async Task<PatientDto> CreatePatientAsync(CreatePatientDto dto)
    {
        _logger.LogInformation("Creating patient with identification number {IdNumber} (type: {IdType})",
            MaskIdentificationNumber(dto.IdentificationNumber.Value), dto.IdentificationNumber.Type);

        // Validate with FluentValidation (includes checksum and uniqueness check)
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for patient creation: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            throw new ValidationException(validationResult.Errors);
        }

        // Create value objects
        var identificationNumber = dto.IdentificationNumber.ToValueObject();
        var email = !string.IsNullOrWhiteSpace(dto.Email) ? new Email(dto.Email) : null;
        var phone = !string.IsNullOrWhiteSpace(dto.Phone) ? new PhoneNumber(dto.Phone) : null;
        var address = dto.Address?.ToValueObject();

        // Create patient using factory method
        var patient = Patient.Create(
            dto.FirstName,
            dto.LastName,
            identificationNumber,
            dto.DateOfBirth,
            dto.Gender,
            dto.ExaminationFrequency,
            email,
            phone,
            address);

        await _patientRepository.AddAsync(patient);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Patient created with ID {PatientId}", patient.ExternalId);

        return patient.ToDto();
    }

    /// <inheritdoc />
    public async Task<PatientDto> UpdatePatientAsync(string externalId, UpdatePatientDto dto)
    {
        _logger.LogInformation("Updating patient {PatientId}", externalId);

        var patient = await _patientRepository.GetByIdAsync(externalId);
        if (patient == null)
        {
            _logger.LogWarning("Patient with ID {PatientId} not found for update", externalId);
            throw new NotFoundException("Patient", externalId);
        }

        // Validate with FluentValidation
        var validationResult = await _updateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for patient update: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            throw new ValidationException(validationResult.Errors);
        }

        // Update personal info
        patient.UpdatePersonalInfo(
            dto.FirstName,
            dto.LastName,
            dto.DateOfBirth,
            dto.Gender);

        // Update contact info
        var email = !string.IsNullOrWhiteSpace(dto.Email) ? new Email(dto.Email) : null;
        var phone = !string.IsNullOrWhiteSpace(dto.Phone) ? new PhoneNumber(dto.Phone) : null;
        var address = dto.Address?.ToValueObject();
        patient.UpdateContactInfo(email, phone, address);

        // Update examination schedule if changed
        if (patient.ExaminationFrequency != dto.ExaminationFrequency)
        {
            patient.UpdateExaminationSchedule(dto.ExaminationFrequency);
        }

        _patientRepository.Update(patient);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Patient {PatientId} updated successfully", externalId);

        return patient.ToDto();
    }

    /// <inheritdoc />
    public async Task DeactivatePatientAsync(string externalId)
    {
        _logger.LogInformation("Deactivating patient {PatientId}", externalId);

        var patient = await _patientRepository.GetByIdAsync(externalId);
        if (patient == null)
        {
            _logger.LogWarning("Patient with ID {PatientId} not found for deactivation", externalId);
            throw new NotFoundException("Patient", externalId);
        }

        if (!patient.IsActive)
        {
            throw new BusinessException("Patient is already deactivated");
        }

        patient.Deactivate();

        _patientRepository.Update(patient);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Patient {PatientId} deactivated successfully", externalId);
    }

    /// <inheritdoc />
    public async Task ReactivatePatientAsync(string externalId)
    {
        _logger.LogInformation("Reactivating patient {PatientId}", externalId);

        var patient = await _patientRepository.GetByIdAsync(externalId);
        if (patient == null)
        {
            _logger.LogWarning("Patient with ID {PatientId} not found for reactivation", externalId);
            throw new NotFoundException("Patient", externalId);
        }

        if (patient.IsActive)
        {
            throw new BusinessException("Patient is already active");
        }

        patient.Reactivate();

        _patientRepository.Update(patient);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Patient {PatientId} reactivated successfully", externalId);
    }

    /// <inheritdoc />
    public async Task RecordExaminationAsync(string externalId, RecordExaminationDto dto)
    {
        _logger.LogInformation("Recording examination for patient {PatientId} on {ExaminationDate}",
            externalId, dto.ExaminationDate.ToShortDateString());

        var patient = await _patientRepository.GetByIdAsync(externalId);
        if (patient == null)
        {
            _logger.LogWarning("Patient with ID {PatientId} not found for examination recording", externalId);
            throw new NotFoundException("Patient", externalId);
        }

        // Validate with FluentValidation
        var validationResult = await _recordExaminationValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Domain logic handles validation of examination date
        patient.RecordExamination(dto.ExaminationDate);

        _patientRepository.Update(patient);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Examination recorded for patient {PatientId}. Next examination: {NextExaminationDate}",
            externalId,
            patient.NextExaminationDate?.ToShortDateString());
    }

    private static string MaskIdentificationNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 6)
            return "***";

        return $"{value[..3]}****{value[^3..]}";
    }
}
