using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PatientSyncHealth.DTOs.Common;
using PatientSyncHealth.DTOs.Examinations;
using PatientSyncHealth.Services.Interfaces;

namespace PatientSyncHealth.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ExaminationsController : ControllerBase
{
    private readonly IExaminationService _examinationService;

    public ExaminationsController(IExaminationService examinationService)
    {
        _examinationService = examinationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ExaminationListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ExaminationListDto>>> GetAll(
        [FromQuery] ExaminationSearchParameters parameters)
    {
        var result = await _examinationService.GetExaminationsAsync(parameters);

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

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ExaminationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExaminationDto>> GetById(string id)
    {
        var examination = await _examinationService.GetByIdAsync(id);
        return Ok(examination);
    }

    [HttpGet("patient/{patientId}")]
    [ProducesResponseType(typeof(List<ExaminationListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ExaminationListDto>>> GetByPatientId(string patientId)
    {
        var examinations = await _examinationService.GetByPatientIdAsync(patientId);
        return Ok(examinations);
    }

    [HttpGet("doctor/{doctorId}")]
    [ProducesResponseType(typeof(List<ExaminationListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ExaminationListDto>>> GetByDoctorId(string doctorId)
    {
        var examinations = await _examinationService.GetByDoctorIdAsync(doctorId);
        return Ok(examinations);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ExaminationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExaminationDto>> Create([FromBody] CreateExaminationDto dto)
    {
        var examination = await _examinationService.CreateExaminationAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = examination.Id }, examination);
    }

    [HttpPost("{id}/complete")]
    [ProducesResponseType(typeof(ExaminationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExaminationDto>> Complete(string id, [FromBody] CompleteExaminationDto dto)
    {
        var examination = await _examinationService.CompleteExaminationAsync(id, dto);
        return Ok(examination);
    }
}
