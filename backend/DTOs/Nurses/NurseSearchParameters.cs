using System.ComponentModel.DataAnnotations;

namespace PatientSyncHealth.DTOs.Nurses;

public record NurseSearchParameters
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 10;

    [MaxLength(100)]
    public string? SearchTerm { get; init; }

    [MaxLength(100)]
    public string? Department { get; init; }

    public bool? IsActive { get; init; }

    [MaxLength(50)]
    public string? SortBy { get; init; } = "LastName";

    public bool SortDescending { get; init; } = false;
}
