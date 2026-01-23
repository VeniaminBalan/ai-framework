using PatientSyncHealth.Domain.Common;
using PatientSyncHealth.Domain.Exceptions;

namespace PatientSyncHealth.Domain.ValueObjects;

public class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string County { get; }
    public string? PostalCode { get; }
    public string Country { get; }

    private Address()
    {
        Street = string.Empty;
        City = string.Empty;
        County = string.Empty;
        Country = string.Empty;
    } // EF Core

    public Address(string street, string city, string county, string? postalCode = null, string country = "Romania")
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new DomainException("Street is required");

        if (string.IsNullOrWhiteSpace(city))
            throw new DomainException("City is required");

        if (string.IsNullOrWhiteSpace(county))
            throw new DomainException("County is required");

        if (!string.IsNullOrWhiteSpace(postalCode) && !IsValidPostalCode(postalCode))
            throw new DomainException("Postal code must be 6 digits");

        Street = street.Trim();
        City = city.Trim();
        County = county.Trim();
        PostalCode = postalCode?.Trim();
        Country = string.IsNullOrWhiteSpace(country) ? "Romania" : country.Trim();
    }

    private static bool IsValidPostalCode(string postalCode)
    {
        var cleaned = postalCode.Trim();
        return cleaned.Length == 6 && cleaned.All(char.IsDigit);
    }

    public string GetFullAddress()
    {
        var parts = new List<string> { Street, City, County };

        if (!string.IsNullOrWhiteSpace(PostalCode))
            parts.Add(PostalCode);

        parts.Add(Country);

        return string.Join(", ", parts);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return County;
        yield return PostalCode;
        yield return Country;
    }

    public override string ToString() => GetFullAddress();
}
