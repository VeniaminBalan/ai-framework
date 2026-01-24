namespace PatientSyncHealth.DTOs.Appointments;

public record CompleteAppointmentDto
{
    public string? ResultingExaminationId { get; init; }
}
