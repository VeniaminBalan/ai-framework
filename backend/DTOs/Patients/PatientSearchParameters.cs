using System.ComponentModel.DataAnnotations;

namespace PatientSyncHealth.DTOs.Patients;

public record PatientSearchParameters
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 10;

    [MaxLength(100)]
    public string? SearchTerm { get; init; }

    public bool? IsActive { get; init; }

    public bool? IsOverdue { get; init; }

    [MaxLength(50)]
    public string? SortBy { get; init; } = "LastName";

    public bool SortDescending { get; init; } = false;
}
