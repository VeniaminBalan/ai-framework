using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PatientSyncHealth.DTOs.Common;
using PatientSyncHealth.DTOs.Doctors;
using PatientSyncHealth.Services.Interfaces;

namespace PatientSyncHealth.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class DoctorsController : ControllerBase
{
    private readonly IDoctorService _doctorService;

    public DoctorsController(IDoctorService doctorService)
    {
        _doctorService = doctorService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DoctorListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DoctorListDto>>> GetAll(
        [FromQuery] DoctorSearchParameters parameters)
    {
        var result = await _doctorService.GetDoctorsAsync(parameters);

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
    [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DoctorDto>> GetById(string id)
    {
        var doctor = await _doctorService.GetByIdAsync(id);
        return Ok(doctor);
    }

    [HttpPost]
    [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DoctorDto>> Create([FromBody] CreateDoctorDto dto)
    {
        var doctor = await _doctorService.CreateDoctorAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = doctor.Id }, doctor);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DoctorDto>> Update(string id, [FromBody] UpdateDoctorDto dto)
    {
        var doctor = await _doctorService.UpdateDoctorAsync(id, dto);
        return Ok(doctor);
    }

    [HttpPost("{id}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(string id)
    {
        await _doctorService.DeactivateDoctorAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(string id)
    {
        await _doctorService.ReactivateDoctorAsync(id);
        return NoContent();
    }
}
