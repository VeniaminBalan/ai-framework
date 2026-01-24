using System.ComponentModel.DataAnnotations;
using PatientSyncHealth.Domain.Enums;

namespace PatientSyncHealth.DTOs.Appointments;

public record AppointmentSearchParameters
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 10;

    public string? PatientId { get; init; }

    public string? DoctorId { get; init; }

    public AppointmentStatus? Status { get; init; }

    public AppointmentPurpose? Purpose { get; init; }

    public DateTime? FromDate { get; init; }

    public DateTime? ToDate { get; init; }

    [MaxLength(50)]
    public string? SortBy { get; init; } = "ScheduledDateTime";

    public bool SortDescending { get; init; } = false;
}
