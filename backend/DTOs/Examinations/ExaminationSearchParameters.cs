using System.ComponentModel.DataAnnotations;

namespace PatientSyncHealth.DTOs.Examinations;

public record ExaminationSearchParameters
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 10;

    public string? PatientId { get; init; }

    public string? DoctorId { get; init; }

    public bool? IsCompleted { get; init; }

    public DateTime? FromDate { get; init; }

    public DateTime? ToDate { get; init; }

    [MaxLength(50)]
    public string? SortBy { get; init; } = "ExaminationDate";

    public bool SortDescending { get; init; } = true;
}
