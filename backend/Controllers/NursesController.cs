using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PatientSyncHealth.DTOs.Common;
using PatientSyncHealth.DTOs.Nurses;
using PatientSyncHealth.Services.Interfaces;

namespace PatientSyncHealth.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class NursesController : ControllerBase
{
    private readonly INurseService _nurseService;

    public NursesController(INurseService nurseService)
    {
        _nurseService = nurseService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NurseListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<NurseListDto>>> GetAll(
        [FromQuery] NurseSearchParameters parameters)
    {
        var result = await _nurseService.GetNursesAsync(parameters);

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
    [ProducesResponseType(typeof(NurseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NurseDto>> GetById(string id)
    {
        var nurse = await _nurseService.GetByIdAsync(id);
        return Ok(nurse);
    }

    [HttpPost]
    [ProducesResponseType(typeof(NurseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NurseDto>> Create([FromBody] CreateNurseDto dto)
    {
        var nurse = await _nurseService.CreateNurseAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = nurse.Id }, nurse);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(NurseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NurseDto>> Update(string id, [FromBody] UpdateNurseDto dto)
    {
        var nurse = await _nurseService.UpdateNurseAsync(id, dto);
        return Ok(nurse);
    }

    [HttpPost("{id}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(string id)
    {
        await _nurseService.DeactivateNurseAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate(string id)
    {
        await _nurseService.ReactivateNurseAsync(id);
        return NoContent();
    }
}
