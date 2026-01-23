using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientSyncHealth.Domain.Aggregates.Patient;
using PatientSyncHealth.Domain.Enums;

namespace PatientSyncHealth.Infrastructure.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");

        // Primary key
        builder.HasKey(p => p.Id);

        // ExternalId - unique index for external API lookups
        builder.Property(p => p.ExternalId)
            .IsRequired()
            .HasMaxLength(50);
        builder.HasIndex(p => p.ExternalId).IsUnique();

        // Audit fields
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.CreatedBy).HasMaxLength(100);
        builder.Property(p => p.UpdatedBy).HasMaxLength(100);

        // Personal Information
        builder.Property(p => p.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.DateOfBirth)
            .IsRequired();

        builder.Property(p => p.Gender)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // CNP Value Object - owned type
        builder.OwnsOne(p => p.Cnp, cnp =>
        {
            cnp.Property(c => c.Value)
                .HasColumnName("Cnp")
                .IsRequired()
                .HasMaxLength(13);

            cnp.HasIndex(c => c.Value).IsUnique();
        });

        // Email Value Object - owned type (optional)
        builder.OwnsOne(p => p.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("Email")
                .HasMaxLength(255);
        });

        // Phone Value Object - owned type (optional)
        builder.OwnsOne(p => p.Phone, phone =>
        {
            phone.Property(ph => ph.Value)
                .HasColumnName("Phone")
                .HasMaxLength(20);
        });

        // Address Value Object - owned type (optional)
        builder.OwnsOne(p => p.Address, address =>
        {
            address.Property(a => a.Street)
                .HasColumnName("Address_Street")
                .HasMaxLength(200);

            address.Property(a => a.City)
                .HasColumnName("Address_City")
                .HasMaxLength(100);

            address.Property(a => a.County)
                .HasColumnName("Address_County")
                .HasMaxLength(100);

            address.Property(a => a.PostalCode)
                .HasColumnName("Address_PostalCode")
                .HasMaxLength(6);

            address.Property(a => a.Country)
                .HasColumnName("Address_Country")
                .HasMaxLength(100);
        });

        // Examination Schedule
        builder.Property(p => p.ExaminationFrequency)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.LastExaminationDate);

        builder.Property(p => p.NextExaminationDate);

        // Status
        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Query indexes for common queries
        builder.HasIndex(p => p.LastName);
        builder.HasIndex(p => p.NextExaminationDate);
        builder.HasIndex(p => p.IsActive);
        builder.HasIndex(p => new { p.IsActive, p.NextExaminationDate });
    }
}
