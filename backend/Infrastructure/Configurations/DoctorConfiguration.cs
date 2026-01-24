using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientSyncHealth.Domain.Aggregates.Doctor;

namespace PatientSyncHealth.Infrastructure.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("Doctors");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.ExternalId)
            .IsRequired()
            .HasMaxLength(50);
        builder.HasIndex(d => d.ExternalId).IsUnique();

        builder.Property(d => d.CreatedAt).IsRequired();
        builder.Property(d => d.CreatedBy).HasMaxLength(100);
        builder.Property(d => d.UpdatedBy).HasMaxLength(100);

        builder.Property(d => d.KeycloakUserId)
            .HasMaxLength(50);
        builder.HasIndex(d => d.KeycloakUserId)
            .IsUnique()
            .HasFilter("\"KeycloakUserId\" IS NOT NULL");

        builder.Property(d => d.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.ComplexProperty(d => d.Specialization, spec =>
        {
            spec.Property(s => s.Value)
                .HasColumnName("Specialization")
                .IsRequired()
                .HasMaxLength(100);
        });

        builder.ComplexProperty(d => d.LicenseNumber, license =>
        {
            license.Property(l => l.Value)
                .HasColumnName("LicenseNumber")
                .IsRequired()
                .HasMaxLength(20);
        });
        // Note: Unique constraint on LicenseNumber is enforced through application logic in validator

        builder.OwnsOne(d => d.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("Email")
                .HasMaxLength(255);
        });

        builder.OwnsOne(d => d.Phone, phone =>
        {
            phone.Property(p => p.Value)
                .HasColumnName("Phone")
                .HasMaxLength(20);
        });

        builder.Property(d => d.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(d => d.LastName);
        builder.HasIndex(d => d.IsActive);
    }
}
