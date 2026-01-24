using FluentValidation;
using PatientSyncHealth.Domain.Aggregates.Examination;
using PatientSyncHealth.Domain.Interfaces;
using PatientSyncHealth.DTOs.Common;
using PatientSyncHealth.DTOs.Examinations;
using PatientSyncHealth.Infrastructure.Data;
using PatientSyncHealth.Mappings;
using PatientSyncHealth.Middleware.Exceptions;
using PatientSyncHealth.Services.Interfaces;

namespace PatientSyncHealth.Services;

public class ExaminationService : IExaminationService
{
    private readonly IExaminationRepository _examinationRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExaminationService> _logger;
    private readonly IValidator<CreateExaminationDto> _createValidator;
    private readonly IValidator<CompleteExaminationDto> _completeValidator;

    public ExaminationService(
        IExaminationRepository examinationRepository,
        IPatientRepository patientRepository,
        IDoctorRepository doctorRepository,
        IUnitOfWork unitOfWork,
        ILogger<ExaminationService> logger,
        IValidator<CreateExaminationDto> createValidator,
        IValidator<CompleteExaminationDto> completeValidator)
    {
        _examinationRepository = examinationRepository;
        _patientRepository = patientRepository;
        _doctorRepository = doctorRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _createValidator = createValidator;
        _completeValidator = completeValidator;
    }

    public async Task<ExaminationDto> GetByIdAsync(string externalId)
    {
        var examination = await _examinationRepository.GetByIdAsync(externalId);

        if (examination == null)
        {
            _logger.LogWarning("Examination with ID {ExaminationId} not found", externalId);
            throw new NotFoundException("Examination", externalId);
        }

        return examination.ToDto();
    }

    public async Task<PagedResult<ExaminationListDto>> GetExaminationsAsync(ExaminationSearchParameters parameters)
    {
        return await _examinationRepository.GetPagedAsync(parameters);
    }

    public async Task<List<ExaminationListDto>> GetByPatientIdAsync(string patientId)
    {
        var patientExists = await _patientRepository.ExistsAsync(patientId);
        if (!patientExists)
        {
            throw new NotFoundException("Patient", patientId);
        }

        return await _examinationRepository.GetByPatientIdAsync(patientId);
    }

    public async Task<List<ExaminationListDto>> GetByDoctorIdAsync(string doctorId)
    {
        var doctorExists = await _doctorRepository.ExistsAsync(doctorId);
        if (!doctorExists)
        {
            throw new NotFoundException("Doctor", doctorId);
        }

        return await _examinationRepository.GetByDoctorIdAsync(doctorId);
    }

    public async Task<ExaminationDto> CreateExaminationAsync(CreateExaminationDto dto)
    {
        _logger.LogInformation("Creating examination for patient {PatientId} by doctor {DoctorId} on {Date}",
            dto.PatientId, dto.DoctorId, dto.ExaminationDate.ToShortDateString());

        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for examination creation: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            throw new ValidationException(validationResult.Errors);
        }

        var examination = Examination.Create(
            dto.PatientId,
            dto.DoctorId,
            dto.ExaminationDate,
            dto.Diagnosis,
            dto.Notes);

        // Update patient's examination record
        var patient = await _patientRepository.GetByIdAsync(dto.PatientId);
        if (patient != null)
        {
            patient.RecordExamination(dto.ExaminationDate);
            _patientRepository.Update(patient);
        }

        await _examinationRepository.AddAsync(examination);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Examination created with ID {ExaminationId}", examination.ExternalId);

        return examination.ToDto();
    }

    public async Task<ExaminationDto> CompleteExaminationAsync(string externalId, CompleteExaminationDto dto)
    {
        _logger.LogInformation("Completing examination {ExaminationId}", externalId);

        var examination = await _examinationRepository.GetByIdAsync(externalId);
        if (examination == null)
        {
            _logger.LogWarning("Examination with ID {ExaminationId} not found", externalId);
            throw new NotFoundException("Examination", externalId);
        }

        var validationResult = await _completeValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for examination completion: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            throw new ValidationException(validationResult.Errors);
        }

        if (examination.IsCompleted)
        {
            throw new BusinessException("Examination is already completed");
        }

        examination.Complete(dto.Diagnosis, dto.Notes);

        _examinationRepository.Update(examination);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Examination {ExaminationId} completed successfully", externalId);

        return examination.ToDto();
    }
}
