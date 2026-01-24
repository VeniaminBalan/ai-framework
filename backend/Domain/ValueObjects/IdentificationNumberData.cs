using System.ComponentModel.DataAnnotations.Schema;
using PatientSyncHealth.Domain.Enums;

namespace PatientSyncHealth.Domain.ValueObjects;

/// <summary>
/// EF Core Complex Type for storing identification number data.
/// This is a persistence concern only - no validation, no behavior.
/// </summary>
[ComplexType]
public class IdentificationNumberData
{
    public required string Value { get; set; }
    public required IdentificationNumberType Type { get; set; }
}
