using System.ComponentModel.DataAnnotations;

namespace PatientSyncHealth.DTOs.Appointments;

public record CancelAppointmentDto
{
    [MaxLength(500)]
    public string? Reason { get; init; }
}
