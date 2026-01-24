using PatientSyncHealth.Domain.Common;
using PatientSyncHealth.Domain.Exceptions;

namespace PatientSyncHealth.Domain.ValueObjects;

public sealed class Department : ValueObject
{
    public string Name { get; private set; } = string.Empty;
    public string? Code { get; private set; }

    private Department() { } // EF Core

    public Department(string name, string? code = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Department name is required");

        name = name.Trim();

        if (name.Length > 100)
            throw new DomainException("Department name cannot exceed 100 characters");

        Name = name;
        Code = code?.Trim().ToUpperInvariant();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
        yield return Code;
    }

    public override string ToString() => Code != null ? $"{Name} ({Code})" : Name;
}
