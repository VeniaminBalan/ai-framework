using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientSyncHealth.DTOs.Common;
using PatientSyncHealth.DTOs.Patients;
using PatientSyncHealth.Services.Interfaces;

namespace PatientSyncHealth.Controllers;

/// <summary>
/// API endpoints for managing patients in the preventive medical examination system.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    /// <summary>
    /// Gets a paginated list of patients with optional filtering and sorting.
    /// </summary>
    /// <param name="parameters">Search and pagination parameters</param>
    /// <returns>Paginated list of patients</returns>
    /// <response code="200">Returns the paginated list of patients</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PatientListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PatientListDto>>> GetAll(
        [FromQuery] PatientSearchParameters parameters)
    {
        var result = await _patientService.GetPatientsAsync(parameters);

        // Add pagination metadata to response headers
        Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(new
        {
            result.TotalCount,
            result.PageSize,
            result.PageNumber,
            result.TotalPages,
            result.HasPreviousPage,
            result.HasNextPage
        }));

        return Ok(result);
    }

    /// <summary>
    /// Gets a patient by their unique identifier.
    /// </summary>
    /// <param name="id">The patient's external ID</param>
    /// <returns>The patient details</returns>
    /// <response code="200">Returns the patient details</response>
    /// <response code="404">If the patient is not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientDto>> GetById(string id)
    {
        var patient = await _patientService.GetByIdAsync(id);
        return Ok(patient);
    }

    /// <summary>
    /// Gets all patients with overdue medical examinations.
    /// </summary>
    /// <returns>List of patients with overdue examinations</returns>
    /// <response code="200">Returns the list of patients with overdue examinations</response>
    [HttpGet("overdue-examinations")]
    [ProducesResponseType(typeof(List<PatientListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PatientListDto>>> GetOverdueExaminations()
    {
        var patients = await _patientService.GetOverdueExaminationsAsync();
        return Ok(patients);
    }

    /// <summary>
    /// Creates a new patient.
    /// </summary>
    /// <param name="dto">The patient creation data</param>
    /// <returns>The created patient</returns>
    /// <response code="201">Returns the newly created patient</response>
    /// <response code="400">If the request data is invalid or CNP already exists</response>
    [HttpPost]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PatientDto>> Create([FromBody] CreatePatientDto dto)
    {
        var patient = await _patientService.CreatePatientAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, patient);
    }

    /// <summary>
    /// Updates an existing patient.
    /// </summary>
    /// <param name="id">The patient's external ID</param>
    /// <param name="dto">The patient update data</param>
    /// <returns>The updated patient</returns>
    /// <response code="200">Returns the updated patient</response>
    /// <response code="400">If the request data is invalid</response>
    /// <response code="404">If the patient is not found</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientDto>> Update(string id, [FromBody] UpdatePatientDto dto)
    {
        var patient = await _patientService.UpdatePatientAsync(id, dto);
        return Ok(patient);
    }

    /// <summary>
    /// Deactivates a patient (soft delete).
    /// </summary>
    /// <param name="id">The patient's external ID</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Patient was successfully deactivated</response>
    /// <response code="400">If the patient is already deactivated</response>
    /// <response code="404">If the patient is not found</response>
    [HttpPost("{id}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(string id)
    {
        await _patientService.DeactivatePatientAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Reactivates a previously deactivated patient.
    /// </summary>
    /// <param name="id">The patient's external ID</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Patient was successfully reactivated</response>
    /// <response code="400">If the patient is already active</response>
    /// <response code="404">If the patient is not found</response>
    [HttpPost("{id}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(string id)
    {
        await _patientService.ReactivatePatientAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Records a completed medical examination for a patient.
    /// Updates the last examination date and calculates the next examination date.
    /// </summary>
    /// <param name="id">The patient's external ID</param>
    /// <param name="dto">The examination data</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Examination was successfully recorded</response>
    /// <response code="400">If the examination date is invalid</response>
    /// <response code="404">If the patient is not found</response>
    [HttpPost("{id}/examinations")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordExamination(string id, [FromBody] RecordExaminationDto dto)
    {
        await _patientService.RecordExaminationAsync(id, dto);
        return NoContent();
    }
}
