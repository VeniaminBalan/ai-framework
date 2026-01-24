using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PatientSyncHealth.DTOs.Appointments;
using PatientSyncHealth.DTOs.Common;
using PatientSyncHealth.Services.Interfaces;

namespace PatientSyncHealth.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AppointmentListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AppointmentListDto>>> GetAll(
        [FromQuery] AppointmentSearchParameters parameters)
    {
        var result = await _appointmentService.GetAppointmentsAsync(parameters);

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
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> GetById(string id)
    {
        var appointment = await _appointmentService.GetByIdAsync(id);
        return Ok(appointment);
    }

    [HttpGet("doctor/{doctorId}/calendar")]
    [ProducesResponseType(typeof(List<AppointmentListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AppointmentListDto>>> GetDoctorCalendar(
        string doctorId,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        var appointments = await _appointmentService.GetDoctorCalendarAsync(doctorId, fromDate, toDate);
        return Ok(appointments);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AppointmentDto>> Schedule([FromBody] ScheduleAppointmentDto dto)
    {
        var appointment = await _appointmentService.ScheduleAppointmentAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, appointment);
    }

    [HttpPut("{id}/reschedule")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> Reschedule(string id, [FromBody] RescheduleAppointmentDto dto)
    {
        var appointment = await _appointmentService.RescheduleAppointmentAsync(id, dto);
        return Ok(appointment);
    }

    [HttpPost("{id}/confirm")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> Confirm(string id)
    {
        var appointment = await _appointmentService.ConfirmAppointmentAsync(id);
        return Ok(appointment);
    }

    [HttpPost("{id}/start")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> Start(string id)
    {
        var appointment = await _appointmentService.StartAppointmentAsync(id);
        return Ok(appointment);
    }

    [HttpPost("{id}/complete")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> Complete(string id, [FromBody] CompleteAppointmentDto dto)
    {
        var appointment = await _appointmentService.CompleteAppointmentAsync(id, dto);
        return Ok(appointment);
    }

    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> Cancel(string id, [FromBody] CancelAppointmentDto dto)
    {
        var appointment = await _appointmentService.CancelAppointmentAsync(id, dto);
        return Ok(appointment);
    }
}
