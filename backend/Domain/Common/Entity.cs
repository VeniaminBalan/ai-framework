namespace PatientSyncHealth.Domain.Common;

public abstract class Entity
{
    public int Id { get; protected set; }

    public string ExternalId { get; protected set; } = Guid.CreateVersion7().ToString();

    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    public string? CreatedBy { get; protected set; }

    public DateTime? UpdatedAt { get; protected set; }

    public string? UpdatedBy { get; protected set; }

    public void SetAuditInfo(string? userId, bool isUpdate = false)
    {
        if (isUpdate)
        {
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = userId;
        }
        else
        {
            CreatedBy = userId;
        }
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        if (Id == default || other.Id == default)
            return false;

        return Id == other.Id;
    }

    public override int GetHashCode() => (GetType().ToString() + Id).GetHashCode();

    public static bool operator ==(Entity? a, Entity? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return a.Equals(b);
    }

    public static bool operator !=(Entity? a, Entity? b) => !(a == b);
}
