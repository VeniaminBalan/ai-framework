using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PatientSyncHealth.Domain.Aggregates.Nurse;

namespace PatientSyncHealth.Infrastructure.Configurations;

public class NurseConfiguration : IEntityTypeConfiguration<Nurse>
{
    public void Configure(EntityTypeBuilder<Nurse> builder)
    {
        builder.ToTable("Nurses");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.ExternalId)
            .IsRequired()
            .HasMaxLength(50);
        builder.HasIndex(n => n.ExternalId).IsUnique();

        builder.Property(n => n.CreatedAt).IsRequired();
        builder.Property(n => n.CreatedBy).HasMaxLength(100);
        builder.Property(n => n.UpdatedBy).HasMaxLength(100);

        builder.Property(n => n.KeycloakUserId)
            .HasMaxLength(50);
        builder.HasIndex(n => n.KeycloakUserId)
            .IsUnique()
            .HasFilter("\"KeycloakUserId\" IS NOT NULL");

        builder.Property(n => n.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(n => n.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.ComplexProperty(n => n.Department, dept =>
        {
            dept.Property(d => d.Name)
                .HasColumnName("DepartmentName")
                .IsRequired()
                .HasMaxLength(100);

            dept.Property(d => d.Code)
                .HasColumnName("DepartmentCode")
                .HasMaxLength(20);
        });

        builder.OwnsOne(n => n.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("Email")
                .HasMaxLength(255);
        });

        builder.OwnsOne(n => n.Phone, phone =>
        {
            phone.Property(p => p.Value)
                .HasColumnName("Phone")
                .HasMaxLength(20);
        });

        builder.Property(n => n.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(n => n.LastName);
        builder.HasIndex(n => n.IsActive);
    }
}
