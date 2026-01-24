using PatientSyncHealth.Domain.Common;
using PatientSyncHealth.Domain.Exceptions;

namespace PatientSyncHealth.Domain.ValueObjects;

public sealed class Specialization : ValueObject
{
    public string Value { get; private set; } = string.Empty;

    private Specialization() { } // EF Core

    public Specialization(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Specialization is required");

        value = value.Trim();

        if (value.Length > 100)
            throw new DomainException("Specialization cannot exceed 100 characters");

        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
